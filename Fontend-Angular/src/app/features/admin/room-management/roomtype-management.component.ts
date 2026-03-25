import { Component, inject, signal, OnInit, AfterViewInit, ViewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule } from '@angular/material/sort';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { RouterLink } from '@angular/router';
import { RoomTypeService, AmenityService } from '../../../core/services/api.services';
import { AmenityRequestService } from '../../../core/services/amenity-request.service';
import { ToastService } from '../../../core/services/toast.service';
import { RoomTypeListDto, AmenityResponseDto } from '../../../core/models/models';

@Component({
  selector: 'app-roomtype-management',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink,
    MatFormFieldModule, MatInputModule, MatButtonModule,
    MatIconModule, MatTooltipModule, MatSelectModule,
    MatDatepickerModule, MatNativeDateModule,
    MatTableModule, MatSortModule, MatPaginatorModule,
    MatProgressSpinnerModule, MatChipsModule,
  ],
  templateUrl: './roomtype-management.component.html',
  styleUrl: './roomtype-management.component.scss'
})
export class RoomTypeManagementComponent implements OnInit {
  private roomTypeService    = inject(RoomTypeService);
  private amenityService     = inject(AmenityService);
  private amenityReqService  = inject(AmenityRequestService);
  private toast              = inject(ToastService);
  private fb                 = inject(FormBuilder);

  roomTypes    = signal<RoomTypeListDto[]>([]);
  amenities    = signal<AmenityResponseDto[]>([]);
  totalCount   = signal(0);
  loading      = signal(false);
  showAddForm  = signal(false);
  editingId    = signal<string | null>(null);
  isSaving     = signal(false);
  showRateForm = signal<string | null>(null);
  showAmenityReqForm = signal(false);
  today        = new Date();
  pageSize     = 10;
  currentPage  = 1;
  displayedColumns = ['name', 'maxOccupancy', 'amenities', 'roomCount', 'isActive', 'actions'];

  addForm = this.fb.group({
    name:         ['', Validators.required],
    description:  [''],
    maxOccupancy: [2, [Validators.required, Validators.min(1)]],
    amenityIds:   [[] as string[]],
    imageUrl:     [''],
  });

  editForm = this.fb.group({
    roomTypeId:   [''],
    name:         ['', Validators.required],
    description:  [''],
    maxOccupancy: [2, [Validators.required, Validators.min(1)]],
    amenityIds:   [[] as string[]],
    imageUrl:     [''],
  });

  rateForm = this.fb.group({
    roomTypeId: ['', Validators.required],
    startDate:  [null as Date | null, Validators.required],
    endDate:    [null as Date | null, Validators.required],
    rate:       [0, [Validators.required, Validators.min(1), Validators.max(99999)]],
  });

  amenityReqForm = this.fb.group({
    amenityName: ['', Validators.required],
    category:    ['', Validators.required],
    iconName:    [''],
  });

  ngOnInit() {
    this.load();
    this.amenityService.getAmenities().subscribe(a => this.amenities.set(a));
  }

  load() {
    this.loading.set(true);
    this.roomTypeService.getRoomTypes().subscribe((res: any) => {
      if (Array.isArray(res)) {
        this.roomTypes.set(res);
        this.totalCount.set(res.length);
      } else {
        this.roomTypes.set(res.roomTypes ?? []);
        this.totalCount.set(res.totalCount ?? 0);
      }
      this.loading.set(false);
    });
  }

  onPage(e: PageEvent) { this.currentPage = e.pageIndex + 1; this.pageSize = e.pageSize; this.load(); }

  private formatDate(d: Date): string { return d.toISOString().split('T')[0]; }

  add() {
    if (this.addForm.invalid) { this.addForm.markAllAsTouched(); return; }
    this.isSaving.set(true);
    this.roomTypeService.addRoomType(this.addForm.value as any).subscribe({
      next: () => {
        this.toast.success('Room type added.');
        this.addForm.reset({ maxOccupancy: 2, amenityIds: [] });
        this.showAddForm.set(false);
        this.load();
        this.isSaving.set(false);
      },
      error: () => this.isSaving.set(false),
    });
  }

  startEdit(rt: RoomTypeListDto) {
    this.editingId.set(rt.roomTypeId);
    const amenityIds = rt.amenityList?.map(a => a.amenityId) ?? [];
    this.editForm.patchValue({
      roomTypeId: rt.roomTypeId, name: rt.name, description: rt.description,
      maxOccupancy: rt.maxOccupancy, amenityIds, imageUrl: rt.imageUrl ?? '',
    });
  }

  saveEdit() {
    if (this.editForm.invalid) return;
    this.isSaving.set(true);
    this.roomTypeService.updateRoomType(this.editForm.value as any).subscribe({
      next: () => {
        this.toast.success('Room type updated.');
        this.editingId.set(null);
        this.load();
        this.isSaving.set(false);
      },
      error: () => this.isSaving.set(false),
    });
  }

  toggleStatus(rt: RoomTypeListDto) {
    this.roomTypeService.toggleRoomTypeStatus(rt.roomTypeId, !rt.isActive).subscribe(() => {
      this.toast.success(`Room type ${!rt.isActive ? 'activated' : 'deactivated'}.`);
      this.load();
    });
  }

  openRateForm(rtId: string) {
    this.showRateForm.set(rtId);
    this.rateForm.patchValue({ roomTypeId: rtId });
  }

  addRate() {
    if (this.rateForm.invalid) { this.rateForm.markAllAsTouched(); return; }
    const { roomTypeId, startDate, endDate, rate } = this.rateForm.value;
    this.isSaving.set(true);
    this.roomTypeService.addRate({
      roomTypeId: roomTypeId!,
      startDate:  this.formatDate(startDate!),
      endDate:    this.formatDate(endDate!),
      rate:       rate!,
    }).subscribe({
      next: () => {
        this.toast.success('Rate added.');
        this.showRateForm.set(null);
        this.rateForm.patchValue({ startDate: null, endDate: null, rate: 0 });
        this.isSaving.set(false);
      },
      error: () => this.isSaving.set(false),
    });
  }

  submitAmenityRequest() {
    if (this.amenityReqForm.invalid) return;
    this.amenityReqService.create(this.amenityReqForm.value as any).subscribe({
      next: () => {
        this.toast.success('Amenity request submitted!');
        this.showAmenityReqForm.set(false);
        this.amenityReqForm.reset();
      }
    });
  }

  getAmenityNames(rt: RoomTypeListDto): string {
    if (rt.amenityList?.length) return rt.amenityList.map(a => a.name).join(', ');
    return rt.amenities || '—';
  }
}