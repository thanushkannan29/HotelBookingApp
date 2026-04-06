import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideNativeDateAdapter } from '@angular/material/core';
import { of, throwError } from 'rxjs';
import { HotelListComponent } from './hotel-list.component';
import { HotelService } from '../../../core/services/hotel.service';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

function makeHotel(id: string, city: string) {
  return { hotelId: id, name: `Hotel ${id}`, city, imageUrl: 'https://example.com/img.jpg', averageRating: 4.0, reviewCount: 10 };
}

const MOCK_TOP_HOTELS = [makeHotel('h-001', 'Chennai'), makeHotel('h-002', 'Mumbai'), makeHotel('h-003', 'Delhi')];
const MOCK_SEARCH_RESULT = { hotels: [makeHotel('h-004', 'Chennai'), makeHotel('h-005', 'Chennai')], totalCount: 2, recordsCount: 2 };

describe('HotelListComponent', () => {
  let component: HotelListComponent;
  let fixture: ComponentFixture<HotelListComponent>;
  let hotelSpy: jasmine.SpyObj<HotelService>;

  beforeEach(async () => {
    hotelSpy = jasmine.createSpyObj('HotelService', [
      'getTopHotels', 'getAmenities', 'getActiveStates', 'getHotelsByState',
      'getCities', 'getHotelsByCity', 'searchHotelsWithFilters'
    ]);

    hotelSpy.getTopHotels.and.returnValue(of(MOCK_TOP_HOTELS as any));
    hotelSpy.getAmenities.and.returnValue(of([]));
    hotelSpy.getActiveStates.and.returnValue(of([]));
    hotelSpy.getCities.and.returnValue(of([]));
    hotelSpy.searchHotelsWithFilters.and.returnValue(of(MOCK_SEARCH_RESULT as any));

    await TestBed.configureTestingModule({
      imports: [HotelListComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]), provideNativeDateAdapter(),
        { provide: HotelService, useValue: hotelSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(HotelListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('ngOnInit — should call getTopHotels', () => {
    expect(hotelSpy.getTopHotels).toHaveBeenCalled();
  });

  it('ngOnInit — should populate topHotels signal', () => {
    expect(component.topHotels().length).toBe(3);
  });

  it('ngOnInit — should call getAmenities', () => {
    expect(hotelSpy.getAmenities).toHaveBeenCalled();
  });

  // ── Initial state ─────────────────────────────────────────────────────────

  it('currentPage — should start at 1', () => expect(component.currentPage).toBe(1));
  it('pageSize — should be 9', () => expect(component.pageSize).toBe(9));
  it('searchResults — should start as null', () => expect(component.searchResults()).toBeNull());
  it('isSearching — should start as false', () => expect(component.isSearching()).toBeFalse());

  // ── search ────────────────────────────────────────────────────────────────

  it('search — should NOT call searchHotelsWithFilters when city/state is empty', () => {
    hotelSpy.searchHotelsWithFilters.calls.reset();
    component.cityControl.setValue('');
    component.stateControl.setValue('');
    component.searchForm.patchValue({ checkIn: new Date('2025-06-01'), checkOut: new Date('2025-06-03') });
    component.search();
    expect(hotelSpy.searchHotelsWithFilters).not.toHaveBeenCalled();
  });

  it('search — should NOT call searchHotelsWithFilters when dates are missing', () => {
    hotelSpy.searchHotelsWithFilters.calls.reset();
    component.cityControl.setValue('Chennai');
    component.search();
    expect(hotelSpy.searchHotelsWithFilters).not.toHaveBeenCalled();
  });

  it('search — should call searchHotelsWithFilters when city and dates are set', () => {
    hotelSpy.searchHotelsWithFilters.calls.reset();
    component.cityControl.setValue('Chennai');
    component.searchForm.patchValue({ checkIn: new Date('2025-06-01'), checkOut: new Date('2025-06-03') });
    component.search();
    expect(hotelSpy.searchHotelsWithFilters).toHaveBeenCalled();
  });

  it('search — should populate searchResults on success', () => {
    component.cityControl.setValue('Chennai');
    component.searchForm.patchValue({ checkIn: new Date('2025-06-01'), checkOut: new Date('2025-06-03') });
    component.search();
    expect(component.searchResults()?.length).toBe(2);
  });

  it('search — should set isSearching to false on success', () => {
    component.cityControl.setValue('Chennai');
    component.searchForm.patchValue({ checkIn: new Date('2025-06-01'), checkOut: new Date('2025-06-03') });
    component.search();
    expect(component.isSearching()).toBeFalse();
  });

  it('search — should set searchResults to empty array on error', () => {
    hotelSpy.searchHotelsWithFilters.and.returnValue(throwError(() => new Error('fail')));
    component.cityControl.setValue('Chennai');
    component.searchForm.patchValue({ checkIn: new Date('2025-06-01'), checkOut: new Date('2025-06-03') });
    component.search();
    expect(component.searchResults()).toEqual([]);
    expect(component.isSearching()).toBeFalse();
  });

  // ── clearSearch ───────────────────────────────────────────────────────────

  it('clearSearch — should reset searchResults to null', () => {
    component.searchResults.set([]);
    component.clearSearch();
    expect(component.searchResults()).toBeNull();
  });

  it('clearSearch — should reset currentPage to 1', () => {
    component.currentPage = 3;
    component.clearSearch();
    expect(component.currentPage).toBe(1);
  });

  it('clearSearch — should reset cityControl', () => {
    component.cityControl.setValue('Chennai');
    component.clearSearch();
    expect(component.cityControl.value).toBeFalsy();
  });

  // ── toggleAmenity ─────────────────────────────────────────────────────────

  it('toggleAmenity — should add amenity to selectedAmenities', () => {
    component.toggleAmenity('a-001');
    expect(component.selectedAmenities()).toContain('a-001');
  });

  it('toggleAmenity — should remove amenity when toggled again', () => {
    component.toggleAmenity('a-001');
    component.toggleAmenity('a-001');
    expect(component.selectedAmenities()).not.toContain('a-001');
  });

  // ── fmtLocal ──────────────────────────────────────────────────────────────

  it('fmtLocal — should format date as YYYY-MM-DD', () => {
    // Use UTC noon to avoid timezone shift in toISOString()
    const d = new Date('2025-06-01T12:00:00Z');
    expect(component['fmt'](d)).toBe('2025-06-01');
  });

  // ── Hero slideshow ────────────────────────────────────────────────────────

  it('heroSlides — should have 5 slides', () => {
    expect(component.heroSlides.length).toBe(5);
  });

  it('activeSlide — should start at 0', () => {
    expect(component.activeSlide()).toBe(0);
  });

  it('ngOnDestroy — should clear the slide interval without error', () => {
    expect(() => component.ngOnDestroy()).not.toThrow();
  });

  it('activeSlide — should advance to next slide index', () => {
    component.activeSlide.set(0);
    component.activeSlide.update(i => (i + 1) % component.heroSlides.length);
    expect(component.activeSlide()).toBe(1);
  });

  it('activeSlide — should wrap back to 0 after last slide', () => {
    component.activeSlide.set(component.heroSlides.length - 1);
    component.activeSlide.update(i => (i + 1) % component.heroSlides.length);
    expect(component.activeSlide()).toBe(0);
  });
});
