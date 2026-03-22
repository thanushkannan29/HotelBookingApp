import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { RoomManagementComponent } from './room-management.component';
import { RoomService, RoomTypeService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { RoomListResponseDto, RoomTypeListDto } from '../../../core/models/models';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

// ── Mock data ──────────────────────────────────────────────────────────────────

const MOCK_ROOM_TYPES: RoomTypeListDto[] = [
  { roomTypeId: 'rt-001', name: 'Deluxe',   description: '', maxOccupancy: 2, amenities: 'WiFi,AC', isActive: true,  roomCount: 5 },
  { roomTypeId: 'rt-002', name: 'Suite',    description: '', maxOccupancy: 4, amenities: 'WiFi,AC,Jacuzzi', isActive: true,  roomCount: 2 },
  { roomTypeId: 'rt-003', name: 'Standard', description: '', maxOccupancy: 2, amenities: 'WiFi', isActive: false, roomCount: 3 },
];

const MOCK_ROOMS: RoomListResponseDto[] = [
  { roomId: 'r-001', roomNumber: '101', floor: 1, roomTypeId: 'rt-001', roomTypeName: 'Deluxe',   isActive: true  },
  { roomId: 'r-002', roomNumber: '102', floor: 1, roomTypeId: 'rt-001', roomTypeName: 'Deluxe',   isActive: true  },
  { roomId: 'r-003', roomNumber: '201', floor: 2, roomTypeId: 'rt-002', roomTypeName: 'Suite',    isActive: false },
  { roomId: 'r-004', roomNumber: '301', floor: 3, roomTypeId: 'rt-003', roomTypeName: 'Standard', isActive: true  },
];

// ─────────────────────────────────────────────────────────────────────────────

describe('RoomManagementComponent', () => {
  let component: RoomManagementComponent;
  let fixture:   ComponentFixture<RoomManagementComponent>;

  let roomSpy:     jasmine.SpyObj<RoomService>;
  let roomTypeSpy: jasmine.SpyObj<RoomTypeService>;
  let toastSpy:    jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    roomSpy     = jasmine.createSpyObj('RoomService', [
      'getRooms', 'addRoom', 'updateRoom', 'toggleRoomStatus'
    ]);
    roomTypeSpy = jasmine.createSpyObj('RoomTypeService', ['getRoomTypes']);
    toastSpy    = jasmine.createSpyObj('ToastService', ['success', 'error']);

    // Default happy-path responses
    roomSpy.getRooms.and.returnValue(of(MOCK_ROOMS));
    roomSpy.addRoom.and.returnValue(of(undefined));
    roomSpy.updateRoom.and.returnValue(of(undefined));
    roomSpy.toggleRoomStatus.and.returnValue(of(undefined));
    roomTypeSpy.getRoomTypes.and.returnValue(of(MOCK_ROOM_TYPES));

    await TestBed.configureTestingModule({
      imports: [RoomManagementComponent],
      providers: [
        provideAnimationsAsync(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: RoomService,     useValue: roomSpy     },
        { provide: RoomTypeService, useValue: roomTypeSpy },
        { provide: ToastService,    useValue: toastSpy    },
      ]
    }).compileComponents();

    fixture   = TestBed.createComponent(RoomManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ── CREATION ───────────────────────────────────────────────────────────────

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  // ── INITIAL SIGNAL STATE ───────────────────────────────────────────────────

  it('showAddForm — should start as false', () => {
    expect(component.showAddForm()).toBeFalse();
  });

  it('editingRoom — should start as null', () => {
    expect(component.editingRoom()).toBeNull();
  });

  it('isSaving — should start as false', () => {
    expect(component.isSaving()).toBeFalse();
  });

  it('page — should start at 1', () => {
    expect(component.page()).toBe(1);
  });

  // ── ngOnInit ───────────────────────────────────────────────────────────────

  it('ngOnInit — should call getRooms with page 1 and pageSize 20', () => {
    expect(roomSpy.getRooms).toHaveBeenCalledWith(1, 20);
  });

  it('ngOnInit — should call getRoomTypes on startup', () => {
    expect(roomTypeSpy.getRoomTypes).toHaveBeenCalledOnceWith();
  });

  it('ngOnInit — should populate rooms signal', () => {
    expect(component.rooms().length).toBe(4);
    expect(component.rooms()[0].roomNumber).toBe('101');
  });

  it('ngOnInit — should populate roomTypes signal', () => {
    expect(component.roomTypes().length).toBe(3);
    expect(component.roomTypes()[0].name).toBe('Deluxe');
  });

  // ── FORM INITIAL STATE ─────────────────────────────────────────────────────

  it('addForm — should be invalid initially', () => {
    expect(component.addForm.invalid).toBeTrue();
  });

  it('addForm — floor should default to 1', () => {
    expect(component.addForm.get('floor')?.value).toBe(1);
  });

  it('editForm — should be invalid initially (roomNumber and roomTypeId empty)', () => {
    expect(component.editForm.get('roomNumber')?.value).toBe('');
    expect(component.editForm.get('roomTypeId')?.value).toBe('');
  });

  // ── FORM VALIDATION — addForm ──────────────────────────────────────────────

  it('addForm — should be valid when all required fields are filled', () => {
    component.addForm.patchValue({ roomNumber: '101', floor: 1, roomTypeId: 'rt-001' });
    expect(component.addForm.valid).toBeTrue();
  });

  it('addForm — should be invalid when roomNumber is empty', () => {
    component.addForm.patchValue({ roomNumber: '', floor: 1, roomTypeId: 'rt-001' });
    expect(component.addForm.invalid).toBeTrue();
  });

  it('addForm — should be invalid when roomTypeId is empty', () => {
    component.addForm.patchValue({ roomNumber: '101', floor: 1, roomTypeId: '' });
    expect(component.addForm.invalid).toBeTrue();
  });

  it('addForm — floor accepts 0 (ground floor)', () => {
    component.addForm.patchValue({ roomNumber: '001', floor: 0, roomTypeId: 'rt-001' });
    expect(component.addForm.valid).toBeTrue();
  });

  it('addForm — floor rejects negative values', () => {
    component.addForm.patchValue({ roomNumber: '001', floor: -1, roomTypeId: 'rt-001' });
    expect(component.addForm.get('floor')?.invalid).toBeTrue();
  });

  // ── addRoom() — HAPPY PATH ─────────────────────────────────────────────────

  it('addRoom() — should call roomService.addRoom with form values', () => {
    component.addForm.patchValue({ roomNumber: '202', floor: 2, roomTypeId: 'rt-002' });

    component.addRoom();

    expect(roomSpy.addRoom).toHaveBeenCalledOnceWith(
      jasmine.objectContaining({ roomNumber: '202', floor: 2, roomTypeId: 'rt-002' })
    );
  });

  it('addRoom() — should show success toast on success', () => {
    component.addForm.patchValue({ roomNumber: '202', floor: 2, roomTypeId: 'rt-002' });

    component.addRoom();

    expect(toastSpy.success).toHaveBeenCalledOnceWith('Room added.');
  });

  it('addRoom() — should hide add form after success', () => {
    component.showAddForm.set(true);
    component.addForm.patchValue({ roomNumber: '202', floor: 2, roomTypeId: 'rt-002' });

    component.addRoom();

    expect(component.showAddForm()).toBeFalse();
  });

  it('addRoom() — should reset form with floor defaulting to 1 after success', () => {
    component.addForm.patchValue({ roomNumber: '202', floor: 3, roomTypeId: 'rt-002' });

    component.addRoom();

    expect(component.addForm.get('roomNumber')?.value).toBeFalsy();
    expect(component.addForm.get('floor')?.value).toBe(1);
  });

  it('addRoom() — should reload rooms after success', () => {
    component.addForm.patchValue({ roomNumber: '202', floor: 2, roomTypeId: 'rt-002' });
    roomSpy.getRooms.calls.reset();

    component.addRoom();

    expect(roomSpy.getRooms).toHaveBeenCalled();
  });

  it('addRoom() — should reset isSaving to false on success', () => {
    component.addForm.patchValue({ roomNumber: '202', floor: 2, roomTypeId: 'rt-002' });

    component.addRoom();

    expect(component.isSaving()).toBeFalse();
  });

  it('addRoom() — should set isSaving to true during in-flight request', () => {
    const subject = new Subject<void>();
    roomSpy.addRoom.and.returnValue(subject.asObservable());
    component.addForm.patchValue({ roomNumber: '202', floor: 2, roomTypeId: 'rt-002' });

    component.addRoom();

    expect(component.isSaving()).toBeTrue();

    subject.next();
    subject.complete();
  });

  // ── addRoom() — INVALID FORM ───────────────────────────────────────────────

  it('addRoom() — should NOT call service when addForm is invalid', () => {
    component.addRoom();
    expect(roomSpy.addRoom).not.toHaveBeenCalled();
  });

  it('addRoom() — should mark all fields touched when form is invalid', () => {
    component.addRoom();
    expect(component.addForm.get('roomNumber')?.touched).toBeTrue();
    expect(component.addForm.get('roomTypeId')?.touched).toBeTrue();
  });

  // ── addRoom() — ERROR ──────────────────────────────────────────────────────

  it('addRoom() — should reset isSaving to false on API error', () => {
    roomSpy.addRoom.and.returnValue(throwError(() => new Error('fail')));
    component.addForm.patchValue({ roomNumber: '202', floor: 2, roomTypeId: 'rt-002' });

    component.addRoom();

    expect(component.isSaving()).toBeFalse();
  });

  it('addRoom() — should NOT show success toast on API error', () => {
    roomSpy.addRoom.and.returnValue(throwError(() => new Error('fail')));
    component.addForm.patchValue({ roomNumber: '202', floor: 2, roomTypeId: 'rt-002' });

    component.addRoom();

    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  // ── startEdit() ────────────────────────────────────────────────────────────

  it('startEdit() — should set editingRoom signal to the given room', () => {
    component.startEdit(MOCK_ROOMS[0]);
    expect(component.editingRoom()).toEqual(MOCK_ROOMS[0]);
  });

  it('startEdit() — should patch editForm with room values', () => {
    component.startEdit(MOCK_ROOMS[0]);
    expect(component.editForm.get('roomId')?.value).toBe('r-001');
    expect(component.editForm.get('roomNumber')?.value).toBe('101');
    expect(component.editForm.get('floor')?.value).toBe(1);
    expect(component.editForm.get('roomTypeId')?.value).toBe('rt-001');
  });

  it('startEdit() — switching rooms updates both signal and form', () => {
    component.startEdit(MOCK_ROOMS[0]);
    component.startEdit(MOCK_ROOMS[2]); // room 201, floor 2, Suite
    expect(component.editingRoom()?.roomId).toBe('r-003');
    expect(component.editForm.get('roomNumber')?.value).toBe('201');
    expect(component.editForm.get('floor')?.value).toBe(2);
  });

  // ── saveEdit() — HAPPY PATH ────────────────────────────────────────────────

  it('saveEdit() — should call roomService.updateRoom with editForm values', () => {
    component.startEdit(MOCK_ROOMS[0]);

    component.saveEdit();

    expect(roomSpy.updateRoom).toHaveBeenCalledOnceWith(
      jasmine.objectContaining({ roomId: 'r-001', roomNumber: '101', floor: 1 })
    );
  });

  it('saveEdit() — should show success toast on success', () => {
    component.startEdit(MOCK_ROOMS[0]);

    component.saveEdit();

    expect(toastSpy.success).toHaveBeenCalledOnceWith('Room updated.');
  });

  it('saveEdit() — should clear editingRoom after success', () => {
    component.startEdit(MOCK_ROOMS[0]);

    component.saveEdit();

    expect(component.editingRoom()).toBeNull();
  });

  it('saveEdit() — should reload rooms after success', () => {
    component.startEdit(MOCK_ROOMS[0]);
    roomSpy.getRooms.calls.reset();

    component.saveEdit();

    expect(roomSpy.getRooms).toHaveBeenCalled();
  });

  it('saveEdit() — should reset isSaving to false on success', () => {
    component.startEdit(MOCK_ROOMS[0]);

    component.saveEdit();

    expect(component.isSaving()).toBeFalse();
  });

  it('saveEdit() — should set isSaving to true during in-flight request', () => {
    const subject = new Subject<void>();
    roomSpy.updateRoom.and.returnValue(subject.asObservable());
    component.startEdit(MOCK_ROOMS[0]);

    component.saveEdit();

    expect(component.isSaving()).toBeTrue();

    subject.next();
    subject.complete();
  });

  // ── saveEdit() — INVALID FORM ──────────────────────────────────────────────

  it('saveEdit() — should NOT call service when editForm is invalid', () => {
    // editForm starts empty/invalid — do not call startEdit
    component.saveEdit();
    expect(roomSpy.updateRoom).not.toHaveBeenCalled();
  });

  // ── saveEdit() — ERROR ─────────────────────────────────────────────────────

  it('saveEdit() — should reset isSaving to false on API error', () => {
    roomSpy.updateRoom.and.returnValue(throwError(() => new Error('fail')));
    component.startEdit(MOCK_ROOMS[0]);

    component.saveEdit();

    expect(component.isSaving()).toBeFalse();
  });

  it('saveEdit() — should NOT show success toast on API error', () => {
    roomSpy.updateRoom.and.returnValue(throwError(() => new Error('fail')));
    component.startEdit(MOCK_ROOMS[0]);

    component.saveEdit();

    expect(toastSpy.success).not.toHaveBeenCalled();
  });

  // ── toggleStatus() ─────────────────────────────────────────────────────────

  it('toggleStatus() — should call toggleRoomStatus with inverted isActive', () => {
    component.toggleStatus(MOCK_ROOMS[0]); // isActive: true → pass false
    expect(roomSpy.toggleRoomStatus).toHaveBeenCalledOnceWith('r-001', false);
  });

  it('toggleStatus() — should pass true when room is currently inactive', () => {
    component.toggleStatus(MOCK_ROOMS[2]); // isActive: false → pass true
    expect(roomSpy.toggleRoomStatus).toHaveBeenCalledOnceWith('r-003', true);
  });

  it('toggleStatus() — should show "activated" toast when activating an inactive room', () => {
    component.toggleStatus(MOCK_ROOMS[2]); // isActive: false → activating
    expect(toastSpy.success).toHaveBeenCalledOnceWith('Room activated.');
  });

  it('toggleStatus() — should show "deactivated" toast when deactivating an active room', () => {
    component.toggleStatus(MOCK_ROOMS[0]); // isActive: true → deactivating
    expect(toastSpy.success).toHaveBeenCalledOnceWith('Room deactivated.');
  });

  it('toggleStatus() — should flip isActive in rooms signal for the toggled room', () => {
    component.toggleStatus(MOCK_ROOMS[0]); // was true → becomes false

    const updated = component.rooms().find(r => r.roomId === 'r-001');
    expect(updated?.isActive).toBeFalse();
  });

  it('toggleStatus() — should activate an inactive room in the signal', () => {
    component.toggleStatus(MOCK_ROOMS[2]); // was false → becomes true

    const updated = component.rooms().find(r => r.roomId === 'r-003');
    expect(updated?.isActive).toBeTrue();
  });

  it('toggleStatus() — should NOT change other rooms in the signal', () => {
    component.toggleStatus(MOCK_ROOMS[0]); // only r-001 changes

    const others = component.rooms().filter(r => r.roomId !== 'r-001');
    expect(others[0].isActive).toBe(MOCK_ROOMS[1].isActive);
    expect(others[1].isActive).toBe(MOCK_ROOMS[2].isActive);
    expect(others[2].isActive).toBe(MOCK_ROOMS[3].isActive);
  });
});