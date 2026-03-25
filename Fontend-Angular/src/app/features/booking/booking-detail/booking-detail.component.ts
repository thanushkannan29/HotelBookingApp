import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DatePipe, DecimalPipe } from '@angular/common';
import { BookingService } from '../../../core/services/booking.service';
import { ToastService } from '../../../core/services/toast.service';
import { ReservationDetailsDto } from '../../../core/models/models';

@Component({
  selector: 'app-booking-detail',
  standalone: true,
  imports: [
    RouterLink, ReactiveFormsModule, DatePipe, DecimalPipe,
    MatButtonModule, MatIconModule, MatFormFieldModule,
    MatInputModule, MatCardModule, MatProgressSpinnerModule
  ],
  templateUrl: './booking-detail.component.html',
  styleUrl: './booking-detail.component.scss'
})
export class BookingDetailComponent implements OnInit {
  private route          = inject(ActivatedRoute);
  private bookingService = inject(BookingService);
  private toast          = inject(ToastService);
  private fb             = inject(FormBuilder);

  reservation    = signal<ReservationDetailsDto | null>(null);
  showCancelForm = signal(false);
  isCancelling   = signal(false);
  isDownloading  = signal(false);

  cancelForm = this.fb.group({
    reason: ['', [Validators.required, Validators.minLength(5)]],
  });

  ngOnInit() {
    const code = this.route.snapshot.paramMap.get('code') ?? '';
    this.bookingService.getReservationByCode(code).subscribe(r => this.reservation.set(r));
  }

  cancel() {
    if (this.cancelForm.invalid) { this.cancelForm.markAllAsTouched(); return; }
    this.isCancelling.set(true);
    const res = this.reservation()!;
    this.bookingService.cancelReservation(res.reservationCode, {
      reason: this.cancelForm.get('reason')!.value!,
    }).subscribe({
      next: () => {
        this.toast.success('Reservation cancelled. Refund request created if applicable.');
        this.reservation.update(r => r ? { ...r, status: 'Cancelled' } : r);
        this.showCancelForm.set(false);
        this.isCancelling.set(false);
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
      const doc = new jsPDF();
      let y = 20;

      doc.setFontSize(18);
      doc.text('StayHub - Booking Confirmation', 20, y); y += 12;

      doc.setFontSize(12);
      doc.text(`Reservation Code: ${res.reservationCode}`, 20, y); y += 8;
      doc.text(`Hotel: ${res.hotelName}`, 20, y); y += 8;
      doc.text(`Room Type: ${res.roomTypeName}`, 20, y); y += 8;
      doc.text(`Check-in: ${res.checkInDate}`, 20, y); y += 8;
      doc.text(`Check-out: ${res.checkOutDate}`, 20, y); y += 8;
      doc.text(`Rooms: ${res.numberOfRooms}`, 20, y); y += 8;
      doc.text(`Status: ${res.status}`, 20, y); y += 12;

      doc.setFontSize(13);
      doc.text('Price Breakdown', 20, y); y += 8;
      doc.setFontSize(11);
      doc.text(`Base Amount: Rs.${res.totalAmount.toFixed(2)}`, 20, y); y += 7;
      if (res.gstAmount > 0) {
        doc.text(`GST (${res.gstPercent}%): Rs.${res.gstAmount.toFixed(2)}`, 20, y); y += 7;
      }
      if (res.discountAmount > 0) {
        doc.text(`Discount: -Rs.${res.discountAmount.toFixed(2)}`, 20, y); y += 7;
      }
      if (res.walletAmountUsed > 0) {
        doc.text(`Wallet Used: -Rs.${res.walletAmountUsed.toFixed(2)}`, 20, y); y += 7;
      }
      doc.setFontSize(13);
      doc.text(`Final Amount: Rs.${res.finalAmount.toFixed(2)}`, 20, y); y += 10;
      doc.setFontSize(10);
      doc.text(`Booked on: ${new Date(res.createdDate).toLocaleDateString()}`, 20, y);

      doc.save(`booking-${res.reservationCode}.pdf`);
      this.toast.success('PDF downloaded!');
    } catch {
      this.toast.error('PDF generation failed. Run: npm install jspdf');
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
}
