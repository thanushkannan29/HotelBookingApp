import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { provideNativeDateAdapter } from '@angular/material/core';
import { of, throwError } from 'rxjs';
import { HotelListComponent } from './hotel-list.component';
import { HotelService } from '../../../core/services/hotel.service';
import { HotelListItemDto, SearchHotelResponseDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

// ── Mock data ──────────────────────────────────────────────────────────────────

function makeHotel(id: string, name: string): HotelListItemDto {
  return {
    hotelId:       id,
    name,
    city:          'Chennai',
    imageUrl:      'https://example.com/img.jpg',
    averageRating: 4.5,
    reviewCount:   100,
    startingPrice: 3500,
  };
}

const MOCK_TOP_HOTELS: HotelListItemDto[] = [
  makeHotel('hotel-001', 'Grand Palace'),
  makeHotel('hotel-002', 'Sea View Inn'),
  makeHotel('hotel-003', 'City Lights'),
];

const MOCK_CITIES = ['Chennai', 'Mumbai', 'Bangalore', 'Delhi'];

const MOCK_SEARCH_RESPONSE: SearchHotelResponseDto = {
  hotels:       [makeHotel('hotel-004', 'Search Result 1'), makeHotel('hotel-005', 'Search Result 2')],
  pageNumber:   1,
  recordsCount: 5,
};

// ─────────────────────────────────────────────────────────────────────────────

describe('HotelListComponent', () => {
  let component: HotelListComponent;
  let fixture:   ComponentFixture<HotelListComponent>;
  let hotelSpy:  jasmine.SpyObj<HotelService>;

  beforeEach(async () => {
    hotelSpy = jasmine.createSpyObj('HotelService', [
      'getTopHotels', 'getCities', 'searchHotels'
    ]);

    hotelSpy.getTopHotels.and.returnValue(of(MOCK_TOP_HOTELS));
    hotelSpy.getCities.and.returnValue(of(MOCK_CITIES));
    hotelSpy.searchHotels.and.returnValue(of(MOCK_SEARCH_RESPONSE));

    await TestBed.configureTestingModule({
      imports: [HotelListComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNativeDateAdapter(),
        { provide: HotelService, useValue: hotelSpy },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(HotelListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── CONSTANTS ──────────────────────────────────────────────────────────────

  it('pageSize — should be 9', () => {
    expect(component.pageSize).toBe(9);
  });

  // ── INITIAL SIGNAL STATE ───────────────────────────────────────────────────

  it('searchResults — should start as null', () => {
    expect(component.searchResults()).toBeNull();
  });

  it('isSearching — should start as false', () => {
    expect(component.isSearching()).toBeFalse();
  });

  it('totalResults — should start as 0', () => {
    expect(component.totalResults()).toBe(0);
  });

  it('currentPage — should start at 1', () => {
    expect(component.currentPage()).toBe(1);
  });

  // ── ngOnInit ───────────────────────────────────────────────────────────────

  it('ngOnInit — should call getTopHotels on startup', () => {
    expect(hotelSpy.getTopHotels).toHaveBeenCalledOnceWith();
  });

  it('ngOnInit — should call getCities on startup', () => {
    expect(hotelSpy.getCities).toHaveBeenCalledOnceWith();
  });

  it('ngOnInit — should populate topHotels signal', () => {
    expect(component.topHotels().length).toBe(3);
    expect(component.topHotels()[0].name).toBe('Grand Palace');
  });

  it('ngOnInit — should populate cities signal', () => {
    expect(component.cities().length).toBe(4);
    expect(component.cities()).toContain('Chennai');
    expect(component.cities()).toContain('Mumbai');
  });

  // ── displayHotels GETTER ───────────────────────────────────────────────────

  it('displayHotels — should return topHotels when searchResults is null', () => {
    expect(component.displayHotels.length).toBe(3);
    expect(component.displayHotels[0].name).toBe('Grand Palace');
  });

  it('displayHotels — should return searchResults when they are set', () => {
    component.searchResults.set(MOCK_SEARCH_RESPONSE.hotels);
    expect(component.displayHotels.length).toBe(2);
    expect(component.displayHotels[0].name).toBe('Search Result 1');
  });

  it('displayHotels — should return empty array when searchResults is empty array', () => {
    component.searchResults.set([]);
    expect(component.displayHotels.length).toBe(0);
  });

  // ── hasMoreResults GETTER ──────────────────────────────────────────────────

  it('hasMoreResults — should be false when searchResults is null', () => {
    expect(component.hasMoreResults).toBeFalse();
  });

  it('hasMoreResults — should be true when searchResults.length < totalResults', () => {
    component.searchResults.set(MOCK_SEARCH_RESPONSE.hotels); // 2 results
    component.totalResults.set(5);                           // 5 total
    expect(component.hasMoreResults).toBeTrue();
  });

  it('hasMoreResults — should be false when all results are loaded', () => {
    component.searchResults.set(MOCK_SEARCH_RESPONSE.hotels); // 2 results
    component.totalResults.set(2);                            // 2 total — all loaded
    expect(component.hasMoreResults).toBeFalse();
  });

  // ── search() — HAPPY PATH ──────────────────────────────────────────────────

  it('search() — should call searchHotels with formatted dates and city', () => {
    component.searchForm.patchValue({
      city:     'Chennai',
      checkIn:  new Date('2025-06-01'),
      checkOut: new Date('2025-06-03'),
    });

    component.search();

    expect(hotelSpy.searchHotels).toHaveBeenCalledOnceWith(
      jasmine.objectContaining({
        city:       'Chennai',
        checkIn:    '2025-06-01',
        checkOut:   '2025-06-03',
        pageNumber: 1,
        pageSize:   9,
      })
    );
  });

  it('search() — should populate searchResults signal on success', () => {
    component.searchForm.patchValue({
      city:     'Chennai',
      checkIn:  new Date('2025-06-01'),
      checkOut: new Date('2025-06-03'),
    });

    component.search();

    expect(component.searchResults()?.length).toBe(2);
    expect(component.searchResults()![0].name).toBe('Search Result 1');
  });

  it('search() — should set totalResults from recordsCount', () => {
    component.searchForm.patchValue({
      city: 'Chennai', checkIn: new Date('2025-06-01'), checkOut: new Date('2025-06-03')
    });

    component.search();

    expect(component.totalResults()).toBe(5);
  });

  it('search() — should reset currentPage to 1', () => {
    component.currentPage.set(3);
    component.searchForm.patchValue({
      city: 'Chennai', checkIn: new Date('2025-06-01'), checkOut: new Date('2025-06-03')
    });

    component.search();

    expect(component.currentPage()).toBe(1);
  });

  it('search() — should reset isSearching to false on success', () => {
    component.searchForm.patchValue({
      city: 'Chennai', checkIn: new Date('2025-06-01'), checkOut: new Date('2025-06-03')
    });

    component.search();

    expect(component.isSearching()).toBeFalse();
  });

  // ── search() — INCOMPLETE FORM ─────────────────────────────────────────────

  it('search() — should NOT call searchHotels when city is missing', () => {
    hotelSpy.searchHotels.calls.reset();
    component.searchForm.patchValue({
      city: '', checkIn: new Date('2025-06-01'), checkOut: new Date('2025-06-03')
    });

    component.search();

    expect(hotelSpy.searchHotels).not.toHaveBeenCalled();
  });

  it('search() — should NOT call searchHotels when checkIn is null', () => {
    hotelSpy.searchHotels.calls.reset();
    component.searchForm.patchValue({
      city: 'Chennai', checkIn: null, checkOut: new Date('2025-06-03')
    });

    component.search();

    expect(hotelSpy.searchHotels).not.toHaveBeenCalled();
  });

  it('search() — should NOT call searchHotels when checkOut is null', () => {
    hotelSpy.searchHotels.calls.reset();
    component.searchForm.patchValue({
      city: 'Chennai', checkIn: new Date('2025-06-01'), checkOut: null
    });

    component.search();

    expect(hotelSpy.searchHotels).not.toHaveBeenCalled();
  });

  // ── search() — ERROR ───────────────────────────────────────────────────────

  it('search() — should reset isSearching to false on API error', () => {
    hotelSpy.searchHotels.and.returnValue(throwError(() => new Error('fail')));
    component.searchForm.patchValue({
      city: 'Chennai', checkIn: new Date('2025-06-01'), checkOut: new Date('2025-06-03')
    });

    component.search();

    expect(component.isSearching()).toBeFalse();
  });

  it('search() — should NOT populate searchResults on API error', () => {
    hotelSpy.searchHotels.and.returnValue(throwError(() => new Error('fail')));
    component.searchForm.patchValue({
      city: 'Chennai', checkIn: new Date('2025-06-01'), checkOut: new Date('2025-06-03')
    });

    component.search();

    expect(component.searchResults()).toBeNull();
  });

  // ── clearSearch() ──────────────────────────────────────────────────────────

  it('clearSearch() — should set searchResults to null', () => {
    component.searchResults.set(MOCK_SEARCH_RESPONSE.hotels);

    component.clearSearch();

    expect(component.searchResults()).toBeNull();
  });

  it('clearSearch() — should reset the search form', () => {
    component.searchForm.patchValue({
      city: 'Chennai', checkIn: new Date('2025-06-01'), checkOut: new Date('2025-06-03')
    });

    component.clearSearch();

    expect(component.searchForm.get('city')?.value).toBeFalsy();
    expect(component.searchForm.get('checkIn')?.value).toBeFalsy();
    expect(component.searchForm.get('checkOut')?.value).toBeFalsy();
  });

  it('clearSearch() — displayHotels should revert to topHotels', () => {
    component.searchResults.set(MOCK_SEARCH_RESPONSE.hotels);
    component.clearSearch();
    // After clear, searchResults is null → displayHotels returns topHotels
    expect(component.searchResults()).toBeNull();
    expect(component.displayHotels).toEqual(component.topHotels());
  });

  // ── loadMore() ─────────────────────────────────────────────────────────────

  it('loadMore() — should call searchHotels with incremented pageNumber', () => {
    component.searchForm.patchValue({
      city: 'Chennai', checkIn: new Date('2025-06-01'), checkOut: new Date('2025-06-03')
    });
    component.searchResults.set(MOCK_SEARCH_RESPONSE.hotels);
    component.currentPage.set(1);
    hotelSpy.searchHotels.calls.reset();

    component.loadMore();

    expect(hotelSpy.searchHotels).toHaveBeenCalledWith(
      jasmine.objectContaining({ pageNumber: 2, pageSize: 9 })
    );
  });

  it('loadMore() — should append results to existing searchResults', () => {
    const extra = [makeHotel('hotel-006', 'Extra Hotel')];
    hotelSpy.searchHotels.and.returnValue(of({
      hotels: extra, pageNumber: 2, recordsCount: 3
    }));
    component.searchForm.patchValue({
      city: 'Chennai', checkIn: new Date('2025-06-01'), checkOut: new Date('2025-06-03')
    });
    component.searchResults.set(MOCK_SEARCH_RESPONSE.hotels); // 2 existing
    component.currentPage.set(1);

    component.loadMore();

    expect(component.searchResults()?.length).toBe(3); // 2 + 1
    expect(component.searchResults()![2].name).toBe('Extra Hotel');
  });

  it('loadMore() — should increment currentPage after load', () => {
    component.searchForm.patchValue({
      city: 'Chennai', checkIn: new Date('2025-06-01'), checkOut: new Date('2025-06-03')
    });
    component.searchResults.set(MOCK_SEARCH_RESPONSE.hotels);
    component.currentPage.set(1);

    component.loadMore();

    expect(component.currentPage()).toBe(2);
  });

  it('loadMore() — should NOT call searchHotels when city is missing', () => {
    hotelSpy.searchHotels.calls.reset();
    component.searchForm.patchValue({ city: '', checkIn: null, checkOut: null });

    component.loadMore();

    expect(hotelSpy.searchHotels).not.toHaveBeenCalled();
  });

  // ── TEMPLATE RENDERS ───────────────────────────────────────────────────────

  it('should render top hotel cards in the template', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Grand Palace');
  });

  it('should render the search form', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    const form = fixture.nativeElement.querySelector('form');
    expect(form).toBeTruthy();
  });
});