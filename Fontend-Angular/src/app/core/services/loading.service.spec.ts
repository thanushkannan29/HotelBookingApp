import { TestBed } from '@angular/core/testing';
import { LoadingService } from './loading.service';

describe('LoadingService', () => {
  let service: LoadingService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(LoadingService);
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should start with isLoading = false', () => {
    expect(service.isLoading()).toBeFalse();
  });

  // ── show() ─────────────────────────────────────────────────────────────────

  it('show() — should set isLoading to true', () => {
    service.show();
    expect(service.isLoading()).toBeTrue();
  });

  it('show() — calling twice should keep isLoading true', () => {
    service.show();
    service.show();
    expect(service.isLoading()).toBeTrue();
  });

  it('show() — calling three times should still keep isLoading true', () => {
    service.show();
    service.show();
    service.show();
    expect(service.isLoading()).toBeTrue();
  });

  // ── hide() ─────────────────────────────────────────────────────────────────

  it('hide() — should set isLoading to false when count reaches zero', () => {
    service.show();
    service.hide();
    expect(service.isLoading()).toBeFalse();
  });

  it('hide() — calling without show() should not throw and isLoading stays false', () => {
    expect(() => service.hide()).not.toThrow();
    expect(service.isLoading()).toBeFalse();
  });

  it('hide() — calling hide() extra times should not go negative (stays false)', () => {
    service.show();
    service.hide();
    service.hide(); // extra call — count would be negative without Math.max guard
    service.hide(); // extra call
    expect(service.isLoading()).toBeFalse();
  });

  // ── CONCURRENT REQUESTS (counter logic) ────────────────────────────────────

  it('counter logic — 2 shows then 1 hide should keep isLoading true', () => {
    service.show(); // count = 1
    service.show(); // count = 2
    service.hide(); // count = 1 → still loading
    expect(service.isLoading()).toBeTrue();
  });

  it('counter logic — 2 shows then 2 hides should set isLoading to false', () => {
    service.show(); // count = 1
    service.show(); // count = 2
    service.hide(); // count = 1
    service.hide(); // count = 0 → done
    expect(service.isLoading()).toBeFalse();
  });

  it('counter logic — 3 shows then 3 hides should set isLoading to false', () => {
    service.show();
    service.show();
    service.show();
    service.hide();
    service.hide();
    expect(service.isLoading()).toBeTrue(); // still 1 pending
    service.hide();
    expect(service.isLoading()).toBeFalse(); // all resolved
  });

  it('counter logic — interleaved show/hide should resolve correctly', () => {
    service.show(); // count = 1
    service.show(); // count = 2
    service.hide(); // count = 1
    service.show(); // count = 2
    service.hide(); // count = 1
    service.hide(); // count = 0
    expect(service.isLoading()).toBeFalse();
  });

  // ── isLoading SIGNAL (readonly) ────────────────────────────────────────────

  it('isLoading — should be a readonly signal (no set method exposed)', () => {
    expect(typeof service.isLoading).toBe('function'); // signal is callable
    expect((service.isLoading as any).set).toBeUndefined();
    expect((service.isLoading as any).update).toBeUndefined();
  });

  it('isLoading — should reflect state changes reactively after show and hide', () => {
    expect(service.isLoading()).toBeFalse();
    service.show();
    expect(service.isLoading()).toBeTrue();
    service.hide();
    expect(service.isLoading()).toBeFalse();
  });
});