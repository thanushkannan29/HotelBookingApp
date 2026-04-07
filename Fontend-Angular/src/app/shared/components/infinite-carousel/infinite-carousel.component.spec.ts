import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { InfiniteCarouselComponent } from './infinite-carousel.component';
import { HotelListItemDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

function makeHotel(id: string): HotelListItemDto {
  return {
    hotelId: id, name: `Hotel ${id}`, city: 'Chennai',
    imageUrl: 'https://example.com/img.jpg', averageRating: 4.0,
    reviewCount: 10
  };
}

const MOCK_HOTELS: HotelListItemDto[] = [
  makeHotel('h-001'), makeHotel('h-002'), makeHotel('h-003'),
];

describe('InfiniteCarouselComponent', () => {
  let component: InfiniteCarouselComponent;
  let fixture: ComponentFixture<InfiniteCarouselComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InfiniteCarouselComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]),
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(InfiniteCarouselComponent);
    component = fixture.componentInstance;
    component.hotels = MOCK_HOTELS;
    component.ngOnChanges({ hotels: { currentValue: MOCK_HOTELS, previousValue: [], firstChange: true, isFirstChange: () => true } });
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  // ── ngOnChanges ───────────────────────────────────────────────────────────

  it('ngOnChanges — should create displayItems as 3x the hotels (triple clone)', () => {
    expect(component.displayItems.length).toBe(MOCK_HOTELS.length * 3);
  });

  it('ngOnChanges — should contain all hotel ids in displayItems', () => {
    const ids = component.displayItems.map(h => h.hotelId);
    expect(ids).toContain('h-001');
    expect(ids).toContain('h-002');
    expect(ids).toContain('h-003');
  });

  it('ngOnChanges — should handle empty hotels array', () => {
    component.hotels = [];
    component.ngOnChanges({ hotels: { currentValue: [], previousValue: MOCK_HOTELS, firstChange: false, isFirstChange: () => false } });
    // Guard: hotels.length === 0 means displayItems is NOT reset — stays from beforeEach
    expect(component.displayItems.length).toBe(MOCK_HOTELS.length * 3);
  });

  // ── prev / next ───────────────────────────────────────────────────────────

  it('prev — should not throw', () => {
    expect(() => component.prev()).not.toThrow();
  });

  it('next — should not throw', () => {
    expect(() => component.next()).not.toThrow();
  });

  // ── ngOnDestroy ───────────────────────────────────────────────────────────

  it('ngOnDestroy — should not throw', () => {
    expect(() => component.ngOnDestroy()).not.toThrow();
  });
});
