import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { CityAutocompleteComponent } from './city-autocomplete.component';
import { LocationService } from '../../../core/services/location.service';
import { ICity } from 'country-state-city';

const MOCK_CITIES: ICity[] = [
  { name: 'Chennai',   stateCode: 'TN', countryCode: 'IN', latitude: '13.08', longitude: '80.27' },
  { name: 'Coimbatore', stateCode: 'TN', countryCode: 'IN', latitude: '11.00', longitude: '76.96' },
];

describe('CityAutocompleteComponent', () => {
  let component: CityAutocompleteComponent;
  let fixture: ComponentFixture<CityAutocompleteComponent>;
  let locationSpy: jasmine.SpyObj<LocationService>;

  beforeEach(async () => {
    locationSpy = jasmine.createSpyObj('LocationService', ['searchCities', 'getStateNameByCity']);
    locationSpy.searchCities.and.returnValue(MOCK_CITIES);
    locationSpy.getStateNameByCity.and.returnValue('Tamil Nadu');

    await TestBed.configureTestingModule({
      imports: [CityAutocompleteComponent, ReactiveFormsModule],
      providers: [
        provideAnimationsAsync(),
        { provide: LocationService, useValue: locationSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CityAutocompleteComponent);
    component = fixture.componentInstance;
    component.control = new FormControl('');
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  // ── ngOnInit — search ─────────────────────────────────────────────────────

  it('should call searchCities after debounce when control value changes', fakeAsync(() => {
    component.control.setValue('Chen');
    tick(300);
    expect(locationSpy.searchCities).toHaveBeenCalledWith('Chen');
  }));

  it('should populate filteredCities after search', fakeAsync(() => {
    component.control.setValue('Chen');
    tick(300);
    expect(component.filteredCities.length).toBe(2);
  }));

  // ── displayFn ─────────────────────────────────────────────────────────────

  it('displayFn — should return city name for ICity object', () => {
    expect(component.displayFn(MOCK_CITIES[0])).toBe('Chennai');
  });

  it('displayFn — should return string as-is', () => {
    expect(component.displayFn('Chennai')).toBe('Chennai');
  });

  it('displayFn — should return empty string for null/undefined', () => {
    expect(component.displayFn(null as any)).toBe('');
    expect(component.displayFn(undefined as any)).toBe('');
  });

  // ── onOptionSelected ──────────────────────────────────────────────────────

  it('onOptionSelected — should set control value to city name', () => {
    component.onOptionSelected(MOCK_CITIES[0]);
    expect(component.control.value).toBe('Chennai');
  });

  it('onOptionSelected — should clear filteredCities', () => {
    component.filteredCities = MOCK_CITIES;
    component.onOptionSelected(MOCK_CITIES[0]);
    expect(component.filteredCities.length).toBe(0);
  });

  it('onOptionSelected — should fill stateControl when provided', () => {
    const stateControl = new FormControl('');
    component.stateControl = stateControl;
    component.onOptionSelected(MOCK_CITIES[0]);
    expect(stateControl.value).toBe('Tamil Nadu');
  });

  it('onOptionSelected — should NOT throw when stateControl is not provided', () => {
    component.stateControl = undefined;
    expect(() => component.onOptionSelected(MOCK_CITIES[0])).not.toThrow();
  });

  // ── ngOnDestroy ───────────────────────────────────────────────────────────

  it('ngOnDestroy — should not throw', () => {
    expect(() => component.ngOnDestroy()).not.toThrow();
  });
});
