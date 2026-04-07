import { Component, inject, signal, OnInit } from '@angular/core';
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
import { MatExpansionModule } from '@angular/material/expansion';
import { RouterLink } from '@angular/router';
import { RoomTypeService, AmenityService } from '../../../core/services/api.services';
import { AmenityRequestService } from '../../../core/services/amenity-request.service';
import { ToastService } from '../../../core/services/toast.service';
import { RoomTypeListDto, AmenityResponseDto, RoomTypeRateDto } from '../../../core/models/models';

@Component({
  selector: 'app-roomtype-management',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink,
    MatFormFieldModule, MatInputModule, MatButtonModule,
    MatIconModule, MatTooltipModule, MatSelectModule,
    MatDatepickerModule, MatNativeDateModule,
    MatTableModule, MatSortModule, MatPaginatorModule,
    MatProgressSpinnerModule, MatChipsModule, MatExpansionModule,
  ],
  templateUrl: './roomtype-management.component.html',
  styleUrl: './roomtype-management.component.scss'
})
export class RoomTypeManagementComponent implements OnInit {
  private roomTypeService   = inject(RoomTypeService);
  private amenityService    = inject(AmenityService);
  private amenityReqService = inject(AmenityRequestService);
  private toast             = inject(ToastService);
  private fb                = inject(FormBuilder);

  roomTypes    = signal<RoomTypeListDto[]>([]);
  amenities    = signal<AmenityResponseDto[]>([]);
  totalCount   = signal(0);
  loading      = signal(false);
  showAddForm  = signal(false);
  editingId    = signal<string | null>(null);
  isSaving     = signal(false);
  showAmenityReqForm = signal(false);
  today        = new Date();
  pageSize     = 10;
  currentPage  = 1;
  displayedColumns = ['name', 'maxOccupancy', 'amenities', 'roomCount', 'isActive', 'actions'];
  rateColumns  = ['dateRange', 'rate', 'actions'];

  // Rate management per room type
  ratesMap     = signal<Record<string, RoomTypeRateDto[]>>({});
  loadingRates = signal<Record<string, boolean>>({});
  expandedRateId = signal<string | null>(null);

  // Add rate form
  showAddRateFor = signal<string | null>(null);
  addRateForm = this.fb.group({
    roomTypeId: ['', Validators.required],
    startDate:  [null as Date | null, Validators.required],
    endDate:    [null as Date | null, Validators.required],
    rate:       [0, [Validators.required, Validators.min(1), Validators.max(99999)]],
  });

  // Edit rate form
  editingRateId = signal<string | null>(null);
  editRateForm = this.fb.group({
    roomTypeRateId: [''],
    startDate:  [null as Date | null, Validators.required],
    endDate:    [null as Date | null, Validators.required],
    rate:       [0, [Validators.required, Validators.min(1), Validators.max(99999)]],
  });

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
    this.roomTypeService.getRoomTypes(this.currentPage, this.pageSize).subscribe((res: any) => {
      const types = Array.isArray(res) ? res : res.roomTypes ?? [];
      this.roomTypes.set(types);
      this.totalCount.set(Array.isArray(res) ? res.length : res.totalCount ?? 0);
      this.loading.set(false);
    });
  }

  onPage(e: PageEvent) { this.currentPage = e.pageIndex + 1; this.pageSize = e.pageSize; this.load(); window.scrollTo({ top: 0, behavior: 'smooth' }); }

  private fmt(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  // ── RATES ─────────────────────────────────────────────────────────────────
  toggleRates(rtId: string) {
    if (this.expandedRateId() === rtId) {
      this.expandedRateId.set(null);
      return;
    }
    this.expandedRateId.set(rtId);
    this.loadRates(rtId);
  }

  loadRates(rtId: string) {
    this.loadingRates.update(m => ({ ...m, [rtId]: true }));
    this.roomTypeService.getRates(rtId).subscribe({
      next: (rates: any) => {
        this.ratesMap.update(m => ({ ...m, [rtId]: rates }));
        this.loadingRates.update(m => ({ ...m, [rtId]: false }));
      },
      error: () => this.loadingRates.update(m => ({ ...m, [rtId]: false }))
    });
  }

  openAddRate(rtId: string) {
    this.showAddRateFor.set(rtId);
    this.editingRateId.set(null);
    this.addRateForm.reset({ roomTypeId: rtId, rate: 0 });
  }

  saveAddRate() {
    if (this.addRateForm.invalid) { this.addRateForm.markAllAsTouched(); return; }
    const { roomTypeId, startDate, endDate, rate } = this.addRateForm.value;
    this.isSaving.set(true);
    this.roomTypeService.addRate({
      roomTypeId: roomTypeId!,
      startDate:  this.fmt(startDate!),
      endDate:    this.fmt(endDate!),
      rate:       rate!,
    }).subscribe({
      next: () => {
        this.toast.success('Rate added.');
        this.showAddRateFor.set(null);
        this.isSaving.set(false);
        this.loadRates(roomTypeId!);
      },
      error: () => this.isSaving.set(false),
    });
  }

  startEditRate(rate: RoomTypeRateDto) {
    this.editingRateId.set(rate.roomTypeRateId);
    this.showAddRateFor.set(null);
    this.editRateForm.patchValue({
      roomTypeRateId: rate.roomTypeRateId,
      startDate: new Date(rate.startDate),
      endDate:   new Date(rate.endDate),
      rate:      rate.rate,
    });
  }

  saveEditRate(rtId: string) {
    if (this.editRateForm.invalid) return;
    const { roomTypeRateId, startDate, endDate, rate } = this.editRateForm.value;
    this.isSaving.set(true);
    this.roomTypeService.updateRate({
      roomTypeRateId: roomTypeRateId!,
      startDate: this.fmt(startDate!),
      endDate:   this.fmt(endDate!),
      rate:      rate!,
    }).subscribe({
      next: () => {
        this.toast.success('Rate updated.');
        this.editingRateId.set(null);
        this.isSaving.set(false);
        this.loadRates(rtId);
      },
      error: () => this.isSaving.set(false),
    });
  }

  cancelEditRate() { this.editingRateId.set(null); }

  getRatesFor(rtId: string): RoomTypeRateDto[] {
    return this.ratesMap()[rtId] ?? [];
  }

  isLoadingRates(rtId: string): boolean {
    return this.loadingRates()[rtId] ?? false;
  }

  // ── ROOM TYPE CRUD ────────────────────────────────────────────────────────
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
    return '—';
  }
}
