import { Component, inject, signal, OnInit, AfterViewInit, ViewChild } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';

import { InventoryService, RoomTypeService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { InventoryResponseDto, RoomTypeListDto } from '../../../core/models/models';

@Component({
  selector: 'app-inventory-management',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink, DatePipe,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatTooltipModule,
    MatDatepickerModule, MatNativeDateModule,
    MatTableModule, MatSortModule, MatPaginatorModule,
  ],
  templateUrl: './inventory-management.component.html',
  styleUrl: './inventory-management.component.scss'
})
export class InventoryManagementComponent implements OnInit, AfterViewInit {

  private inventoryService = inject(InventoryService);
  private roomTypeService  = inject(RoomTypeService);
  private toast            = inject(ToastService);
  private fb               = inject(FormBuilder);

  roomTypes  = signal<RoomTypeListDto[]>([]);
  isSaving   = signal(false);
  editingId  = signal<string | null>(null);
  editValue  = signal(0);
  today      = new Date();

  dataSource = new MatTableDataSource<InventoryResponseDto>([]);
  displayedColumns = ['date', 'totalInventory', 'reservedInventory', 'available', 'actions'];

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

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

  // ✅ FIXED: handle paged response safely
  ngOnInit() {
    this.roomTypeService.getRoomTypes().subscribe((res: any) => {
      console.log('RoomTypes API:', res);

      // handle BOTH cases (array OR paged object)
      if (Array.isArray(res)) {
        this.roomTypes.set(res);
      } else {
        this.roomTypes.set(res.roomTypes ?? []);
      }
    });
  }

  ngAfterViewInit() {
    this.dataSource.sort = this.sort;
    this.dataSource.paginator = this.paginator;
  }

  private formatDate(d: Date): string {
    return d.toISOString().split('T')[0];
  }

  // ✅ FIXED: ensure array always passed to table
  loadInventory() {
    const { roomTypeId, start, end } = this.viewForm.value;
    if (!roomTypeId || !start || !end) return;

    this.inventoryService
      .getInventory(roomTypeId, this.formatDate(start), this.formatDate(end))
      .subscribe((res: any) => {
        console.log('Inventory API:', res);

        if (Array.isArray(res)) {
          this.dataSource.data = res;
        } else {
          this.dataSource.data = res.inventory ?? [];
        }
      });
  }

  addInventory() {
    if (this.addForm.invalid) {
      this.addForm.markAllAsTouched();
      return;
    }

    const { roomTypeId, startDate, endDate, totalInventory } = this.addForm.value;

    this.isSaving.set(true);

    this.inventoryService.addInventory({
      roomTypeId:     roomTypeId!,
      startDate:      this.formatDate(startDate!),
      endDate:        this.formatDate(endDate!),
      totalInventory: totalInventory!,
    }).subscribe({
      next: () => {
        this.toast.success('Inventory added.');
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
    }).subscribe(() => {
      this.toast.success('Inventory updated.');
      this.editingId.set(null);
      this.loadInventory();
    });
  }
}
