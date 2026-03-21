import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';
import { RoomService, RoomTypeService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { RoomListResponseDto, RoomTypeListDto } from '../../../core/models/models';

@Component({
  selector: 'app-room-management',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatSlideToggleModule, MatTooltipModule
  ],
  templateUrl: './room-management.component.html',
  styleUrl: './room-management.component.scss'
})
export class RoomManagementComponent implements OnInit {
  private roomService = inject(RoomService);
  private roomTypeService = inject(RoomTypeService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  rooms = signal<RoomListResponseDto[]>([]);
  roomTypes = signal<RoomTypeListDto[]>([]);
  showAddForm = signal(false);
  editingRoom = signal<RoomListResponseDto | null>(null);
  isSaving = signal(false);
  page = signal(1);

  addForm = this.fb.group({
    roomNumber: ['', Validators.required],
    floor:      [1, [Validators.required, Validators.min(0)]],
    roomTypeId: ['', Validators.required],
  });

  editForm = this.fb.group({
    roomId:     [''],
    roomNumber: ['', Validators.required],
    floor:      [1, [Validators.required]],
    roomTypeId: ['', Validators.required],
  });

  ngOnInit() {
    this.loadRooms();
    this.roomTypeService.getRoomTypes().subscribe(rt => this.roomTypes.set(rt));
  }

  loadRooms() {
    this.roomService.getRooms(this.page(), 20).subscribe(r => this.rooms.set(r));
  }

  addRoom() {
    if (this.addForm.invalid) { this.addForm.markAllAsTouched(); return; }
    this.isSaving.set(true);
    this.roomService.addRoom(this.addForm.value as any).subscribe({
      next: () => {
        this.toast.success('Room added.');
        this.addForm.reset({ floor: 1 });
        this.showAddForm.set(false);
        this.loadRooms();
        this.isSaving.set(false);
      },
      error: () => this.isSaving.set(false),
    });
  }

  startEdit(room: RoomListResponseDto) {
    this.editingRoom.set(room);
    this.editForm.patchValue({ roomId: room.roomId, roomNumber: room.roomNumber, floor: room.floor, roomTypeId: room.roomTypeId });
  }

  saveEdit() {
    if (this.editForm.invalid) return;
    this.isSaving.set(true);
    this.roomService.updateRoom(this.editForm.value as any).subscribe({
      next: () => {
        this.toast.success('Room updated.');
        this.editingRoom.set(null);
        this.loadRooms();
        this.isSaving.set(false);
      },
      error: () => this.isSaving.set(false),
    });
  }

  toggleStatus(room: RoomListResponseDto) {
    this.roomService.toggleRoomStatus(room.roomId, !room.isActive).subscribe(() => {
      this.toast.success(`Room ${!room.isActive ? 'activated' : 'deactivated'}.`);
      this.rooms.update(r => r.map(x => x.roomId === room.roomId ? { ...x, isActive: !x.isActive } : x));
    });
  }
}
