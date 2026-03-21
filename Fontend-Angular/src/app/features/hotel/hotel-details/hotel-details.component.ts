import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatDividerModule } from '@angular/material/divider';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { DatePipe, DecimalPipe } from '@angular/common';
import { HotelService } from '../../../core/services/hotel.service';
import { AuthService } from '../../../core/services/auth.service';
import {
  HotelDetailsDto, RoomAvailabilityDto
} from '../../../core/models/models';

@Component({
  selector: 'app-hotel-details',
  standalone: true,
  imports: [
    RouterLink, ReactiveFormsModule,
    MatButtonModule, MatIconModule,
    MatTabsModule, MatDividerModule,
    MatDatepickerModule, MatNativeDateModule,
    MatFormFieldModule, MatInputModule,
    DatePipe, DecimalPipe
  ],
  templateUrl: './hotel-details.component.html',
  styleUrl: './hotel-details.component.scss'
})
export class HotelDetailsComponent implements OnInit {
  private route        = inject(ActivatedRoute);
  private hotelService = inject(HotelService);
  private fb           = inject(FormBuilder);
  auth                 = inject(AuthService);

  hotel        = signal<HotelDetailsDto | null>(null);
  availability = signal<RoomAvailabilityDto[]>([]);
  isLoadingAvail = signal(false);
  hotelId      = '';
  today        = new Date();

  dateForm = this.fb.group({
    checkIn:  [null as Date | null],
    checkOut: [null as Date | null],
  });

  get checkOutMin(): Date {
    const ci = this.dateForm.get('checkIn')?.value as Date | null;
    if (!ci) return this.today;
    const d = new Date(ci); d.setDate(d.getDate() + 1); return d;
  }

  ngOnInit() {
    this.hotelId = this.route.snapshot.paramMap.get('id') ?? '';
    this.hotelService.getHotelDetails(this.hotelId).subscribe(h => this.hotel.set(h));

    // Default to today + 2 days
    const ci = new Date();
    const co = new Date(); co.setDate(co.getDate() + 2);
    this.dateForm.patchValue({ checkIn: ci, checkOut: co });
    this.loadAvailability(ci, co);

    // Reload when dates change
    this.dateForm.get('checkIn')?.valueChanges.subscribe(() => this.onDateChange());
    this.dateForm.get('checkOut')?.valueChanges.subscribe(() => this.onDateChange());
  }

  private onDateChange() {
    const ci = this.dateForm.get('checkIn')?.value as Date | null;
    const co = this.dateForm.get('checkOut')?.value as Date | null;
    if (ci && co && co > ci) this.loadAvailability(ci, co);
  }

  private loadAvailability(ci: Date, co: Date) {
    this.isLoadingAvail.set(true);
    const ciStr = ci.toISOString().split('T')[0];
    const coStr = co.toISOString().split('T')[0];
    this.hotelService.getAvailability(this.hotelId, ciStr, coStr).subscribe(a => {
      // Deduplicate — keep entry with min available rooms (most conservative)
      const map = new Map<string, RoomAvailabilityDto>();
      for (const item of a) {
        const ex = map.get(item.roomTypeId);
        if (!ex || item.availableRooms < ex.availableRooms)
          map.set(item.roomTypeId, item);
      }
      this.availability.set(Array.from(map.values()));
      this.isLoadingAvail.set(false);
    });
  }

  get checkInStr(): string {
    const ci = this.dateForm.get('checkIn')?.value as Date | null;
    return ci ? ci.toISOString().split('T')[0] : '';
  }
  get checkOutStr(): string {
    const co = this.dateForm.get('checkOut')?.value as Date | null;
    return co ? co.toISOString().split('T')[0] : '';
  }
  get totalNights(): number {
    const ci = this.dateForm.get('checkIn')?.value as Date | null;
    const co = this.dateForm.get('checkOut')?.value as Date | null;
    if (!ci || !co) return 0;
    return Math.max(0, (co.getTime() - ci.getTime()) / 86400000);
  }
  stars = [1,2,3,4,5];
}
