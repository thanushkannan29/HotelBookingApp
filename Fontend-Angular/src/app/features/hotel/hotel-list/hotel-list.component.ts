import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { DatePipe } from '@angular/common';
import { HotelService } from '../../../core/services/hotel.service';
import { HotelListItemDto } from '../../../core/models/models';
import { HotelCardComponent } from '../hotel-card/hotel-card.component';

@Component({
  selector: 'app-hotel-list',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatDatepickerModule, MatNativeDateModule,
    HotelCardComponent, DatePipe
  ],
  templateUrl: './hotel-list.component.html',
  styleUrl: './hotel-list.component.scss'
})
export class HotelListComponent implements OnInit {
  private hotelService = inject(HotelService);
  private fb = inject(FormBuilder);
  private router = inject(Router);

  topHotels = signal<HotelListItemDto[]>([]);
  searchResults = signal<HotelListItemDto[] | null>(null);
  cities = signal<string[]>([]);
  isSearching = signal(false);
  totalResults = signal(0);
  currentPage = signal(1);
  readonly pageSize = 9;

  searchForm = this.fb.group({
    city: [''],
    checkIn: [null as Date | null],
    checkOut: [null as Date | null],
  });

  today = new Date();

  ngOnInit() {
    this.hotelService.getTopHotels().subscribe(hotels => this.topHotels.set(hotels));
    this.hotelService.getCities().subscribe(cities => this.cities.set(cities));
  }

  search() {
    const { city, checkIn, checkOut } = this.searchForm.value;
    if (!city || !checkIn || !checkOut) return;

    this.isSearching.set(true);
    this.currentPage.set(1);

    const req = {
      city,
      checkIn: this.formatDate(checkIn!),
      checkOut: this.formatDate(checkOut!),
      pageNumber: 1,
      pageSize: this.pageSize,
    };

    this.hotelService.searchHotels(req).subscribe({
      next: res => {
        this.searchResults.set(res.hotels);
        this.totalResults.set(res.recordsCount);
        this.isSearching.set(false);
      },
      error: () => this.isSearching.set(false),
    });
  }

  loadMore() {
    const { city, checkIn, checkOut } = this.searchForm.value;
    if (!city || !checkIn || !checkOut) return;
    const nextPage = this.currentPage() + 1;

    this.hotelService.searchHotels({
      city,
      checkIn: this.formatDate(checkIn!),
      checkOut: this.formatDate(checkOut!),
      pageNumber: nextPage,
      pageSize: this.pageSize,
    }).subscribe(res => {
      this.searchResults.update(prev => [...(prev ?? []), ...res.hotels]);
      this.currentPage.set(nextPage);
    });
  }

  clearSearch() {
    this.searchResults.set(null);
    this.searchForm.reset();
  }

  private formatDate(d: Date): string {
    return d.toISOString().split('T')[0];
  }

  get hasMoreResults(): boolean {
    return (this.searchResults()?.length ?? 0) < this.totalResults();
  }

  get displayHotels(): HotelListItemDto[] {
    return this.searchResults() ?? this.topHotels();
  }
}
