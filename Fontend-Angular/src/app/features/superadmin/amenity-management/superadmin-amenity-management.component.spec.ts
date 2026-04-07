import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { SuperadminAmenityManagementComponent } from './superadmin-amenity-management.component';
import { AmenityService } from '../../../core/services/amenity.service';
import { ToastService } from '../../../core/services/toast.service';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

const MOCK_AMENITIES = [
  { amenityId: 'a-001', name: 'WiFi',  category: 'Tech',     iconName: 'wifi', isActive: true },
  { amenityId: 'a-002', name: 'Pool',  category: 'Services', iconName: 'pool', isActive: false },
];
const MOCK_PAGED = { totalCount: 2, amenities: MOCK_AMENITIES };

describe('SuperadminAmenityManagementComponent', () => {
  let component: SuperadminAmenityManagementComponent;
  let fixture: ComponentFixture<SuperadminAmenityManagementComponent>;
  let serviceSpy: jasmine.SpyObj<AmenityService>;
  let toastSpy: jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    serviceSpy = jasmine.createSpyObj('AmenityService', ['getAllPaged', 'create', 'update', 'toggleStatus', 'delete']);
    toastSpy   = jasmine.createSpyObj('ToastService', ['success', 'error']);

    serviceSpy.getAllPaged.and.returnValue(of(MOCK_PAGED as any));
    serviceSpy.create.and.returnValue(of(MOCK_AMENITIES[0] as any));
    serviceSpy.update.and.returnValue(of(MOCK_AMENITIES[0] as any));
    serviceSpy.toggleStatus.and.returnValue(of({ isActive: true }));
    serviceSpy.delete.and.returnValue(of(undefined));

    await TestBed.configureTestingModule({
      imports: [SuperadminAmenityManagementComponent],
      providers: [
        provideAnimationsAsync(), provideHttpClient(), provideHttpClientTesting(),
        provideRouter([]),
        { provide: AmenityService, useValue: serviceSpy },
        { provide: ToastService,   useValue: toastSpy },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(SuperadminAmenityManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  // ── ngOnInit ──────────────────────────────────────────────────────────────

  it('ngOnInit — should call getAllPaged', () => {
    expect(serviceSpy.getAllPaged).toHaveBeenCalled();
  });

  it('ngOnInit — should populate amenities signal', () => {
    expect(component.amenities().length).toBe(2);
  });

  it('ngOnInit — should set totalCount', () => {
    expect(component.totalCount()).toBe(2);
  });

  it('loading — should be false after load', () => {
    expect(component.loading()).toBeFalse();
  });

  it('load — should set loading to false on error', () => {
    serviceSpy.getAllPaged.and.returnValue(throwError(() => new Error('fail')));
    component.load();
    expect(component.loading()).toBeFalse();
  });

  // ── Initial state ─────────────────────────────────────────────────────────

  it('editingId — should start as null', () => expect(component.editingId()).toBeNull());
  it('saving — should start as false',   () => expect(component.saving()).toBeFalse());

  // ── startEdit / cancelEdit ────────────────────────────────────────────────

  it('startEdit — should set editingId', () => {
    component.startEdit(MOCK_AMENITIES[0] as any);
    expect(component.editingId()).toBe('a-001');
  });

  it('startEdit — should patch form with amenity values', () => {
    component.startEdit(MOCK_AMENITIES[0] as any);
    expect(component.form.get('name')?.value).toBe('WiFi');
    expect(component.form.get('category')?.value).toBe('Tech');
  });

  it('cancelEdit — should clear editingId', () => {
    component.startEdit(MOCK_AMENITIES[0] as any);
    component.cancelEdit();
    expect(component.editingId()).toBeNull();
  });

  // ── save — create ─────────────────────────────────────────────────────────

  it('save — should call create when editingId is null', () => {
    component.form.patchValue({ name: 'Gym', category: 'Services' });
    component.save();
    expect(serviceSpy.create).toHaveBeenCalled();
  });

  it('save — should show "Amenity created." toast on create', () => {
    component.form.patchValue({ name: 'Gym', category: 'Services' });
    component.save();
    expect(toastSpy.success).toHaveBeenCalledWith('Amenity created.');
  });

  // ── save — update ─────────────────────────────────────────────────────────

  it('save — should call update when editingId is set', () => {
    component.startEdit(MOCK_AMENITIES[0] as any);
    component.save();
    expect(serviceSpy.update).toHaveBeenCalled();
  });

  it('save — should show "Amenity updated." toast on update', () => {
    component.startEdit(MOCK_AMENITIES[0] as any);
    component.save();
    expect(toastSpy.success).toHaveBeenCalledWith('Amenity updated.');
  });

  it('save — should NOT call service when form is invalid', () => {
    component.save();
    expect(serviceSpy.create).not.toHaveBeenCalled();
    expect(serviceSpy.update).not.toHaveBeenCalled();
  });

  it('save — should mark all touched when form is invalid', () => {
    component.save();
    expect(component.form.get('name')?.touched).toBeTrue();
  });

  it('save — should reset saving to false on error', () => {
    serviceSpy.create.and.returnValue(throwError(() => new Error('fail')));
    component.form.patchValue({ name: 'Gym', category: 'Services' });
    component.save();
    expect(component.saving()).toBeFalse();
  });

  // ── toggle ────────────────────────────────────────────────────────────────

  it('toggle — should call toggleStatus', () => {
    component.toggle(MOCK_AMENITIES[0] as any);
    expect(serviceSpy.toggleStatus).toHaveBeenCalledWith('a-001');
  });

  it('toggle — should show toast with activation status', () => {
    serviceSpy.toggleStatus.and.returnValue(of({ isActive: true }));
    component.toggle(MOCK_AMENITIES[0] as any);
    expect(toastSpy.success).toHaveBeenCalledWith('Amenity activated.');
  });

  // ── onPage ────────────────────────────────────────────────────────────────

  it('onPage — should update currentPage and reload', () => {
    serviceSpy.getAllPaged.calls.reset();
    component.onPage({ pageIndex: 1, pageSize: 10, length: 20 } as any);
    expect(component.currentPage).toBe(2);
    expect(serviceSpy.getAllPaged).toHaveBeenCalled();
  });
});
