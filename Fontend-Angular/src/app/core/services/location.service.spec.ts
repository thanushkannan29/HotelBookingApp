import { TestBed } from '@angular/core/testing';
import { LocationService } from './location.service';

describe('LocationService', () => {
  let service: LocationService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(LocationService);
  });

  it('should be created', () => expect(service).toBeTruthy());

  // ── getStates ─────────────────────────────────────────────────────────────

  it('getStates — should return Indian states', () => {
    const states = service.getStates();
    expect(states.length).toBeGreaterThan(0);
    const names = states.map(s => s.name);
    expect(names).toContain('Tamil Nadu');
    expect(names).toContain('Maharashtra');
  });

  // ── getCitiesOfState ──────────────────────────────────────────────────────

  it('getCitiesOfState — should return cities for Tamil Nadu (TN)', () => {
    const cities = service.getCitiesOfState('TN');
    expect(cities.length).toBeGreaterThan(0);
    const names = cities.map(c => c.name);
    expect(names).toContain('Chennai');
  });

  it('getCitiesOfState — should return empty array for invalid state code', () => {
    const cities = service.getCitiesOfState('INVALID');
    expect(cities).toEqual([]);
  });

  // ── getStateByCode ────────────────────────────────────────────────────────

  it('getStateByCode — should return state for valid code', () => {
    const state = service.getStateByCode('TN');
    expect(state).toBeTruthy();
    expect(state?.name).toBe('Tamil Nadu');
  });

  it('getStateByCode — should return undefined for invalid code', () => {
    const state = service.getStateByCode('INVALID');
    expect(state).toBeUndefined();
  });

  // ── searchCities ──────────────────────────────────────────────────────────

  it('searchCities — should return cities matching query', () => {
    const results = service.searchCities('Chen');
    expect(results.length).toBeGreaterThan(0);
    expect(results[0].name.toLowerCase()).toContain('chen');
  });

  it('searchCities — should return empty array for query shorter than 2 chars', () => {
    expect(service.searchCities('C')).toEqual([]);
    expect(service.searchCities('')).toEqual([]);
  });

  it('searchCities — should return at most 20 results', () => {
    const results = service.searchCities('a');
    expect(results.length).toBeLessThanOrEqual(20);
  });

  // ── getStateNameByCity ────────────────────────────────────────────────────

  it('getStateNameByCity — should return state name for known city', () => {
    const state = service.getStateNameByCity('Chennai');
    expect(state).toBeTruthy();
    expect(state).toBe('Tamil Nadu');
  });

  it('getStateNameByCity — should return empty string for unknown city', () => {
    expect(service.getStateNameByCity('UnknownCityXYZ')).toBe('');
  });
});
