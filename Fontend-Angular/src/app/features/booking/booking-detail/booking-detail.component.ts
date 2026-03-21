import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { DatePipe, DecimalPipe } from '@angular/common';
import { BookingService } from '../../../core/services/booking.service';
import { ToastService } from '../../../core/services/toast.service';
import { ReservationDetailsDto } from '../../../core/models/models';

@Component({
  selector: 'app-booking-detail',
  standalone: true,
  imports: [
    RouterLink, ReactiveFormsModule, DatePipe, DecimalPipe,
    MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule
  ],
  templateUrl: './booking-detail.component.html',
  styleUrl: './booking-detail.component.scss'
})
export class BookingDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private bookingService = inject(BookingService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  reservation = signal<ReservationDetailsDto | null>(null);
  showCancelForm = signal(false);
  isCancelling = signal(false);

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
