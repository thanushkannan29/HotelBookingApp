import { Component, inject, signal, computed, OnInit, AfterViewInit, ViewChild } from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatSliderModule } from '@angular/material/slider';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSelectModule } from '@angular/material/select';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { DatePipe } from '@angular/common';
import { AmenityResponseDto, HotelListItemDto } from '../../../core/models/models';
import { HotelService } from '../../../core/services/hotel.service';
import { HotelCardComponent } from '../hotel-card/hotel-card.component';
import { CityAutocompleteComponent } from '../../../shared/components/city-autocomplete/city-autocomplete.component';
import { InfiniteCarouselComponent } from '../../../shared/components/infinite-carousel/infinite-carousel.component';

@Component({
  selector: 'app-hotel-list',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatDatepickerModule, MatNativeDateModule,
    MatSliderModule, MatCheckboxModule, MatPaginatorModule,
    HotelCardComponent, CityAutocompleteComponent, DatePipe,
    InfiniteCarouselComponent,
  ],
  templateUrl: './hotel-list.component.html',
  styleUrl: './hotel-list.component.scss'
})
export class HotelListComponent implements OnInit, AfterViewInit {
  private hotelService = inject(HotelService);
  private fb = inject(FormBuilder);

  topHotels = signal<HotelListItemDto[]>([]);
  searchResults = signal<HotelListItemDto[] | null>(null);
  cityGroups = signal<{ cityName: string; hotels: HotelListItemDto[] }[]>([]);
  stateGroups = signal<{ stateName: string; hotels: HotelListItemDto[] }[]>([]);
  isSearching = signal(false);
  totalResults = signal(0);
  currentPage = signal(1);
  readonly pageSize = 9;

  // F3E: Filter signals
  minPrice = signal(0);
  maxPrice = signal(50000);
  minRating = signal(0);
  selectedAmenities = signal<string[]>([]);
  amenities = signal<string[]>([]);
  sortBy = signal('');
  amenityObjects = signal<any[]>([]);

  // Paginated results
  paginatedResults = signal<HotelListItemDto[]>([]);

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  // City and State controls for search
  cityControl = new FormControl('');
  stateControl = new FormControl('');

  searchForm = this.fb.group({
    checkIn: [null as Date | null],
    checkOut: [null as Date | null],
  });

  today = new Date();

  get tomorrow(): Date {
    const d = new Date(); d.setHours(0, 0, 0, 0); d.setDate(d.getDate() + 1); return d;
  }

  // F3E: client-side filtered results
  filteredResults = computed(() => {
    const results = this.searchResults() ?? [];
    return results.filter(h =>
      (h.startingPrice ?? 0) >= this.minPrice() &&
      (h.startingPrice ?? 0) <= this.maxPrice() &&
      h.averageRating >= this.minRating()
    );
  });

  ngOnInit() {
    this.hotelService.getTopHotels().subscribe(hotels => this.topHotels.set(hotels));
    this.hotelService.getAmenities().subscribe(a => {
      this.amenities.set(a.map(x => x.name));
      this.amenityObjects.set(a);
    });
    this.loadStateGroups();
  }

  ngAfterViewInit() {
    if (this.paginator) {
      this.paginator.page.subscribe(() => {
        this.updatePaginatedResults();
      });
    }
  }

  private loadStateGroups() {
    this.hotelService.getActiveStates().subscribe({
      next: states => {
        if (states.length === 0) { this.loadCityGroups(); return; }
        const limited = states.slice(0, 6);
        forkJoin(
          limited.map(state =>
            this.hotelService.getHotelsByState(state).pipe(catchError(() => of([])))
          )
        ).subscribe(results => {
          const groups = limited
            .map((state, i) => ({ stateName: state, hotels: results[i] as HotelListItemDto[] }))
            .filter(g => g.hotels.length > 0)
            .sort((a, b) => a.stateName.localeCompare(b.stateName));
          if (groups.length === 0) {
            this.loadCityGroups();
          } else {
            this.stateGroups.set(groups);
          }
        });
      },
      error: () => this.loadCityGroups()
    });
  }

  private loadCityGroups() {
    this.hotelService.getCities().subscribe(cities => {
      const limited = cities.slice(0, 5);
      forkJoin(
        limited.map(city =>
          this.hotelService.getHotelsByCity(city).pipe(catchError(() => of([])))
        )
      ).subscribe(results => {
        const groups = limited
          .map((city, i) => ({ cityName: city, hotels: results[i] as HotelListItemDto[] }))
          .filter(g => g.hotels.length > 0);
        this.cityGroups.set(groups);
      });
    });
  }

  search() {
    const city = this.cityControl.value?.trim();
    const state = this.stateControl.value?.trim();
    const { checkIn, checkOut } = this.searchForm.value;
    if ((!city && !state) || !checkIn || !checkOut) return;

    this.isSearching.set(true);
    this.currentPage.set(1);

    this.hotelService.searchHotelsWithFilters({
      city: city || undefined,
      state: state || undefined,
      checkIn: this.formatDate(checkIn!),
      checkOut: this.formatDate(checkOut!),
      pageNumber: 1,
      pageSize: 100,
      amenityIds: this.selectedAmenities().length > 0 ? this.selectedAmenities() : undefined,
      minPrice: this.minPrice() > 0 ? this.minPrice() : undefined,
      maxPrice: this.maxPrice() < 50000 ? this.maxPrice() : undefined,
      sortBy: this.sortBy() || undefined,
    }).subscribe({
      next: res => {
        this.searchResults.set(res.hotels);
        this.totalResults.set(res.recordsCount);
        this.isSearching.set(false);
        this.updatePaginatedResults();
      },
      error: () => this.isSearching.set(false),
    });
  }

   updatePaginatedResults() {
    const all = this.filteredResults();
    const start = (this.paginator?.pageIndex ?? 0) * this.pageSize;
    this.paginatedResults.set(all.slice(start, start + this.pageSize));
  }

  applyFilters() {
    if (this.paginator) this.paginator.firstPage();
    this.updatePaginatedResults();
  }

  clearSearch() {
    this.searchResults.set(null);
    this.cityControl.reset();
    this.stateControl.reset();
    this.searchForm.reset();
    this.minPrice.set(0);
    this.maxPrice.set(50000);
    this.minRating.set(0);
  }

  toggleAmenity(amenityId: string) {
    const current = this.selectedAmenities();
    if (current.includes(amenityId)) {
      this.selectedAmenities.set(current.filter(a => a !== amenityId));
    } else {
      this.selectedAmenities.set([...current, amenityId]);
    }
    this.search();
  }

  private formatDate(d: Date): string {
    return d.toISOString().split('T')[0];
  }

  get hasMoreResults(): boolean {
    return (this.searchResults()?.length ?? 0) < this.totalResults();
  }
}