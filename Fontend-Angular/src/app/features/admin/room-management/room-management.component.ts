import { Component, inject, signal, OnInit, AfterViewInit, ViewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatTabsModule } from '@angular/material/tabs';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { RoomService, RoomTypeService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { RoomListResponseDto, RoomTypeListDto, RoomOccupancyDto } from '../../../core/models/models';

@Component({
  selector: 'app-room-management',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink, DatePipe,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatSlideToggleModule, MatTooltipModule,
    MatTableModule, MatSortModule, MatPaginatorModule,
    MatTabsModule, MatDatepickerModule, MatNativeDateModule,
  ],
  templateUrl: './room-management.component.html',
  styleUrl: './room-management.component.scss'
})
export class RoomManagementComponent implements OnInit, AfterViewInit {
  private roomService = inject(RoomService);
  private roomTypeService = inject(RoomTypeService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  roomTypes = signal<RoomTypeListDto[]>([]);
  showAddForm = signal(false);
  editingRoom = signal<RoomListResponseDto | null>(null);
  isSaving = signal(false);

  // F5: MatTableDataSource for rooms
  dataSource = new MatTableDataSource<RoomListResponseDto>([]);
  displayedColumns = ['roomNumber', 'floor', 'roomTypeName', 'isActive', 'actions'];

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  // F7E: Occupancy
  occupancyDataSource = new MatTableDataSource<RoomOccupancyDto>([]);
  occupancyColumns = ['roomNumber', 'floor', 'roomTypeName', 'status', 'reservationCode'];
  occupancyDate = signal<Date | null>(null);
  today = new Date();

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

  ngAfterViewInit() {
    this.dataSource.sort = this.sort;
    this.dataSource.paginator = this.paginator;
  }

  loadRooms() {
    this.roomService.getRooms(1, 200).subscribe(r => {
      this.dataSource.data = r;
    });
  }

  applyFilter(event: Event) {
    const val = (event.target as HTMLInputElement).value;
    this.dataSource.filter = val.trim().toLowerCase();
    if (this.dataSource.paginator) this.dataSource.paginator.firstPage();
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
      this.dataSource.data = this.dataSource.data.map(x =>
        x.roomId === room.roomId ? { ...x, isActive: !x.isActive } : x
      );
    });
  }

  // F7E: Load occupancy for a date
  onOccupancyDateChange(date: Date | null) {
    if (!date) return;
    this.occupancyDate.set(date);
    const dateStr = date.toISOString().split('T')[0];
    this.roomService.getRoomOccupancy(dateStr).subscribe(data => {
      this.occupancyDataSource.data = data;
    });
  }

  applyOccupancyFilter(event: Event) {
    const val = (event.target as HTMLInputElement).value;
    this.occupancyDataSource.filter = val.trim().toLowerCase();
  }
}