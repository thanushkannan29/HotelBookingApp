import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SpinnerComponent } from './spinner.component';
import { LoadingService } from '../../../core/services/loading.service';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

describe('SpinnerComponent', () => {
  let component: SpinnerComponent;
  let fixture:   ComponentFixture<SpinnerComponent>;
  let loadingSpy: jasmine.SpyObj<LoadingService>;

  beforeEach(async () => {
    loadingSpy = jasmine.createSpyObj('LoadingService', ['show', 'hide'], {
      isLoading: jasmine.createSpy('isLoading').and.returnValue(false)
    });

    await TestBed.configureTestingModule({
      imports: [SpinnerComponent],
      providers: [
        provideAnimationsAsync(),
        { provide: LoadingService, useValue: loadingSpy }
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(SpinnerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── LOADING SERVICE INTEGRATION ────────────────────────────────────────────

  it('loading — should expose the injected LoadingService', () => {
    expect(component.loading).toBeTruthy();
  });

  // ── TEMPLATE — HIDDEN WHEN NOT LOADING ─────────────────────────────────────

  it('should NOT render the spinner when isLoading is false', () => {
    (loadingSpy.isLoading as jasmine.Spy).and.returnValue(false);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.full-page-spinner')).toBeFalsy();
  });

  it('should NOT render mat-progress-spinner when isLoading is false', () => {
    (loadingSpy.isLoading as jasmine.Spy).and.returnValue(false);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('mat-progress-spinner')).toBeFalsy();
  });

  it('should NOT render "Loading..." text when isLoading is false', () => {
    (loadingSpy.isLoading as jasmine.Spy).and.returnValue(false);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).not.toContain('Loading...');
  });

  // ── TEMPLATE — VISIBLE WHEN LOADING ────────────────────────────────────────

  it('should render the spinner overlay when isLoading is true', () => {
    (loadingSpy.isLoading as jasmine.Spy).and.returnValue(true);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.full-page-spinner')).toBeTruthy();
  });

  it('should render mat-progress-spinner when isLoading is true', () => {
    (loadingSpy.isLoading as jasmine.Spy).and.returnValue(true);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('mat-progress-spinner')).toBeTruthy();
  });

  it('should display "Loading..." text when isLoading is true', () => {
    (loadingSpy.isLoading as jasmine.Spy).and.returnValue(true);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Loading...');
  });

  // ── TEMPLATE — REACTIVE TO SIGNAL CHANGES ──────────────────────────────────

  it('should hide spinner when isLoading transitions from true to false', () => {
    // Start loading
    (loadingSpy.isLoading as jasmine.Spy).and.returnValue(true);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.full-page-spinner')).toBeTruthy();

    // Stop loading
    (loadingSpy.isLoading as jasmine.Spy).and.returnValue(false);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.full-page-spinner')).toBeFalsy();
  });

  it('should show spinner when isLoading transitions from false to true', () => {
    // Start hidden
    (loadingSpy.isLoading as jasmine.Spy).and.returnValue(false);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.full-page-spinner')).toBeFalsy();

    // Start loading
    (loadingSpy.isLoading as jasmine.Spy).and.returnValue(true);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.full-page-spinner')).toBeTruthy();
  });

  // ── SPINNER ATTRIBUTES ─────────────────────────────────────────────────────

  it('mat-progress-spinner should have mode="indeterminate"', () => {
    (loadingSpy.isLoading as jasmine.Spy).and.returnValue(true);
    fixture.detectChanges();

    const spinner = fixture.nativeElement.querySelector('mat-progress-spinner');
    expect(spinner?.getAttribute('mode')).toBe('indeterminate');
  });

  it('mat-progress-spinner should have diameter set to 40', () => {
    (loadingSpy.isLoading as jasmine.Spy).and.returnValue(true);
    fixture.detectChanges();

    const spinner = fixture.nativeElement.querySelector('mat-progress-spinner');
    expect(spinner?.getAttribute('diameter')).toBe('40');
  });
});