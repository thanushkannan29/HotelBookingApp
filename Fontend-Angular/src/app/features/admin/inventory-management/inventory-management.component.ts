import { Component, inject, signal, OnInit, ViewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatTableModule } from '@angular/material/table';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { InventoryService, RoomTypeService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { InventoryResponseDto, RoomTypeListDto } from '../../../core/models/models';

@Component({
  selector: 'app-inventory-management',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, RouterLink, DatePipe,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatTooltipModule,
    MatDatepickerModule, MatNativeDateModule,
    MatTableModule, MatPaginatorModule, MatChipsModule, MatProgressSpinnerModule,
  ],
  templateUrl: './inventory-management.component.html',
  styleUrl: './inventory-management.component.scss'
})
export class InventoryManagementComponent implements OnInit {
  private inventoryService = inject(InventoryService);
  private roomTypeService  = inject(RoomTypeService);
  private toast            = inject(ToastService);
  private fb               = inject(FormBuilder);

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  roomTypes       = signal<RoomTypeListDto[]>([]);
  allInventories  = signal<InventoryResponseDto[]>([]);  // full list from API
  inventories     = signal<InventoryResponseDto[]>([]);  // current page slice
  totalCount      = signal(0);
  isLoading       = signal(false);
  isSaving        = signal(false);
  editingId       = signal<string | null>(null);
  editValue       = signal(0);
  today           = new Date();
  pageSize        = 10;
  currentPage     = 1;
  displayedColumns = ['date', 'totalInventory', 'reservedInventory', 'available', 'actions'];

  addForm = this.fb.group({
    roomTypeId:     ['', Validators.required],
    startDate:      [null as Date | null, Validators.required],
    endDate:        [null as Date | null, Validators.required],
    totalInventory: [1, [Validators.required, Validators.min(1)]],
  });

  viewForm = this.fb.group({
    roomTypeId: ['', Validators.required],
    start:      [null as Date | null, Validators.required],
    end:        [null as Date | null, Validators.required],
  });

  ngOnInit() {
    this.roomTypeService.getRoomTypes().subscribe((res: any) => {
      this.roomTypes.set(Array.isArray(res) ? res : (res.roomTypes ?? []));
    });
  }

  private fmt(d: Date): string {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  private applyPage() {
    const all = this.allInventories();
    const start = (this.currentPage - 1) * this.pageSize;
    this.inventories.set(all.slice(start, start + this.pageSize));
    this.totalCount.set(all.length);
  }

  loadInventory() {
    const { roomTypeId, start, end } = this.viewForm.value;
    if (!roomTypeId || !start || !end) return;
    this.isLoading.set(true);
    this.currentPage = 1;
    this.paginator?.firstPage();
    this.inventoryService.getInventory(roomTypeId, this.fmt(start), this.fmt(end)).subscribe({
      next: (res: any) => {
        const all: InventoryResponseDto[] = Array.isArray(res) ? res : (res.inventory ?? []);
        this.allInventories.set(all);
        this.applyPage();
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  onPage(e: PageEvent) {
    this.currentPage = e.pageIndex + 1;
    this.pageSize    = e.pageSize;
    this.applyPage();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  addInventory() {
    if (this.addForm.invalid) { this.addForm.markAllAsTouched(); return; }
    const { roomTypeId, startDate, endDate, totalInventory } = this.addForm.value;
    this.isSaving.set(true);
    this.inventoryService.addInventory({
      roomTypeId: roomTypeId!,
      startDate:  this.fmt(startDate!),
      endDate:    this.fmt(endDate!),
      totalInventory: totalInventory!,
    }).subscribe({
      next: () => {
        this.toast.success('Inventory set successfully.');
        this.addForm.patchValue({ startDate: null, endDate: null });
        this.isSaving.set(false);
        this.loadInventory();
      },
      error: () => this.isSaving.set(false),
    });
  }

  startEditInv(inv: InventoryResponseDto) {
    this.editingId.set(inv.roomTypeInventoryId);
    this.editValue.set(inv.totalInventory);
  }

  saveEditInv(inv: InventoryResponseDto) {
    this.inventoryService.updateInventory({
      roomTypeInventoryId: inv.roomTypeInventoryId,
      totalInventory: this.editValue(),
    }).subscribe({
      next: () => {
        this.toast.success('Inventory updated.');
        this.editingId.set(null);
        this.loadInventory();
      }
    });
  }
}
