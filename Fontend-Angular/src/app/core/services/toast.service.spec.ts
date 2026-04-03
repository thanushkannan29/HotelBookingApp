import { TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ToastService } from './toast.service';

describe('ToastService', () => {
  let service: ToastService;
  let snackBarSpy: jasmine.SpyObj<MatSnackBar>;

  beforeEach(() => {
    // Create a spy object with the 'open' method mocked
    snackBarSpy = jasmine.createSpyObj('MatSnackBar', ['open']);

    TestBed.configureTestingModule({
      providers: [
        ToastService,
        { provide: MatSnackBar, useValue: snackBarSpy }
      ]
    });

    service = TestBed.inject(ToastService);
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  // ── success() ──────────────────────────────────────────────────────────────

  it('success() — should call snackBar.open with correct message', () => {
    service.success('Hotel booked successfully!');

    expect(snackBarSpy.open).toHaveBeenCalledOnceWith(
      'Hotel booked successfully!',
      '✕',
      jasmine.objectContaining({ panelClass: ['toast-success'] })
    );
  });

  it('success() — should use duration of 3500ms', () => {
    service.success('Payment confirmed');

    const config = snackBarSpy.open.calls.mostRecent().args[2];
    expect(config?.duration).toBe(3500);
  });

  it('success() — should position at top-right', () => {
    service.success('Profile updated');

    const config = snackBarSpy.open.calls.mostRecent().args[2];
    expect(config?.horizontalPosition).toBe('right');
    expect(config?.verticalPosition).toBe('top');
  });

  it('success() — action button label should be ✕', () => {
    service.success('Done');

    const action = snackBarSpy.open.calls.mostRecent().args[1];
    expect(action).toBe('✕');
  });

  // ── error() ────────────────────────────────────────────────────────────────

  it('error() — should call snackBar.open with correct message', () => {
    service.error('Something went wrong. Please try again.');

    expect(snackBarSpy.open).toHaveBeenCalledOnceWith(
      'Something went wrong. Please try again.',
      '✕',
      jasmine.objectContaining({ panelClass: ['toast-error'] })
    );
  });

  it('error() — should use duration of 5000ms (longer than success)', () => {
    service.error('Network error');

    const config = snackBarSpy.open.calls.mostRecent().args[2];
    expect(config?.duration).toBe(5000);
  });

  it('error() — should position at top-right', () => {
    service.error('Unauthorized');

    const config = snackBarSpy.open.calls.mostRecent().args[2];
    expect(config?.horizontalPosition).toBe('right');
    expect(config?.verticalPosition).toBe('top');
  });

  it('error() — duration should be greater than success duration (errors stay longer)', () => {
    service.success('ok');
    const successDuration = snackBarSpy.open.calls.mostRecent().args[2]?.duration;

    snackBarSpy.open.calls.reset();

    service.error('fail');
    const errorDuration = snackBarSpy.open.calls.mostRecent().args[2]?.duration;

    expect(errorDuration!).toBeGreaterThan(successDuration!);
  });

  // ── info() ─────────────────────────────────────────────────────────────────

  it('info() — should call snackBar.open with correct message', () => {
    service.info('Your session will expire soon.');

    expect(snackBarSpy.open).toHaveBeenCalledOnceWith(
      'Your session will expire soon.',
      '✕',
      jasmine.objectContaining({ panelClass: ['toast-info'] })
    );
  });

  it('info() — should use duration of 3500ms', () => {
    service.info('Tip: Use filters to narrow results');

    const config = snackBarSpy.open.calls.mostRecent().args[2];
    expect(config?.duration).toBe(3500);
  });

  it('info() — should position at top-right', () => {
    service.info('New update available');

    const config = snackBarSpy.open.calls.mostRecent().args[2];
    expect(config?.horizontalPosition).toBe('right');
    expect(config?.verticalPosition).toBe('top');
  });

  // ── warning() ──────────────────────────────────────────────────────────────

  it('warning() — should call snackBar.open with correct message', () => {
    service.warning('Your booking expires in 2 minutes!');

    expect(snackBarSpy.open).toHaveBeenCalledOnceWith(
      'Your booking expires in 2 minutes!',
      '✕',
      jasmine.objectContaining({ panelClass: ['toast-warning'] })
    );
  });

  it('warning() — should use duration of 4000ms', () => {
    service.warning('Check your details before confirming');

    const config = snackBarSpy.open.calls.mostRecent().args[2];
    expect(config?.duration).toBe(4000);
  });

  it('warning() — should position at top-right', () => {
    service.warning('Low inventory');

    const config = snackBarSpy.open.calls.mostRecent().args[2];
    expect(config?.horizontalPosition).toBe('right');
    expect(config?.verticalPosition).toBe('top');
  });

  // ── PANEL CLASSES (each toast has unique CSS class) ────────────────────────

  it('each method should use a different panelClass', () => {
    service.success('a');
    const successClass = snackBarSpy.open.calls.mostRecent().args[2]?.panelClass;
    snackBarSpy.open.calls.reset();

    service.error('b');
    const errorClass = snackBarSpy.open.calls.mostRecent().args[2]?.panelClass;
    snackBarSpy.open.calls.reset();

    service.info('c');
    const infoClass = snackBarSpy.open.calls.mostRecent().args[2]?.panelClass;
    snackBarSpy.open.calls.reset();

    service.warning('d');
    const warningClass = snackBarSpy.open.calls.mostRecent().args[2]?.panelClass;

    expect(successClass).toEqual(['toast-success']);
    expect(errorClass).toEqual(['toast-error']);
    expect(infoClass).toEqual(['toast-info']);
    expect(warningClass).toEqual(['toast-warning']);

    // All four must be different
    const all = [successClass, errorClass, infoClass, warningClass];
    const unique = new Set(all.map(c => JSON.stringify(c)));
    expect(unique.size).toBe(4);
  });

  // ── CALLED ONCE PER INVOCATION ─────────────────────────────────────────────

  it('should call snackBar.open exactly once per toast method call', () => {
    service.success('one');
    expect(snackBarSpy.open).toHaveBeenCalledTimes(1);

    service.error('two');
    expect(snackBarSpy.open).toHaveBeenCalledTimes(2);

    service.info('three');
    expect(snackBarSpy.open).toHaveBeenCalledTimes(3);

    service.warning('four');
    expect(snackBarSpy.open).toHaveBeenCalledTimes(4);
  });
});
