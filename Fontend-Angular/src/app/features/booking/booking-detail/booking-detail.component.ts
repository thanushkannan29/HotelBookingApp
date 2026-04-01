import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { DatePipe, DecimalPipe } from '@angular/common';
import { BookingService } from '../../../core/services/booking.service';
import { TransactionService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { QrPaymentResponseDto, ReservationDetailsDto } from '../../../core/models/models';

import { environment } from '../../../../environments/environment';

declare var Razorpay: any;

@Component({
  selector: 'app-booking-detail',
  standalone: true,
  imports: [
    CommonModule, RouterLink, ReactiveFormsModule, DatePipe, DecimalPipe,
    MatButtonModule, MatIconModule, MatFormFieldModule,
    MatInputModule, MatCardModule, MatProgressSpinnerModule, MatChipsModule,
    MatDividerModule,
  ],
  templateUrl: './booking-detail.component.html',
  styleUrl: './booking-detail.component.scss'
})
export class BookingDetailComponent implements OnInit, OnDestroy {
  private route              = inject(ActivatedRoute);
  private bookingService     = inject(BookingService);
  private transactionService = inject(TransactionService);
  private toast              = inject(ToastService);
  private fb                 = inject(FormBuilder);

  reservation    = signal<ReservationDetailsDto | null>(null);
  showCancelForm = signal(false);
  showPayPanel   = signal(false);
  isCancelling   = signal(false);
  isDownloading  = signal(false);
  isPaying       = signal(false);
  qrPayment      = signal<QrPaymentResponseDto | null>(null);

  // Countdown timer for pending reservations
  timeLeft       = signal<string>('');
  isExpired      = signal(false);
  private timer: any;

  cancelForm = this.fb.group({
    reason: ['', [Validators.required, Validators.minLength(5)]],
  });

  ngOnInit() {
    const code = this.route.snapshot.paramMap.get('code') ?? '';
    this.bookingService.getReservationByCode(code).subscribe(r => {
      this.reservation.set(r);
      if (r.status === 'Pending' && r.expiryTime) {
        this.startCountdown(new Date(r.expiryTime));
        // Pre-load QR so it's ready when guest opens pay panel
        this.bookingService.getPaymentQr(r.reservationId).subscribe({
          next: qr => this.qrPayment.set(qr),
          error: () => {}
        });
      }
    });
    this.loadRazorpay();
  }

  ngOnDestroy() {
    if (this.timer) clearInterval(this.timer);
  }

  private loadRazorpay() {
    if (typeof Razorpay !== 'undefined') return;
    const script = document.createElement('script');
    script.src = 'https://checkout.razorpay.com/v1/checkout.js';
    script.async = true;
    document.head.appendChild(script);
  }

  private startCountdown(expiry: Date) {
    const tick = () => {
      const now = new Date().getTime();
      const diff = expiry.getTime() - now;
      if (diff <= 0) {
        this.timeLeft.set('Expired — awaiting system cancellation');
        this.isExpired.set(true);
        clearInterval(this.timer);
        // Do NOT locally mutate status — backend is source of truth.
        // The cleanup service will cancel it within 5 minutes.
        return;
      }
      const mins = Math.floor(diff / 60000);
      const secs = Math.floor((diff % 60000) / 1000);
      this.timeLeft.set(`${mins}m ${secs}s`);
    };
    tick();
    this.timer = setInterval(tick, 1000);
  }

  canPayNow(res: ReservationDetailsDto): boolean {
    // Show Pay Now as long as backend status is still Pending —
    // even if the frontend timer has expired, the backend may not have cancelled yet.
    return res.status === 'Pending';
  }

  openPayPanel() {
    this.showPayPanel.set(true);
  }

  payWithRazorpay(methodId: number = 3) {
    const res = this.reservation();
    if (!res) return;

    // Wallet-only: finalAmount is 0
    if (res.finalAmount <= 0) {
      this.confirmPayment(res.reservationId, 5);
      return;
    }

    const amountPaise = Math.round(res.finalAmount * 100);
    const upiId = this.qrPayment()?.upiId ?? res.upiId ?? '';

    const options: any = {
      key: environment.razorpayKeyId,
      amount: amountPaise,
      currency: 'INR',
      name: '🏨 Thanush StayHub',
      description: `Booking: ${res.reservationCode} — ${res.hotelName}`,
      notes: { reservationCode: res.reservationCode },
      theme: { color: '#2d3a8c' },
      handler: () => this.confirmPayment(res.reservationId, methodId),
      modal: {
        ondismiss: () => {
          this.bookingService.recordFailedPayment(res.reservationId).subscribe();
          this.toast.error('Payment cancelled. You can retry.');
        }
      }
    };

    if (methodId === 3 && upiId) {
      options.prefill = { method: 'upi', vpa: upiId };
    } else if (methodId === 4) {
      options.prefill = { method: 'netbanking' };
    } else if (methodId === 1 || methodId === 2) {
      options.prefill = { method: 'card' };
    }

    try {
      const rzp = new Razorpay(options);
      rzp.on('payment.failed', (r: any) => {
        this.bookingService.recordFailedPayment(res.reservationId).subscribe();
        this.toast.error(`Payment failed: ${r.error?.description ?? 'Unknown error'}`);
      });
      rzp.open();
    } catch {
      this.toast.error('Razorpay failed to load. Please try again.');
    }
  }

  payManualUpi() {
    const res = this.reservation();
    if (!res) return;
    this.confirmPayment(res.reservationId, 3);
  }

  private confirmPayment(reservationId: string, methodId: number) {
    this.isPaying.set(true);
    this.transactionService.createPayment({ reservationId, paymentMethod: methodId }).subscribe({
      next: () => {
        this.isPaying.set(false);
        this.toast.success('Payment successful! Booking confirmed.');
        this.reservation.update(r => r ? { ...r, status: 'Confirmed' } : r);
        this.showPayPanel.set(false);
        if (this.timer) clearInterval(this.timer);
      },
      error: () => {
        this.isPaying.set(false);
        this.toast.error('Confirmation failed. Contact support.');
      }
    });
  }

  cancel() {
    if (this.cancelForm.invalid) { this.cancelForm.markAllAsTouched(); return; }
    this.isCancelling.set(true);
    const res = this.reservation()!;
    this.bookingService.cancelReservation(res.reservationCode, {
      reason: this.cancelForm.get('reason')!.value!,
    }).subscribe({
      next: () => {
        this.toast.success('Reservation cancelled. Refund will be credited to your wallet if applicable.');
        this.reservation.update(r => r ? { ...r, status: 'Cancelled' } : r);
        this.showCancelForm.set(false);
        this.isCancelling.set(false);
        if (this.timer) clearInterval(this.timer);
      },
      error: () => this.isCancelling.set(false),
    });
  }

  async downloadPdf() {
    const res = this.reservation();
    if (!res) return;
    this.isDownloading.set(true);
    try {
      const { default: jsPDF } = await import('jspdf');
      const doc = new jsPDF({ unit: 'mm', format: 'a4' });
      const W = 210, margin = 18;

      // ── Header band ──────────────────────────────────────────────────────
      doc.setFillColor(45, 58, 140);
      doc.rect(0, 0, W, 36, 'F');
      doc.setTextColor(255, 255, 255);
      doc.setFontSize(22); doc.setFont('helvetica', 'bold');
      doc.text('Thanush StayHub', margin, 16);
      doc.setFontSize(10); doc.setFont('helvetica', 'normal');
      doc.text('Booking Confirmation', margin, 24);
      doc.setFontSize(9);
      doc.text(`Generated: ${new Date().toLocaleString('en-IN')}`, W - margin, 24, { align: 'right' });

      // ── Status badge ─────────────────────────────────────────────────────
      const statusColor: Record<string, [number,number,number]> = {
        Confirmed: [46,125,50], Pending: [245,127,23],
        Cancelled: [198,40,40], Completed: [21,101,192], NoShow: [97,97,97]
      };
      const [r, g, b] = statusColor[res.status] ?? [97,97,97];
      doc.setFillColor(r, g, b);
      doc.roundedRect(W - margin - 32, 8, 32, 10, 2, 2, 'F');
      doc.setFontSize(9); doc.setFont('helvetica', 'bold'); doc.setTextColor(255,255,255);
      doc.text(res.status.toUpperCase(), W - margin - 16, 14.5, { align: 'center' });

      let y = 46;
      doc.setTextColor(30, 30, 30);

      // ── Reservation code ─────────────────────────────────────────────────
      doc.setFillColor(240, 242, 255);
      doc.rect(margin, y - 5, W - margin * 2, 14, 'F');
      doc.setFontSize(11); doc.setFont('helvetica', 'bold');
      doc.text('Reservation Code:', margin + 3, y + 3);
      doc.setFontSize(13); doc.setTextColor(45, 58, 140);
      doc.text(res.reservationCode, margin + 52, y + 3);
      doc.setTextColor(30, 30, 30);
      y += 20;

      // ── Two-column layout: Hotel | Stay Details ───────────────────────────
      const col1 = margin, col2 = W / 2 + 4;

      const sectionHeader = (label: string, xPos: number, yPos: number) => {
        doc.setFontSize(9); doc.setFont('helvetica', 'bold');
        doc.setTextColor(45, 58, 140);
        doc.text(label.toUpperCase(), xPos, yPos);
        doc.setDrawColor(45, 58, 140);
        doc.line(xPos, yPos + 1, xPos + 80, yPos + 1);
        doc.setTextColor(30, 30, 30);
      };

      const row = (label: string, value: string, xPos: number, yPos: number) => {
        doc.setFontSize(9); doc.setFont('helvetica', 'normal'); doc.setTextColor(100,100,100);
        doc.text(label, xPos, yPos);
        doc.setFont('helvetica', 'bold'); doc.setTextColor(30,30,30);
        doc.text(value, xPos + 28, yPos);
      };

      sectionHeader('Hotel Details', col1, y);
      sectionHeader('Stay Details', col2, y);
      y += 7;

      row('Hotel:', res.hotelName, col1, y);
      row('Check-in:', new Date(res.checkInDate).toLocaleDateString('en-IN', { day:'2-digit', month:'short', year:'numeric' }), col2, y);
      y += 7;
      row('Room Type:', res.roomTypeName, col1, y);
      row('Check-out:', new Date(res.checkOutDate).toLocaleDateString('en-IN', { day:'2-digit', month:'short', year:'numeric' }), col2, y);
      y += 7;
      row('Rooms:', String(res.numberOfRooms), col1, y);
      const nights = Math.round((new Date(res.checkOutDate).getTime() - new Date(res.checkInDate).getTime()) / 86400000);
      row('Nights:', String(nights), col2, y);
      y += 7;
      if (res.cancellationFeePaid) {
        doc.setFontSize(8); doc.setFont('helvetica', 'normal'); doc.setTextColor(46,125,50);
        doc.text('🛡 Cancellation Protection Active', col1, y);
        doc.setTextColor(30,30,30);
        y += 6;
      }
      y += 6;

      // ── Price Breakdown ───────────────────────────────────────────────────
      sectionHeader('Price Breakdown', col1, y); y += 8;

      const priceRow = (label: string, amount: string, color?: [number,number,number]) => {
        doc.setFontSize(9); doc.setFont('helvetica', 'normal');
        doc.setTextColor(color ? color[0] : 60, color ? color[1] : 60, color ? color[2] : 60);
        doc.text(label, col1, y);
        doc.text(amount, W - margin, y, { align: 'right' });
        doc.setTextColor(30,30,30);
        y += 7;
      };

      priceRow('Base Amount', `Rs. ${res.totalAmount.toFixed(2)}`);
      if (res.gstAmount > 0) priceRow(`GST (${res.gstPercent}%)`, `Rs. ${res.gstAmount.toFixed(2)}`);
      if (res.discountAmount > 0) priceRow('Promo Discount', `-Rs. ${res.discountAmount.toFixed(2)}`, [46,125,50]);
      if (res.walletAmountUsed > 0) priceRow('Wallet Used', `-Rs. ${res.walletAmountUsed.toFixed(2)}`, [46,125,50]);
      if ((res as any).cancellationFeeAmount > 0) priceRow('Cancellation Protection', `+Rs. ${(res as any).cancellationFeeAmount.toFixed(2)}`, [21,101,192]);

      // Total line
      doc.setDrawColor(200,200,200); doc.line(col1, y - 2, W - margin, y - 2);
      doc.setFillColor(45, 58, 140);
      doc.rect(col1, y, W - margin * 2, 10, 'F');
      doc.setFontSize(11); doc.setFont('helvetica', 'bold'); doc.setTextColor(255,255,255);
      doc.text('Total Amount Paid', col1 + 3, y + 7);
      doc.text(`Rs. ${res.finalAmount.toFixed(2)}`, W - margin - 3, y + 7, { align: 'right' });
      y += 18;

      // ── Booked on ─────────────────────────────────────────────────────────
      doc.setFontSize(8); doc.setFont('helvetica', 'normal'); doc.setTextColor(120,120,120);
      doc.text(`Booked on: ${new Date(res.createdDate).toLocaleString('en-IN')}`, col1, y);
      y += 12;

      // ── Footer ────────────────────────────────────────────────────────────
      doc.setFillColor(240, 242, 255);
      doc.rect(0, 277, W, 20, 'F');
      doc.setFontSize(8); doc.setTextColor(45, 58, 140);
      doc.text('Thank you for choosing Thanush StayHub! For support: support@thanushstayhub.in', W / 2, 285, { align: 'center' });
      doc.setTextColor(120,120,120);
      doc.text('This is a computer-generated document. No signature required.', W / 2, 291, { align: 'center' });

      doc.save(`ThanushStayHub-Booking-${res.reservationCode}.pdf`);
      this.toast.success('PDF downloaded!');
    } catch (e) {
      this.toast.error('PDF generation failed. Ensure jsPDF is installed: npm install jspdf');
    }
    this.isDownloading.set(false);
  }

  statusClass(status: string): string {
    const map: Record<string, string> = {
      Pending: 'badge-warning', Confirmed: 'badge-success',
      Completed: 'badge-primary', Cancelled: 'badge-error', NoShow: 'badge-muted',
    };
    return map[status] ?? 'badge-muted';
  }

  canCancel(res: ReservationDetailsDto): boolean {
    return res.status === 'Pending' || res.status === 'Confirmed';
  }

  getRefundPreview(res: ReservationDetailsDto): string {
    const checkIn = new Date(res.checkInDate);
    const today = new Date(); today.setHours(0,0,0,0); checkIn.setHours(0,0,0,0);
    const days = Math.round((checkIn.getTime() - today.getTime()) / 86400000);

    // After check-in or stay already passed — no refund
    if (res.isCheckedIn || days < 0) {
      return `No refund — reservation already checked in or stay has passed`;
    }

    if (res.cancellationFeePaid) {
      // With protection: full refund before check-in day, 50% on check-in day
      if (days > 0) return `Full refund of ₹${res.totalAmount.toFixed(2)} — protection active, cancelled before check-in day`;
      return `50% refund of ₹${(res.totalAmount * 0.5).toFixed(2)} — cancelled on check-in day (protection provides partial refund)`;
    }

    // Without protection — industry-standard tiered policy
    if (days >= 7) return `Full refund of ₹${res.totalAmount.toFixed(2)} — free cancellation, 7+ days before check-in`;
    if (days >= 3) return `50% refund of ₹${(res.totalAmount * 0.5).toFixed(2)} — cancelled 3–6 days before check-in`;
    if (days >= 1) return `25% refund of ₹${(res.totalAmount * 0.25).toFixed(2)} — cancelled 1–2 days before check-in`;
    return `No refund — cancelled on check-in day`;
  }
}
