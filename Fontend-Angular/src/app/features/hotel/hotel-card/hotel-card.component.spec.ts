import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { HotelCardComponent } from './hotel-card.component';
import { HotelListItemDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

// ── Mock data ──────────────────────────────────────────────────────────────────

const MOCK_HOTEL: HotelListItemDto = {
  hotelId:       'hotel-001',
  name:          'Grand Palace',
  city:          'Chennai',
  imageUrl:      'https://example.com/img.jpg',
  averageRating: 4.7,
  reviewCount:   120,
  startingPrice: 3500,
};

// ─────────────────────────────────────────────────────────────────────────────

describe('HotelCardComponent', () => {
  let component: HotelCardComponent;
  let fixture:   ComponentFixture<HotelCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HotelCardComponent],
      providers: [
        provideAnimationsAsync(),provideRouter([])] // required by RouterLink
    }).compileComponents();

    fixture   = TestBed.createComponent(HotelCardComponent);
    component = fixture.componentInstance;

    // Provide required @Input before detectChanges
    component.hotel = MOCK_HOTEL;
    fixture.detectChanges();
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── stars GETTER ───────────────────────────────────────────────────────────

  it('stars — should return [1, 2, 3, 4, 5]', () => {
    expect(component.stars).toEqual([1, 2, 3, 4, 5]);
  });

  it('stars — should always return exactly 5 elements', () => {
    expect(component.stars.length).toBe(5);
  });

  // ── ratingClass GETTER ─────────────────────────────────────────────────────

  it('ratingClass — should return "excellent" for rating >= 4.5', () => {
    component.hotel = { ...MOCK_HOTEL, averageRating: 4.5 };
    expect(component.ratingClass).toBe('excellent');
  });

  it('ratingClass — should return "excellent" for rating = 5.0', () => {
    component.hotel = { ...MOCK_HOTEL, averageRating: 5.0 };
    expect(component.ratingClass).toBe('excellent');
  });

  it('ratingClass — should return "great" for rating = 4.0', () => {
    component.hotel = { ...MOCK_HOTEL, averageRating: 4.0 };
    expect(component.ratingClass).toBe('great');
  });

  it('ratingClass — should return "great" for rating = 4.4', () => {
    component.hotel = { ...MOCK_HOTEL, averageRating: 4.4 };
    expect(component.ratingClass).toBe('great');
  });

  it('ratingClass — should return "good" for rating = 3.0', () => {
    component.hotel = { ...MOCK_HOTEL, averageRating: 3.0 };
    expect(component.ratingClass).toBe('good');
  });

  it('ratingClass — should return "good" for rating = 3.9', () => {
    component.hotel = { ...MOCK_HOTEL, averageRating: 3.9 };
    expect(component.ratingClass).toBe('good');
  });

  it('ratingClass — should return "fair" for rating < 3.0', () => {
    component.hotel = { ...MOCK_HOTEL, averageRating: 2.5 };
    expect(component.ratingClass).toBe('fair');
  });

  it('ratingClass — should return "fair" for rating = 0', () => {
    component.hotel = { ...MOCK_HOTEL, averageRating: 0 };
    expect(component.ratingClass).toBe('fair');
  });

  // ── ratingLabel GETTER ─────────────────────────────────────────────────────

  it('ratingLabel — should return "Excellent" for rating >= 4.5', () => {
    component.hotel = { ...MOCK_HOTEL, averageRating: 4.8 };
    expect(component.ratingLabel).toBe('Excellent');
  });

  it('ratingLabel — should return "Great" for rating = 4.2', () => {
    component.hotel = { ...MOCK_HOTEL, averageRating: 4.2 };
    expect(component.ratingLabel).toBe('Great');
  });

  it('ratingLabel — should return "Good" for rating = 3.5', () => {
    component.hotel = { ...MOCK_HOTEL, averageRating: 3.5 };
    expect(component.ratingLabel).toBe('Good');
  });

  it('ratingLabel — should return "Fair" for rating = 2.0', () => {
    component.hotel = { ...MOCK_HOTEL, averageRating: 2.0 };
    expect(component.ratingLabel).toBe('Fair');
  });

  it('ratingClass and ratingLabel should be consistent', () => {
    const cases: Array<[number, string, string]> = [
      [4.9, 'excellent', 'Excellent'],
      [4.1, 'great',     'Great'],
      [3.2, 'good',      'Good'],
      [1.5, 'fair',      'Fair'],
    ];

    cases.forEach(([rating, expectedClass, expectedLabel]) => {
      component.hotel = { ...MOCK_HOTEL, averageRating: rating };
      expect(component.ratingClass).toBe(expectedClass);
      expect(component.ratingLabel).toBe(expectedLabel);
    });
  });

  // ── imagePlaceholder GETTER ────────────────────────────────────────────────

  it('imagePlaceholder — should return a hex color string', () => {
    expect(component.imagePlaceholder).toMatch(/^#[0-9a-f]{6}$/i);
  });

  it('imagePlaceholder — should be deterministic for the same hotel name', () => {
    const first  = component.imagePlaceholder;
    const second = component.imagePlaceholder;
    expect(first).toBe(second);
  });

  it('imagePlaceholder — should return one of the five defined colors', () => {
    const allowedColors = ['#2d3a8c', '#1a4d5c', '#3d2b1f', '#1a3a2d', '#3d1a3a'];
    expect(allowedColors).toContain(component.imagePlaceholder);
  });

  it('imagePlaceholder — different hotel names can produce different colors', () => {
    // 'A'.charCodeAt(0) = 65, 'B'.charCodeAt(0) = 66 → 65%5=0, 66%5=1 → different colors
    component.hotel = { ...MOCK_HOTEL, name: 'AAAAA' };
    const colorA = component.imagePlaceholder;

    component.hotel = { ...MOCK_HOTEL, name: 'BBBBB' };
    const colorB = component.imagePlaceholder;

    // These should differ (A→idx 0, B→idx 1)
    expect(colorA).not.toBe(colorB);
  });

  // ── @Input hotel ───────────────────────────────────────────────────────────

  it('hotel — should reflect the provided @Input value', () => {
    expect(component.hotel.hotelId).toBe('hotel-001');
    expect(component.hotel.name).toBe('Grand Palace');
    expect(component.hotel.city).toBe('Chennai');
    expect(component.hotel.averageRating).toBe(4.7);
  });

  it('hotel — should update getters when @Input changes', () => {
    component.hotel = { ...MOCK_HOTEL, averageRating: 2.0 };
    expect(component.ratingLabel).toBe('Fair');
    expect(component.ratingClass).toBe('fair');
  });

  // ── TEMPLATE RENDERS ───────────────────────────────────────────────────────

  it('should display the hotel name in the template', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Grand Palace');
  });

  it('should display the hotel city in the template', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Chennai');
  });

  it('should display the review count in the template', async () => {
    fixture.detectChanges();
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('120');
  });
});