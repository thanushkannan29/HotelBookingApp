import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { DatePipe, DecimalPipe } from '@angular/common';
import { HotelService } from '../../../core/services/hotel.service';
import { ToastService } from '../../../core/services/toast.service';
import { SuperAdminHotelListDto } from '../../../core/models/models';

@Component({
  selector: 'app-hotel-control',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, DatePipe, DecimalPipe],
  templateUrl: './hotel-control.component.html',
  styleUrl: './hotel-control.component.scss'
})
export class HotelControlComponent implements OnInit {
  private hotelService = inject(HotelService);
  private toast = inject(ToastService);

  hotels = signal<SuperAdminHotelListDto[]>([]);
  filterMode = signal<'all' | 'active' | 'blocked'>('all');

  ngOnInit() {
    this.hotelService.getAllHotelsForSuperAdmin().subscribe(h => this.hotels.set(h));
  }

  block(hotel: SuperAdminHotelListDto) {
    if (!confirm(`Block ${hotel.name}? This will prevent the admin from activating it.`)) return;
    this.hotelService.blockHotel(hotel.hotelId).subscribe(() => {
      this.toast.success(`${hotel.name} blocked.`);
      this.hotels.update(h => h.map(x => x.hotelId === hotel.hotelId
        ? { ...x, isBlockedBySuperAdmin: true, isActive: false } : x));
    });
  }

  unblock(hotel: SuperAdminHotelListDto) {
    this.hotelService.unblockHotel(hotel.hotelId).subscribe(() => {
      this.toast.success(`${hotel.name} unblocked.`);
      this.hotels.update(h => h.map(x => x.hotelId === hotel.hotelId
        ? { ...x, isBlockedBySuperAdmin: false } : x));
    });
  }

  get filtered(): SuperAdminHotelListDto[] {
    const m = this.filterMode();
    if (m === 'active')  return this.hotels().filter(h => h.isActive && !h.isBlockedBySuperAdmin);
    if (m === 'blocked') return this.hotels().filter(h => h.isBlockedBySuperAdmin);
    return this.hotels();
  }
}
