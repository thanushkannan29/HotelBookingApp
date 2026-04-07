import { Component, inject, signal, OnInit, AfterViewInit, ViewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatTabsModule } from '@angular/material/tabs';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { debounceTime, distinctUntilChanged, Subject } from 'rxjs';
import { RoomService, RoomTypeService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { RoomListResponseDto, RoomTypeListDto, RoomOccupancyDto } from '../../../core/models/models';

@Component({
  selector: 'app-room-management',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatSlideToggleModule, MatTooltipModule,
    MatTableModule, MatSortModule, MatPaginatorModule,
    MatTabsModule, MatDatepickerModule, MatNativeDateModule, MatProgressSpinnerModule,
    MatChipsModule,
  ],
  templateUrl: './room-management.component.html',
  styleUrl: './room-management.component.scss'
})
export class RoomManagementComponent implements OnInit {
  private roomService     = inject(RoomService);
  private roomTypeService = inject(RoomTypeService);
  private toast           = inject(ToastService);
  private fb              = inject(FormBuilder);

  roomTypes    = signal<RoomTypeListDto[]>([]);
  rooms        = signal<RoomListResponseDto[]>([]);
  totalCount   = signal(0);
  loading      = signal(false);
  showAddForm  = signal(false);
  editingRoom  = signal<RoomListResponseDto | null>(null);
  isSaving     = signal(false);
  pageSize     = 10;
  currentPage  = 1;
  displayedColumns = ['roomNumber', 'floor', 'roomTypeName', 'isActive', 'actions'];

  occupancyRooms   = signal<RoomOccupancyDto[]>([]);
  occupancyDate    = signal<Date | null>(null);
  today            = new Date();
  occupancyColumns = ['roomNumber', 'floor', 'roomTypeName', 'status', 'reservationCode'];

  private searchSubject = new Subject<string>();

  addForm = this.fb.group({
    roomNumber: ['', [Validators.required, Validators.maxLength(20), Validators.pattern(/^[a-zA-Z0-9]+$/)]],
    floor:      [1, [Validators.required, Validators.min(0), Validators.max(100)]],
    roomTypeId: ['', Validators.required],
  });

  editForm = this.fb.group({
    roomId:     [''],
    roomNumber: ['', [Validators.required, Validators.maxLength(20)]],
    floor:      [1, [Validators.required, Validators.min(0), Validators.max(100)]],
    roomTypeId: ['', Validators.required],
  });

  ngOnInit() {
    this.loadRooms();
    this.roomTypeService.getRoomTypes().subscribe((res: any) => {
      this.roomTypes.set(Array.isArray(res) ? res : res.roomTypes ?? []);
    });
    this.searchSubject.pipe(debounceTime(400), distinctUntilChanged())
      .subscribe(() => { this.currentPage = 1; this.loadRooms(); });
  }

  loadRooms() {
    this.loading.set(true);
    this.roomService.getRooms(this.currentPage, this.pageSize).subscribe({
      next: (res: any) => {
        // Handle both array and paged response
        if (Array.isArray(res)) {
          this.rooms.set(res);
          this.totalCount.set(res.length);
        } else {
          this.rooms.set(res.items ?? res.rooms ?? []);
          this.totalCount.set(res.totalCount ?? 0);
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onPage(e: PageEvent) { this.currentPage = e.pageIndex + 1; this.pageSize = e.pageSize; this.loadRooms(); window.scrollTo({ top: 0, behavior: 'smooth' }); }

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
      this.loadRooms();
    });
  }

  onOccupancyDateChange(date: Date | null) {
    if (!date) return;
    this.occupancyDate.set(date);
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    const dateStr = `${y}-${m}-${d}`;
    this.roomService.getRoomOccupancy(dateStr).subscribe(data => this.occupancyRooms.set(data));
  }
}