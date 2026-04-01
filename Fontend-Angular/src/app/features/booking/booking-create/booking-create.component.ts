import { Component, inject, signal, OnInit, OnDestroy, computed, ViewChild, effect } from '@angular/core';
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
  ReservationResponseDto, ReservationDetailsDto, QrPaymentResponseDto, WalletResponseDto
} from '../../../core/models/models';

import { environment } from '../../../../environments/environment';

// Razorpay type declaration
declare var Razorpay: any;

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
export class BookingCreateComponent implements OnInit, OnDestroy {
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
  payCancellationFee = signal(false);
  resumeTimeLeft     = signal('');
  resumeExpired      = signal(false);
  private resumeTimer: any;

  toggleWallet(checked: boolean) {
    this.useWallet.set(checked);
    if (checked) {
      const balance = this.walletInfo()?.balance ?? 0;
      const maxUsable = Math.max(0, this.baseTotal() + this.gstAmount() + this.cancellationFeeAmount() - this.promoDiscount());
      const autoAmount = Math.min(balance, maxUsable);
      this.bookingForm.patchValue({ walletAmount: autoAmount });
      this.walletAmountSignal.set(autoAmount);
    } else {
      this.bookingForm.patchValue({ walletAmount: 0 });
      this.walletAmountSignal.set(0);
    }
  }
  get today(): Date {
    const d = new Date(); d.setHours(0, 0, 0, 0); return d;
  }
  get tomorrow(): Date {
    const d = new Date(); d.setHours(0, 0, 0, 0); d.setDate(d.getDate() + 1); return d;
  }

  // Reactive signals for form values that drive computed totals
  selectedRoomTypeId = signal<string>('');
  numberOfRooms      = signal<number>(1);
  checkInDate        = signal<Date | null>(null);
  checkOutDate       = signal<Date | null>(null);
  walletAmountSignal = signal<number>(0);

  bookingForm = this.fb.group({
    hotelId:       ['', Validators.required],
    roomTypeId:    ['', Validators.required],
    checkInDate:   [null as Date | null, Validators.required],
    checkOutDate:  [null as Date | null, Validators.required],
    numberOfRooms: [1, [Validators.required, Validators.min(1), Validators.max(10)]],
    promoCode:     [''],
    walletAmount:  [0, [Validators.min(0)]],
  });

  topUpForm = this.fb.group({
    amount: [500, [Validators.required, Validators.min(1), Validators.max(100000)]]
  });

  selectedRoomType = computed(() => {
    const rtId = this.selectedRoomTypeId();
    return this.availability().find(a => a.roomTypeId === rtId);
  });

  totalNights = computed(() => {
    const ci = this.checkInDate();
    const co = this.checkOutDate();
    if (!ci || !co) return 0;
    const ciMid = new Date(ci); ciMid.setHours(0,0,0,0);
    const coMid = new Date(co); coMid.setHours(0,0,0,0);
    return Math.max(0, Math.round((coMid.getTime() - ciMid.getTime()) / 86400000));
  });

  baseTotal = computed(() => {
    const rt    = this.selectedRoomType();
    const rooms = this.numberOfRooms();
    return (rt?.pricePerNight ?? 0) * this.totalNights() * rooms;
  });

  gstPercent = computed(() => this.hotel()?.gstPercent ?? 0);
  gstAmount  = computed(() => Math.round(this.baseTotal() * this.gstPercent() / 100 * 100) / 100);

  maxRooms = computed(() => {
    const rt = this.selectedRoomType();
    return rt ? Math.min(rt.availableRooms, 10) : 10;
  });

  walletUsedAmount = computed(() => {
    if (!this.useWallet()) return 0;
    const entered = this.walletAmountSignal();
    const balance = this.walletInfo()?.balance ?? 0;
    const maxUsable = Math.max(0, this.baseTotal() + this.gstAmount() + this.cancellationFeeAmount() - this.promoDiscount());
    return Math.min(entered, balance, maxUsable);
  });

  cancellationFeeAmount = computed(() =>
    this.payCancellationFee() ? Math.round(this.baseTotal() * 0.10 * 100) / 100 : 0
  );

  finalTotal = computed(() =>
    Math.max(0, this.baseTotal() + this.gstAmount() - this.promoDiscount() - this.walletUsedAmount() + this.cancellationFeeAmount())
  );

  // True when wallet covers the full remaining amount
  walletCoversAll = computed(() =>
    this.useWallet() && this.finalTotal() === 0 && this.baseTotal() > 0
  );

  step1Valid = computed(() =>
    !!this.selectedRoomTypeId() &&
    !!this.checkInDate() &&
    !!this.checkOutDate()
  );

  ngOnInit() {
    const p = this.route.snapshot.queryParams;

    // ── RESUME MODE: returning from booking-detail to complete payment ──
    const resumeCode = p['resume'];
    if (resumeCode) {
      this.isLoadingHotel.set(true);
      this.bookingService.getReservationByCode(resumeCode).subscribe({
        next: details => {
          // Populate hotel info
          this.hotelService.getHotelDetails(details.hotelId).subscribe(h => {
            this.hotel.set(h);
            this.isLoadingHotel.set(false);
          });
          // Map ReservationDetailsDto → ReservationResponseDto shape
          const res: ReservationResponseDto = {
            reservationCode: details.reservationCode,
            reservationId:   details.reservationId,
            totalAmount:     details.totalAmount,
            gstPercent:      details.gstPercent,
            gstAmount:       details.gstAmount,
            discountPercent: details.discountPercent,
            discountAmount:  details.discountAmount,
            walletAmountUsed: details.walletAmountUsed,
            finalAmount:     details.finalAmount,
            status:          details.status,
            totalRooms:      details.numberOfRooms,
            rooms:           details.rooms,
          };
          this.createdReservation.set(res);
          // Restore signals so the countdown and expiry work
          if (details.expiryTime) {
            this.startResumeCountdown(new Date(details.expiryTime));
          }
          // Load QR for payment
          this.bookingService.getPaymentQr(details.reservationId).subscribe({
            next: qr => this.qrPayment.set(qr),
            error: () => {}
          });
          // Jump to step 3 after view init
          setTimeout(() => {
            this.stepper?.steps.forEach((_, i) => {
              if (i < 2) this.stepper.steps.get(i)!.completed = true;
            });
            this.stepper?.selectedIndex !== 2 && (this.stepper.selectedIndex = 2);
          }, 200);
        },
        error: () => this.isLoadingHotel.set(false)
      });
      this.loadWallet();
      this.loadRazorpay();
      return;
    }
    let checkIn  = p['checkIn']  ? this.parseLocalDate(p['checkIn'])  : null;
    let checkOut = p['checkOut'] ? this.parseLocalDate(p['checkOut']) : null;

    // Auto-advance past/today dates to tomorrow
    const todayLocal = new Date(); todayLocal.setHours(0,0,0,0);
    if (checkIn) {
      const ci = new Date(checkIn); ci.setHours(0,0,0,0);
      if (ci <= todayLocal) {
        const origCheckIn = new Date(checkIn);
        checkIn = new Date(todayLocal); checkIn.setDate(checkIn.getDate() + 1);
        if (checkOut) {
          const nights = Math.round((checkOut.getTime() - origCheckIn.getTime()) / 86400000);
          checkOut = new Date(checkIn); checkOut.setDate(checkOut.getDate() + Math.max(1, nights));
        }
      }
    }
    // Also ensure checkout is after checkin and not in the past
    if (checkOut) {
      const co = new Date(checkOut); co.setHours(0,0,0,0);
      const minCo = checkIn ? new Date(checkIn) : new Date(todayLocal);
      minCo.setDate(minCo.getDate() + 1);
      if (co <= minCo) {
        checkOut = new Date(minCo);
      }
    }

    const roomTypeId = p['roomTypeId'] ?? '';
    this.bookingForm.patchValue({
      hotelId:      p['hotelId'] ?? '',
      roomTypeId,
      checkInDate:  checkIn,
      checkOutDate: checkOut,
    });

    // Sync signals
    this.selectedRoomTypeId.set(roomTypeId);
    this.checkInDate.set(checkIn);
    this.checkOutDate.set(checkOut);

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
    this.loadRazorpay();

    // Track whether initial patch has been applied to avoid double API call on init
    let initDone = false;
    setTimeout(() => { initDone = true; }, 0);

    // Sync form → signals for reactive computed
    this.bookingForm.get('checkInDate')?.valueChanges.subscribe(v => {
      this.checkInDate.set(v as Date | null);
      if (initDone) this.onDateChange();
    });
    this.bookingForm.get('checkOutDate')?.valueChanges.subscribe(v => {
      this.checkOutDate.set(v as Date | null);
      if (initDone) this.onDateChange();
    });
    this.bookingForm.get('numberOfRooms')?.valueChanges.subscribe(v => {
      this.numberOfRooms.set(v ?? 1);
    });
    this.bookingForm.get('walletAmount')?.valueChanges.subscribe(v => {
      this.walletAmountSignal.set(v ?? 0);
    });

    this.bookingForm.get('roomTypeId')?.valueChanges
      .pipe(distinctUntilChanged())
      .subscribe(rtId => {
        this.selectedRoomTypeId.set(rtId ?? '');
        this.availableRooms.set([]);
        this.promoValid.set(null);
        this.promoMessage.set('');
        this.promoDiscount.set(0);
        if (rtId) this.onRoomTypeChange(rtId);
      });
  }

  private loadRazorpay() {
    if (typeof Razorpay !== 'undefined') return;
    const script = document.createElement('script');
    script.src = 'https://checkout.razorpay.com/v1/checkout.js';
    script.async = true;
    document.head.appendChild(script);
  }

  loadWallet() {
    this.walletService.getWallet(1, 1).subscribe({
      next: data => this.walletInfo.set(data.wallet),
      error: () => {}
    });
  }

  private onDateChange() {
    const { hotelId, checkInDate, checkOutDate } = this.bookingForm.value;
    if (hotelId && checkInDate && checkOutDate) {
      this.loadAvailability(hotelId, checkInDate as Date, checkOutDate as Date);
      // Update URL with new dates
      this.updateUrl();
    }
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

  private updateUrl() {
    const { hotelId, roomTypeId, checkInDate, checkOutDate } = this.bookingForm.value;
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        hotelId,
        roomTypeId: roomTypeId || null,
        checkIn:  checkInDate ? this.fmtLocal(checkInDate as Date) : null,
        checkOut: checkOutDate ? this.fmtLocal(checkOutDate as Date) : null,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }

  selectRoomType(rtId: string) {
    this.bookingForm.patchValue({ roomTypeId: rtId });
    this.selectedRoomTypeId.set(rtId);
    this.availableRooms.set([]);
    this.promoValid.set(null);
    this.promoMessage.set('');
    this.promoDiscount.set(0);
    this.onRoomTypeChange(rtId);
    this.updateUrl();

    // Cap numberOfRooms to available
    const rt = this.availability().find(a => a.roomTypeId === rtId);
    if (rt) {
      const maxAvail = Math.min(rt.availableRooms, 10);
      const current = this.bookingForm.get('numberOfRooms')?.value ?? 1;
      this.bookingForm.get('numberOfRooms')?.setValidators([
        Validators.required, Validators.min(1), Validators.max(maxAvail)
      ]);
      this.bookingForm.get('numberOfRooms')?.updateValueAndValidity();
      if (current > maxAvail) {
        this.bookingForm.patchValue({ numberOfRooms: maxAvail });
        this.numberOfRooms.set(maxAvail);
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
    const amount = this.topUpForm.value.amount!;
    const amountPaise = Math.round(amount * 100);

    const options: any = {
      key: environment.razorpayKeyId,
      amount: amountPaise,
      currency: 'INR',
      name: '🏨 Thanush StayHub',
      description: `Wallet Top-up — ₹${amount}`,
      image: 'https://i.imgur.com/n5tjHFD.png',
      theme: { color: '#2d3a8c' },
      handler: () => {
        this.isToppingUp.set(true);
        this.walletService.topUp({ amount }).subscribe({
          next: w => {
            this.walletInfo.set(w);
            this.toast.success(`₹${amount} added to wallet!`);
            this.topUpForm.reset({ amount: 500 });
            this.showTopUp.set(false);
            this.isToppingUp.set(false);
          },
          error: () => this.isToppingUp.set(false)
        });
      },
      modal: { ondismiss: () => this.toast.error('Payment cancelled.') }
    };

    try {
      const rzp = new Razorpay(options);
      rzp.open();
    } catch {
      this.toast.error('Razorpay failed to load. Please try again.');
    }
  }

  createReservation() {
    const v = this.bookingForm.value;
    const checkIn = v.checkInDate as Date;

    if (!checkIn || !v.checkOutDate || !v.roomTypeId) {
      this.toast.error('Please complete all required fields.');
      return;
    }

    const rt = this.selectedRoomType();
    if (rt && (v.numberOfRooms ?? 0) > rt.availableRooms) {
      this.toast.error(`Only ${rt.availableRooms} room(s) available for this type.`);
      return;
    }

    this.isBooking.set(true);
    this.bookingService.createReservation({
      hotelId:              v.hotelId!,
      roomTypeId:           v.roomTypeId!,
      checkInDate:          this.fmtLocal(checkIn),
      checkOutDate:         this.fmtLocal(v.checkOutDate as Date),
      numberOfRooms:        v.numberOfRooms!,
      promoCodeUsed:        this.promoValid() ? v.promoCode ?? undefined : undefined,
      walletAmountToUse:    this.walletUsedAmount(),
      payCancellationFee:   this.payCancellationFee(),
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

  // ── RAZORPAY PAYMENT ──────────────────────────────────────────────────────
  payWithRazorpay(paymentMethodId: number = 3) {
    const res = this.createdReservation();
    if (!res) return;

    // If wallet covers everything, skip Razorpay entirely
    if (this.walletCoversAll() || paymentMethodId === 5) {
      this.confirmWalletOnlyPayment(res);
      return;
    }

    const amountPaise = Math.round(res.finalAmount * 100);
    const hotelName = this.hotel()?.name ?? 'Thanush StayHub';
    const upiId = this.qrPayment()?.upiId ?? '';

    // Map payment method to Razorpay method string
    const rzpMethodMap: Record<number, string> = {
      1: 'card',      // CreditCard
      2: 'card',      // DebitCard
      3: 'upi',       // UPI
      4: 'netbanking' // NetBanking
    };

    const options: any = {
      key: environment.razorpayKeyId,
      amount: amountPaise,
      currency: 'INR',
      name: '🏨 Thanush StayHub',
      description: `Booking: ${res.reservationCode} — ${hotelName}`,
      image: 'https://i.imgur.com/n5tjHFD.png',
      prefill: { name: '', email: '', contact: '' },
      notes: { reservationCode: res.reservationCode, hotelName },
      theme: { color: '#2d3a8c' },
      handler: (response: any) => {
        this.isPaying.set(true);
        this.transactionService.createPayment({
          reservationId: res.reservationId,
          paymentMethod: paymentMethodId,
        }).subscribe({
          next: () => {
            this.isPaying.set(false);
            this.toast.success('Payment successful! Booking confirmed.');
            this.router.navigate(['/booking', res.reservationCode]);
          },
          error: () => {
            this.isPaying.set(false);
            this.toast.error('Payment recorded but confirmation failed. Contact support.');
          }
        });
      },
      modal: {
        ondismiss: () => {
          this.bookingService.recordFailedPayment(res.reservationId).subscribe();
          this.toast.error('Payment cancelled. Retry from My Bookings.');
        }
      }
    };

    // Pre-select payment method in Razorpay
    if (paymentMethodId === 3 && upiId) {
      options.prefill.method = 'upi';
      options.prefill.vpa = upiId;
    } else if (paymentMethodId === 4) {
      options.prefill.method = 'netbanking';
    }

    try {
      const rzp = new Razorpay(options);
      rzp.on('payment.failed', (response: any) => {
        this.bookingService.recordFailedPayment(res.reservationId).subscribe();
        this.toast.error(`Payment failed: ${response.error?.description ?? 'Unknown error'}. Retry from My Bookings.`);
      });
      rzp.open();
    } catch {
      this.toast.error('Razorpay failed to load. Please try again.');
    }
  }

  private confirmWalletOnlyPayment(res: ReservationResponseDto) {
    this.isPaying.set(true);
    // Wallet already deducted at reservation creation — just record as success
    this.transactionService.createPayment({
      reservationId: res.reservationId,
      paymentMethod: 5, // Wallet
    }).subscribe({
      next: () => {
        this.isPaying.set(false);
        this.toast.success('Paid fully from wallet! Booking confirmed.');
        this.router.navigate(['/booking', res.reservationCode]);
      },
      error: () => this.isPaying.set(false)
    });
  }

  // Manual UPI confirm (after scanning QR)
  payManual() {
    this.payManualWith(3);
  }

  payManualWith(paymentMethodId: number) {
    const res = this.createdReservation();
    if (!res) return;
    this.isPaying.set(true);
    this.transactionService.createPayment({
      reservationId: res.reservationId,
      paymentMethod: paymentMethodId,
    }).subscribe({
      next: () => {
        this.isPaying.set(false);
        this.toast.success('Payment confirmed! Booking complete.');
        this.router.navigate(['/booking', res.reservationCode]);
      },
      error: () => this.isPaying.set(false),
    });
  }

  private startResumeCountdown(expiry: Date) {
    const tick = () => {
      const diff = expiry.getTime() - Date.now();
      if (diff <= 0) {
        this.resumeTimeLeft.set('Expired');
        this.resumeExpired.set(true);
        clearInterval(this.resumeTimer);
        return;
      }
      const mins = Math.floor(diff / 60000);
      const secs = Math.floor((diff % 60000) / 1000);
      this.resumeTimeLeft.set(`${mins}m ${secs}s`);
    };
    tick();
    this.resumeTimer = setInterval(tick, 1000);
  }

  ngOnDestroy() {
    if (this.resumeTimer) clearInterval(this.resumeTimer);
  }

  private parseLocalDate(s: string): Date {
    const [y, m, d] = s.split('-').map(Number);
    return new Date(y, m - 1, d);
  }

  fmtLocal(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  get checkOutMin(): Date {
    const ci = this.checkInDate();
    const minDate = new Date(this.tomorrow);
    if (!ci) return minDate;
    const d = new Date(ci); d.setDate(d.getDate() + 1);
    // Return whichever is later — day after check-in or tomorrow
    return d > minDate ? d : minDate;
  }
}
