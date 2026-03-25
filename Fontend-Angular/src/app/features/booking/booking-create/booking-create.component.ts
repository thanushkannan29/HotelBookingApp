import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatStepperModule } from '@angular/material/stepper';
import { MatRadioModule } from '@angular/material/radio';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { distinctUntilChanged } from 'rxjs';
import { BookingService } from '../../../core/services/booking.service';
import { TransactionService } from '../../../core/services/api.services';
import { HotelService } from '../../../core/services/hotel.service';
import { WalletService } from '../../../core/services/wallet.service';
import { ToastService } from '../../../core/services/toast.service';
import {
  HotelDetailsDto, RoomAvailabilityDto, AvailableRoomDto,
  ReservationResponseDto, PaymentMethod, QrPaymentResponseDto,
  WalletResponseDto
} from '../../../core/models/models';

@Component({
  selector: 'app-booking-create',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatStepperModule,
    MatRadioModule, MatDatepickerModule, MatNativeDateModule,
    MatProgressSpinnerModule, MatCardModule, MatSlideToggleModule,
    MatChipsModule, MatTooltipModule,
  ],
  templateUrl: './booking-create.component.html',
  styleUrl: './booking-create.component.scss'
})
export class BookingCreateComponent implements OnInit {
  private fb                 = inject(FormBuilder);
  private route              = inject(ActivatedRoute);
  private router             = inject(Router);
  private bookingService     = inject(BookingService);
  private transactionService = inject(TransactionService);
  private hotelService       = inject(HotelService);
  private walletService      = inject(WalletService);
  private toast              = inject(ToastService);

  hotel              = signal<HotelDetailsDto | null>(null);
  availability       = signal<RoomAvailabilityDto[]>([]);
  availableRooms     = signal<AvailableRoomDto[]>([]);
  createdReservation = signal<ReservationResponseDto | null>(null);
  qrPayment          = signal<QrPaymentResponseDto | null>(null);
  walletInfo         = signal<WalletResponseDto | null>(null);
  isLoadingHotel     = signal(true);
  isBooking          = signal(false);
  isPaying           = signal(false);
  isValidatingPromo  = signal(false);
  promoValid         = signal<boolean | null>(null);
  promoMessage       = signal('');
  promoDiscount      = signal(0);
  useWallet          = signal(false);
  today              = new Date();
  tomorrow           = new Date(Date.now() + 86400000); // same-day block: min is tomorrow

  paymentMethods = Object.entries(PaymentMethod).map(([k, v]) => ({ id: +k, label: v }));

  bookingForm = this.fb.group({
    hotelId:       ['', Validators.required],
    roomTypeId:    ['', Validators.required],
    checkInDate:   [null as Date | null, Validators.required],
    checkOutDate:  [null as Date | null, Validators.required],
    numberOfRooms: [1, [Validators.required, Validators.min(1), Validators.max(10)]],
    promoCode:     [''],
    walletAmount:  [0, [Validators.min(0)]],
  });

  paymentForm = this.fb.group({
    paymentMethod: [1, Validators.required],
  });

  selectedRoomType = computed(() => {
    const rtId = this.bookingForm.get('roomTypeId')?.value;
    return this.availability().find(a => a.roomTypeId === rtId);
  });

  totalNights = computed(() => {
    const ci = this.bookingForm.get('checkInDate')?.value as Date | null;
    const co = this.bookingForm.get('checkOutDate')?.value as Date | null;
    if (!ci || !co) return 0;
    return Math.max(0, Math.floor((co.getTime() - ci.getTime()) / 86400000));
  });

  baseTotal = computed(() => {
    const rt    = this.selectedRoomType();
    const rooms = this.bookingForm.get('numberOfRooms')?.value ?? 1;
    return (rt?.pricePerNight ?? 0) * this.totalNights() * rooms;
  });

  gstPercent = computed(() => (this.hotel() as any)?.gstPercent ?? 0);
  gstAmount  = computed(() => Math.round(this.baseTotal() * this.gstPercent() / 100 * 100) / 100);

  finalTotal = computed(() => {
    const walletUsed = this.useWallet()
      ? Math.min(this.bookingForm.get('walletAmount')?.value ?? 0, this.walletInfo()?.balance ?? 0)
      : 0;
    return Math.max(0, this.baseTotal() + this.gstAmount() - this.promoDiscount() - walletUsed);
  });

  ngOnInit() {
    const p = this.route.snapshot.queryParams;
    const checkIn  = p['checkIn']  ? new Date(p['checkIn'])  : null;
    const checkOut = p['checkOut'] ? new Date(p['checkOut']) : null;

    this.bookingForm.patchValue({
      hotelId:     p['hotelId']    ?? '',
      roomTypeId:  p['roomTypeId'] ?? '',
      checkInDate:  checkIn,
      checkOutDate: checkOut,
    });

    if (p['hotelId']) {
      this.hotelService.getHotelDetails(p['hotelId']).subscribe(h => {
        this.hotel.set(h);
        this.isLoadingHotel.set(false);
      });
      if (checkIn && checkOut) this.loadAvailability(p['hotelId'], checkIn, checkOut);
      else this.isLoadingHotel.set(false);
    } else {
      this.isLoadingHotel.set(false);
    }

    // Load wallet balance
    this.walletService.getWallet(1, 1).subscribe({
      next: data => this.walletInfo.set(data.wallet),
      error: () => {}
    });

    this.bookingForm.get('checkInDate')?.valueChanges.subscribe(() => this.onDateChange());
    this.bookingForm.get('checkOutDate')?.valueChanges.subscribe(() => this.onDateChange());

    // Room type change: reset rooms and reload availability
    this.bookingForm.get('roomTypeId')?.valueChanges
      .pipe(distinctUntilChanged())
      .subscribe(rtId => {
        this.availableRooms.set([]);
        if (rtId) this.onRoomTypeChange(rtId);
      });
  }

  private onDateChange() {
    const { hotelId, checkInDate, checkOutDate } = this.bookingForm.value;
    if (hotelId && checkInDate && checkOutDate)
      this.loadAvailability(hotelId, checkInDate as Date, checkOutDate as Date);
  }

  private loadAvailability(hotelId: string, ci: Date, co: Date) {
    const ciStr = this.fmt(ci), coStr = this.fmt(co);
    this.hotelService.getAvailability(hotelId, ciStr, coStr).subscribe(a => {
      const map = new Map<string, RoomAvailabilityDto>();
      for (const item of a) {
        const ex = map.get(item.roomTypeId);
        if (!ex || item.availableRooms < ex.availableRooms) map.set(item.roomTypeId, item);
      }
      this.availability.set(Array.from(map.values()));
    });
  }

  onRoomTypeChange(rtId: string) {
    const { hotelId, checkInDate, checkOutDate } = this.bookingForm.value;
    if (hotelId && checkInDate && checkOutDate) {
      const ci = this.fmt(checkInDate as Date), co = this.fmt(checkOutDate as Date);
      this.bookingService.getAvailableRooms(hotelId!, rtId, ci, co)
        .subscribe(rooms => this.availableRooms.set(rooms));
    }
  }

  applyPromo() {
    const code = this.bookingForm.get('promoCode')?.value?.trim();
    const hotelId = this.bookingForm.get('hotelId')?.value;
    if (!code || !hotelId) return;

    this.isValidatingPromo.set(true);
    this.bookingService.validatePromoCode({
      code, hotelId, totalAmount: this.baseTotal()
    }).subscribe({
      next: result => {
        this.promoValid.set(result.isValid);
        this.promoMessage.set(result.message);
        this.promoDiscount.set(result.isValid ? result.discountAmount : 0);
        this.isValidatingPromo.set(false);
      },
      error: () => { this.isValidatingPromo.set(false); this.promoValid.set(false); }
    });
  }

  createReservation() {
    if (this.bookingForm.invalid) { this.bookingForm.markAllAsTouched(); return; }
    const v = this.bookingForm.value;

    // Same-day booking block
    const checkIn = v.checkInDate as Date;
    const todayDate = new Date(); todayDate.setHours(0,0,0,0);
    const checkInDate = new Date(checkIn); checkInDate.setHours(0,0,0,0);
    if (checkInDate <= todayDate) {
      this.toast.error('Same-day booking is not allowed. Please select a future date.');
      return;
    }

    const walletUsed = this.useWallet()
      ? Math.min(v.walletAmount ?? 0, this.walletInfo()?.balance ?? 0)
      : 0;

    this.isBooking.set(true);
    this.bookingService.createReservation({
      hotelId:          v.hotelId!,
      roomTypeId:       v.roomTypeId!,
      checkInDate:      this.fmt(checkIn),
      checkOutDate:     this.fmt(v.checkOutDate as Date),
      numberOfRooms:    v.numberOfRooms!,
      promoCodeUsed:    this.promoValid() ? v.promoCode ?? undefined : undefined,
      walletAmountToUse: walletUsed,
    }).subscribe({
      next: res => {
        this.createdReservation.set(res);
        this.isBooking.set(false);
        this.toast.success('Reservation created! Pay within 10 minutes to confirm.');
        // Load QR code if UPI
        this.bookingService.getPaymentQr(res.reservationId).subscribe({
          next: qr => this.qrPayment.set(qr),
          error: () => {}
        });
      },
      error: () => this.isBooking.set(false),
    });
  }

  pay() {
    const res = this.createdReservation();
    if (!res || this.paymentForm.invalid) return;
    this.isPaying.set(true);
    this.transactionService.createPayment({
      reservationId: res.reservationId,
      paymentMethod: this.paymentForm.get('paymentMethod')!.value!,
    }).subscribe({
      next: () => {
        this.isPaying.set(false);
        this.toast.success('Payment successful! Booking confirmed.');
        this.router.navigate(['/booking', res.reservationCode]);
      },
      error: () => this.isPaying.set(false),
    });
  }

  private fmt(d: Date): string { return d.toISOString().split('T')[0]; }

  get checkOutMin(): Date {
    const ci = this.bookingForm.get('checkInDate')?.value as Date | null;
    if (!ci) return this.tomorrow;
    const d = new Date(ci); d.setDate(d.getDate() + 1);
    return d;
  }
}
