import { Component, inject, signal, computed, OnInit, AfterViewInit, ViewChild } from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
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
        if (states.length === 0) {
          // No states set yet — fall back to city-based groups
          this.loadCityGroups();
          return;
        }
        const limited = states.slice(0, 6);
        const groups: { stateName: string; hotels: HotelListItemDto[] }[] = [];
        let loaded = 0;
        for (const state of limited) {
          this.hotelService.getHotelsByState(state).subscribe(hotels => {
            if (hotels.length > 0) groups.push({ stateName: state, hotels: hotels.slice(0, 10) });
            loaded++;
            if (loaded === limited.length) {
              if (groups.length === 0) {
                this.loadCityGroups();
              } else {
                this.stateGroups.set(groups.sort((a, b) => a.stateName.localeCompare(b.stateName)));
              }
            }
          });
        }
      },
      error: () => this.loadCityGroups()
    });
  }

  private loadCityGroups() {
    this.hotelService.getCities().subscribe(cities => {
      const limited = cities.slice(0, 5);
      const groups: { cityName: string; hotels: HotelListItemDto[] }[] = [];
      let loaded = 0;
      for (const city of limited) {
        this.hotelService.getHotelsByCity(city).subscribe(hotels => {
          groups.push({ cityName: city, hotels: hotels.slice(0, 10) });
          loaded++;
          if (loaded === limited.length) {
            this.cityGroups.set(groups);
          }
        });
      }
    });
  }

  search() {
    const city = this.cityControl.value;
    const { checkIn, checkOut } = this.searchForm.value;
    if (!city || !checkIn || !checkOut) return;

    this.isSearching.set(true);
    this.currentPage.set(1);

    this.hotelService.searchHotelsWithFilters({
      city,
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
        this.hotelService.getAmenities().subscribe(a => {
          this.amenities.set(a.map(x => x.name));
          this.amenityObjects.set(a);
        });
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