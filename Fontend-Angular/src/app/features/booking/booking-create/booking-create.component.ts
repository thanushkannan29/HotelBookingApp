import { Component, inject, signal, OnInit, computed, ViewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatStepperModule, MatStepper } from '@angular/material/stepper';
import { MatRadioModule } from '@angular/material/radio';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
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
    MatChipsModule, MatTooltipModule, MatDividerModule,
  ],
  templateUrl: './booking-create.component.html',
  styleUrl: './booking-create.component.scss'
})
export class BookingCreateComponent implements OnInit {
  @ViewChild('stepper') stepper!: MatStepper;

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
  isToppingUp        = signal(false);
  promoValid         = signal<boolean | null>(null);
  promoMessage       = signal('');
  promoDiscount      = signal(0);
  useWallet          = signal(false);
  showTopUp          = signal(false);
  today              = new Date();
  tomorrow           = new Date(Date.now() + 86400000);

  // Payment methods — filter out "Wallet" (id=5) since wallet is handled separately
  paymentMethods = Object.entries(PaymentMethod)
    .filter(([k]) => !isNaN(+k) && +k !== 5)
    .map(([k, v]) => ({ id: +k, label: v as string }));

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
    paymentMethod: [3, Validators.required], // default UPI
  });

  topUpForm = this.fb.group({
    amount: [500, [Validators.required, Validators.min(1), Validators.max(100000)]]
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

  gstPercent = computed(() => this.hotel()?.gstPercent ?? 0);
  gstAmount  = computed(() => Math.round(this.baseTotal() * this.gstPercent() / 100 * 100) / 100);

  // Max rooms = available rooms for selected type (capped at 10)
  maxRooms = computed(() => {
    const rt = this.selectedRoomType();
    return rt ? Math.min(rt.availableRooms, 10) : 10;
  });

  walletUsedAmount = computed(() => {
    if (!this.useWallet()) return 0;
    const entered = this.bookingForm.get('walletAmount')?.value ?? 0;
    const balance = this.walletInfo()?.balance ?? 0;
    const maxUsable = Math.max(0, this.baseTotal() + this.gstAmount() - this.promoDiscount());
    return Math.min(entered, balance, maxUsable);
  });

  finalTotal = computed(() =>
    Math.max(0, this.baseTotal() + this.gstAmount() - this.promoDiscount() - this.walletUsedAmount())
  );

  // Step 1 is valid when room type + dates are selected
  step1Valid = computed(() =>
    !!this.bookingForm.get('roomTypeId')?.value &&
    !!this.bookingForm.get('checkInDate')?.value &&
    !!this.bookingForm.get('checkOutDate')?.value
  );

  ngOnInit() {
    const p = this.route.snapshot.queryParams;
    // Parse date strings as local dates (not UTC) to avoid timezone shift
    let checkIn  = p['checkIn']  ? this.parseLocalDate(p['checkIn'])  : null;
    let checkOut = p['checkOut'] ? this.parseLocalDate(p['checkOut']) : null;

    // If checkIn is today or in the past, auto-advance to tomorrow
    const todayLocal = new Date(); todayLocal.setHours(0,0,0,0);
    if (checkIn) {
      const ci = new Date(checkIn); ci.setHours(0,0,0,0);
      if (ci <= todayLocal) {
        checkIn = new Date(todayLocal); checkIn.setDate(checkIn.getDate() + 1);
        // Also push checkout forward by same offset
        if (checkOut) {
          const diff = checkOut.getTime() - (p['checkIn'] ? this.parseLocalDate(p['checkIn']).getTime() : 0);
          checkOut = new Date(checkIn.getTime() + diff);
        }
      }
    }

    this.bookingForm.patchValue({
      hotelId:      p['hotelId']    ?? '',
      roomTypeId:   p['roomTypeId'] ?? '',
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

    this.loadWallet();

    this.bookingForm.get('checkInDate')?.valueChanges.subscribe(() => this.onDateChange());
    this.bookingForm.get('checkOutDate')?.valueChanges.subscribe(() => this.onDateChange());

    // Room type change: reload available rooms
    this.bookingForm.get('roomTypeId')?.valueChanges
      .pipe(distinctUntilChanged())
      .subscribe(rtId => {
        this.availableRooms.set([]);
        // Reset promo when room type changes (price changes)
        this.promoValid.set(null);
        this.promoMessage.set('');
        this.promoDiscount.set(0);
        if (rtId) this.onRoomTypeChange(rtId);
      });
  }

  loadWallet() {
    this.walletService.getWallet(1, 1).subscribe({
      next: data => this.walletInfo.set(data.wallet),
      error: () => {}
    });
  }

  private onDateChange() {
    const { hotelId, checkInDate, checkOutDate } = this.bookingForm.value;
    if (hotelId && checkInDate && checkOutDate)
      this.loadAvailability(hotelId, checkInDate as Date, checkOutDate as Date);
  }

  private loadAvailability(hotelId: string, ci: Date, co: Date) {
    const ciStr = this.fmtLocal(ci), coStr = this.fmtLocal(co);
    this.hotelService.getAvailability(hotelId, ciStr, coStr).subscribe(a => {
      const map = new Map<string, RoomAvailabilityDto>();
      for (const item of a) {
        const ex = map.get(item.roomTypeId);
        if (!ex || item.availableRooms < ex.availableRooms) map.set(item.roomTypeId, item);
      }
      this.availability.set(Array.from(map.values()));
    });
  }

  selectRoomType(rtId: string) {
    this.bookingForm.patchValue({ roomTypeId: rtId });
    this.availableRooms.set([]);
    this.promoValid.set(null);
    this.promoMessage.set('');
    this.promoDiscount.set(0);
    this.onRoomTypeChange(rtId);

    // Update URL query param so browser reflects the selection
    const { hotelId, checkInDate, checkOutDate } = this.bookingForm.value;
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        hotelId,
        roomTypeId: rtId,
        checkIn:  checkInDate ? this.fmtLocal(checkInDate as Date) : null,
        checkOut: checkOutDate ? this.fmtLocal(checkOutDate as Date) : null,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });

    // Cap numberOfRooms to available rooms for this type and update validator
    const rt = this.availability().find(a => a.roomTypeId === rtId);
    if (rt) {
      const maxAvail = Math.min(rt.availableRooms, 10);
      const current = this.bookingForm.get('numberOfRooms')?.value ?? 1;
      // Update validator dynamically
      this.bookingForm.get('numberOfRooms')?.setValidators([
        Validators.required, Validators.min(1), Validators.max(maxAvail)
      ]);
      this.bookingForm.get('numberOfRooms')?.updateValueAndValidity();
      if (current > maxAvail) {
        this.bookingForm.patchValue({ numberOfRooms: maxAvail });
      }
    }
  }

  onRoomTypeChange(rtId: string) {
    const { hotelId, checkInDate, checkOutDate } = this.bookingForm.value;
    if (hotelId && checkInDate && checkOutDate) {
      const ci = this.fmtLocal(checkInDate as Date), co = this.fmtLocal(checkOutDate as Date);
      this.bookingService.getAvailableRooms(hotelId!, rtId, ci, co)
        .subscribe(rooms => this.availableRooms.set(rooms));
    }
  }

  applyPromo() {
    const code = this.bookingForm.get('promoCode')?.value?.trim();
    const hotelId = this.bookingForm.get('hotelId')?.value;
    if (!code || !hotelId) { this.toast.error('Enter a promo code first.'); return; }
    if (this.baseTotal() === 0) { this.toast.error('Select room type and dates first.'); return; }

    this.isValidatingPromo.set(true);
    this.bookingService.validatePromoCode({ code, hotelId, totalAmount: this.baseTotal() }).subscribe({
      next: result => {
        this.promoValid.set(result.isValid);
        this.promoMessage.set(result.message);
        this.promoDiscount.set(result.isValid ? result.discountAmount : 0);
        this.isValidatingPromo.set(false);
        if (result.isValid) this.toast.success(result.message);
        else this.toast.error(result.message);
      },
      error: () => { this.isValidatingPromo.set(false); this.promoValid.set(false); }
    });
  }

  clearPromo() {
    this.bookingForm.patchValue({ promoCode: '' });
    this.promoValid.set(null);
    this.promoMessage.set('');
    this.promoDiscount.set(0);
  }

  topUp() {
    if (this.topUpForm.invalid) return;
    this.isToppingUp.set(true);
    this.walletService.topUp({ amount: this.topUpForm.value.amount! }).subscribe({
      next: w => {
        this.walletInfo.set(w);
        this.toast.success(`₹${this.topUpForm.value.amount} added to wallet!`);
        this.topUpForm.reset({ amount: 500 });
        this.showTopUp.set(false);
        this.isToppingUp.set(false);
      },
      error: () => this.isToppingUp.set(false)
    });
  }

  // Called when clicking "Confirm & Proceed to Payment" on Step 2
  createReservation() {
    const v = this.bookingForm.value;
    const checkIn = v.checkInDate as Date;

    // Basic client-side validation
    if (!checkIn || !v.checkOutDate || !v.roomTypeId) {
      this.toast.error('Please complete all required fields.');
      return;
    }

    // Validate rooms don't exceed available
    const rt = this.selectedRoomType();
    if (rt && (v.numberOfRooms ?? 0) > rt.availableRooms) {
      this.toast.error(`Only ${rt.availableRooms} room(s) available for this type.`);
      return;
    }

    this.isBooking.set(true);
    this.bookingService.createReservation({
      hotelId:           v.hotelId!,
      roomTypeId:        v.roomTypeId!,
      checkInDate:       this.fmtLocal(checkIn),
      checkOutDate:      this.fmtLocal(v.checkOutDate as Date),
      numberOfRooms:     v.numberOfRooms!,
      promoCodeUsed:     this.promoValid() ? v.promoCode ?? undefined : undefined,
      walletAmountToUse: this.walletUsedAmount(),
    }).subscribe({
      next: res => {
        this.createdReservation.set(res);
        this.isBooking.set(false);
        this.toast.success('Reservation created! Complete payment within 10 minutes.');
        setTimeout(() => this.stepper?.next(), 100);
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

  /** Parse a YYYY-MM-DD string as a LOCAL date (avoids UTC midnight timezone shift) */
  private parseLocalDate(s: string): Date {
    const [y, m, d] = s.split('-').map(Number);
    return new Date(y, m - 1, d);
  }

  /** Format a Date as YYYY-MM-DD using LOCAL date parts (avoids UTC shift) */
  private fmtLocal(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  get checkOutMin(): Date {
    const ci = this.bookingForm.get('checkInDate')?.value as Date | null;
    if (!ci) return this.tomorrow;
    const d = new Date(ci); d.setDate(d.getDate() + 1);
    return d;
  }
}
