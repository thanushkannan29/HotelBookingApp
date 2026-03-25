import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
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
import { DecimalPipe } from '@angular/common';
import { BookingService } from '../../../core/services/booking.service';
import { TransactionService } from '../../../core/services/api.services';
import { HotelService } from '../../../core/services/hotel.service';
import { ToastService } from '../../../core/services/toast.service';
import {
  HotelDetailsDto, RoomAvailabilityDto, AvailableRoomDto,
  ReservationResponseDto, PaymentMethod, PaymentIntentDto
} from '../../../core/models/models';

@Component({
  selector: 'app-booking-create',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink, DecimalPipe,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatStepperModule,
    MatRadioModule, MatDatepickerModule, MatNativeDateModule,
    MatProgressSpinnerModule, MatCardModule,
  ],
  templateUrl: './booking-create.component.html',
  styleUrl: './booking-create.component.scss'
})
export class BookingCreateComponent implements OnInit {
  private fb                  = inject(FormBuilder);
  private route               = inject(ActivatedRoute);
  private router              = inject(Router);
  private bookingService      = inject(BookingService);
  private transactionService  = inject(TransactionService);
  private hotelService        = inject(HotelService);
  private toast               = inject(ToastService);

  hotel             = signal<HotelDetailsDto | null>(null);
  availability      = signal<RoomAvailabilityDto[]>([]);
  availableRooms    = signal<AvailableRoomDto[]>([]);
  createdReservation= signal<ReservationResponseDto | null>(null);
  paymentIntent     = signal<PaymentIntentDto | null>(null);  // F4D
  isLoadingHotel    = signal(true);
  isBooking         = signal(false);
  isPaying          = signal(false);
  today             = new Date();

  paymentMethods = Object.entries(PaymentMethod).map(([k, v]) => ({ id: +k, label: v }));

  bookingForm = this.fb.group({
    hotelId:       ['', Validators.required],
    roomTypeId:    ['', Validators.required],
    checkInDate:   [null as Date | null, Validators.required],
    checkOutDate:  [null as Date | null, Validators.required],
    numberOfRooms: [1, [Validators.required, Validators.min(1)]],
  });

  paymentForm = this.fb.group({
    paymentMethod: [1, Validators.required],
  });

  selectedRoomType = computed(() => {
    const rtId = this.bookingForm.get('roomTypeId')?.value;
    return this.availability().find(a => a.roomTypeId === rtId);
  });

  // F4B: Math.floor for whole numbers
  totalNights = computed(() => {
    const ci = this.bookingForm.get('checkInDate')?.value as Date | null;
    const co = this.bookingForm.get('checkOutDate')?.value as Date | null;
    if (!ci || !co) return 0;
    const diff = Math.floor((co.getTime() - ci.getTime()) / 86400000);
    return Math.max(0, diff);
  });

  estimatedTotal = computed(() => {
    const rt    = this.selectedRoomType();
    const rooms = this.bookingForm.get('numberOfRooms')?.value ?? 1;
    return (rt?.pricePerNight ?? 0) * this.totalNights() * rooms;
  });

  ngOnInit() {
    const p = this.route.snapshot.queryParams;
    const checkIn  = p['checkIn']  ? new Date(p['checkIn'])  : null;
    const checkOut = p['checkOut'] ? new Date(p['checkOut']) : null;

    this.bookingForm.patchValue({
      hotelId:    p['hotelId']    ?? '',
      roomTypeId: p['roomTypeId'] ?? '',
      checkInDate:  checkIn,
      checkOutDate: checkOut,
    });

    if (p['hotelId']) {
      this.hotelService.getHotelDetails(p['hotelId']).subscribe(h => {
        this.hotel.set(h);
        this.isLoadingHotel.set(false);
      });
      if (checkIn && checkOut) {
        this.loadAvailability(p['hotelId'], checkIn, checkOut);
      } else {
        this.isLoadingHotel.set(false);
      }
    } else {
      this.isLoadingHotel.set(false);
    }

    this.bookingForm.get('checkInDate')?.valueChanges.subscribe(() => this.onDateChange());
    this.bookingForm.get('checkOutDate')?.valueChanges.subscribe(() => this.onDateChange());

    // F4A: Fix roomTypeId valueChanges not triggering onRoomTypeChange
    this.bookingForm.get('roomTypeId')?.valueChanges.subscribe(rtId => {
      if (rtId) this.onRoomTypeChange(rtId);
    });
  }

  private onDateChange() {
    const { hotelId, checkInDate, checkOutDate } = this.bookingForm.value;
    if (hotelId && checkInDate && checkOutDate) {
      this.loadAvailability(hotelId, checkInDate as Date, checkOutDate as Date);
    }
  }

  private loadAvailability(hotelId: string, ci: Date, co: Date) {
    const ciStr = ci.toISOString().split('T')[0];
    const coStr = co.toISOString().split('T')[0];
    this.hotelService.getAvailability(hotelId, ciStr, coStr).subscribe(a => {
      const map = new Map<string, RoomAvailabilityDto>();
      for (const item of a) {
        const ex = map.get(item.roomTypeId);
        if (!ex || item.availableRooms < ex.availableRooms)
          map.set(item.roomTypeId, item);
      }
      this.availability.set(Array.from(map.values()));
    });
  }

  onRoomTypeChange(rtId: string) {
    const { hotelId, checkInDate, checkOutDate } = this.bookingForm.value;
    if (hotelId && checkInDate && checkOutDate) {
      const ci = (checkInDate as Date).toISOString().split('T')[0];
      const co = (checkOutDate as Date).toISOString().split('T')[0];
      this.bookingService.getAvailableRooms(hotelId!, rtId, ci, co)
        .subscribe(rooms => this.availableRooms.set(rooms));
    }
  }

  private fmt(d: Date): string { return d.toISOString().split('T')[0]; }

  createReservation() {
    if (this.bookingForm.invalid) { this.bookingForm.markAllAsTouched(); return; }
    const v = this.bookingForm.value;
    this.isBooking.set(true);
    this.bookingService.createReservation({
      hotelId:       v.hotelId!,
      roomTypeId:    v.roomTypeId!,
      checkInDate:   this.fmt(v.checkInDate as Date),
      checkOutDate:  this.fmt(v.checkOutDate as Date),
      numberOfRooms: v.numberOfRooms!,
    }).subscribe({
      next: res => {
        this.createdReservation.set(res);
        this.isBooking.set(false);
        this.toast.success('Reservation created! Pay within 10 minutes to confirm.');
        // F4D: Load payment intent for UPI details
        this.transactionService.getPaymentIntent(res.reservationId).subscribe({
          next: intent => this.paymentIntent.set(intent),
          error: () => {} // non-fatal
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

  get checkOutMin(): Date {
    const ci = this.bookingForm.get('checkInDate')?.value as Date | null;
    if (!ci) return this.today;
    const d = new Date(ci);
    d.setDate(d.getDate() + 1);
    return d;
  }
}