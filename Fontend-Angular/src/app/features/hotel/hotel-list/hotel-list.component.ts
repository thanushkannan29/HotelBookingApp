import { Component, inject, signal, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule } from '@angular/forms';
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
import { MatPaginatorModule, MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
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
    MatSliderModule, MatCheckboxModule, MatPaginatorModule, MatProgressSpinnerModule,
    HotelCardComponent, CityAutocompleteComponent, DatePipe,
    InfiniteCarouselComponent,
  ],
  templateUrl: './hotel-list.component.html',
  styleUrl: './hotel-list.component.scss'
})
export class HotelListComponent implements OnInit, OnDestroy {
  private hotelService = inject(HotelService);
  private fb = inject(FormBuilder);

  // ── Hero slideshow ────────────────────────────────────────────────────────
  readonly heroSlides = [
    { url: 'https://images.unsplash.com/photo-1566073771259-6a8506099945?w=1600&q=80&auto=format&fit=crop' },
    { url: 'https://images.unsplash.com/photo-1542314831-068cd1dbfeeb?w=1600&q=80&auto=format&fit=crop' },
    { url: 'https://images.unsplash.com/photo-1520250497591-112f2f40a3f4?w=1600&q=80&auto=format&fit=crop' },
    { url: 'https://images.unsplash.com/photo-1551882547-ff40c63fe5fa?w=1600&q=80&auto=format&fit=crop' },
    { url: 'https://images.unsplash.com/photo-1571003123894-1f0594d2b5d9?w=1600&q=80&auto=format&fit=crop' },
  ];
  activeSlide = signal(0);
  private slideInterval?: ReturnType<typeof setInterval>;

  topHotels     = signal<HotelListItemDto[]>([]);
  searchResults = signal<HotelListItemDto[] | null>(null);
  cityGroups    = signal<{ cityName: string; hotels: HotelListItemDto[] }[]>([]);
  stateGroups   = signal<{ stateName: string; hotels: HotelListItemDto[] }[]>([]);
  isSearching   = signal(false);
  totalResults  = signal(0);
  readonly pageSize = 9;
  currentPage   = 1;

  // Filters
  minPrice          = signal(0);
  maxPrice          = signal(50000);
  selectedAmenities = signal<string[]>([]);
  amenityObjects    = signal<AmenityResponseDto[]>([]);
  sortBy            = signal('');

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  cityControl  = new FormControl('');
  stateControl = new FormControl('');

  searchForm = this.fb.group({
    checkIn:  [null as Date | null],
    checkOut: [null as Date | null],
  });

  today = new Date();

  get tomorrow(): Date {
    const d = new Date(); d.setHours(0, 0, 0, 0); d.setDate(d.getDate() + 1); return d;
  }

  ngOnInit() {
    this.hotelService.getTopHotels().subscribe(hotels => this.topHotels.set(hotels));
    this.hotelService.getAmenities().subscribe(a => this.amenityObjects.set(a));
    this.loadStateGroups();

    // Start hero slideshow — rotate every 5 seconds
    this.slideInterval = setInterval(() => {
      this.activeSlide.update(i => (i + 1) % this.heroSlides.length);
    }, 5000);
  }

  ngOnDestroy() {
    clearInterval(this.slideInterval);
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
          if (groups.length === 0) this.loadCityGroups();
          else this.stateGroups.set(groups);
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

  // ── SEARCH ────────────────────────────────────────────────────────────────
  search(page = 1) {
    const city  = this.cityControl.value?.trim();
    const state = this.stateControl.value?.trim();
    const { checkIn, checkOut } = this.searchForm.value;
    if ((!city && !state) || !checkIn || !checkOut) return;

    this.isSearching.set(true);
    this.currentPage = page;

    this.hotelService.searchHotelsWithFilters({
      city:       city  || undefined,
      state:      state || undefined,
      checkIn:    this.fmt(checkIn!),
      checkOut:   this.fmt(checkOut!),
      pageNumber: page,
      pageSize:   this.pageSize,
      amenityIds: this.selectedAmenities().length > 0 ? this.selectedAmenities() : undefined,
      minPrice:   this.minPrice() > 0       ? this.minPrice() : undefined,
      maxPrice:   this.maxPrice() < 50000   ? this.maxPrice() : undefined,
      sortBy:     this.sortBy() || undefined,
    }).subscribe({
      next: res => {
        this.searchResults.set(res.hotels);
        this.totalResults.set(res.totalCount ?? res.recordsCount);
        this.isSearching.set(false);
      },
      error: () => { this.isSearching.set(false); this.searchResults.set([]); },
    });
  }

  onPage(e: PageEvent) {
    this.search(e.pageIndex + 1);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  applyFilters() {
    this.paginator?.firstPage();
    this.search(1);
  }

  toggleAmenity(amenityId: string) {
    const current = this.selectedAmenities();
    this.selectedAmenities.set(
      current.includes(amenityId)
        ? current.filter(a => a !== amenityId)
        : [...current, amenityId]
    );
    this.paginator?.firstPage();
    this.search(1);
  }

  clearSearch() {
    this.searchResults.set(null);
    this.totalResults.set(0);
    this.currentPage = 1;
    this.cityControl.reset();
    this.stateControl.reset();
    this.searchForm.reset();
    this.minPrice.set(0);
    this.maxPrice.set(50000);
    this.selectedAmenities.set([]);
    this.sortBy.set('');
    this.paginator?.firstPage();
  }

  private fmt(d: Date): string { return d.toISOString().split('T')[0]; }
}
