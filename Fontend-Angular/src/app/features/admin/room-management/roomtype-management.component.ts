import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { RouterLink } from '@angular/router';
import { RoomTypeService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { RoomTypeListDto } from '../../../core/models/models';

@Component({
  selector: 'app-roomtype-management',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink,
    MatFormFieldModule, MatInputModule, MatButtonModule,
    MatIconModule, MatTooltipModule,
    MatDatepickerModule, MatNativeDateModule
  ],
  templateUrl: './roomtype-management.component.html',
  styleUrl: './roomtype-management.component.scss'
})
export class RoomTypeManagementComponent implements OnInit {
  private roomTypeService = inject(RoomTypeService);
  private toast           = inject(ToastService);
  private fb              = inject(FormBuilder);

  roomTypes    = signal<RoomTypeListDto[]>([]);
  showAddForm  = signal(false);
  editingId    = signal<string | null>(null);
  isSaving     = signal(false);
  showRateForm = signal<string | null>(null);
  today        = new Date();

  addForm = this.fb.group({
    name:         ['', Validators.required],
    description:  [''],
    maxOccupancy: [2, [Validators.required, Validators.min(1)]],
    amenities:    [''],
  });

  editForm = this.fb.group({
    roomTypeId:   [''],
    name:         ['', Validators.required],
    description:  [''],
    maxOccupancy: [2, [Validators.required, Validators.min(1)]],
    amenities:    [''],
  });

  rateForm = this.fb.group({
    roomTypeId: ['', Validators.required],
    startDate:  [null as Date | null, Validators.required],
    endDate:    [null as Date | null, Validators.required],
    rate:       [0, [Validators.required, Validators.min(1)]],
  });

  ngOnInit() { this.load(); }

  load() {
    this.roomTypeService.getRoomTypes().subscribe(rt => this.roomTypes.set(rt));
  }

  private formatDate(d: Date): string {
    return d.toISOString().split('T')[0];
  }

  add() {
    if (this.addForm.invalid) { this.addForm.markAllAsTouched(); return; }
    this.isSaving.set(true);
    this.roomTypeService.addRoomType(this.addForm.value as any).subscribe({
      next: () => {
        this.toast.success('Room type added.');
        this.addForm.reset({ maxOccupancy: 2 });
        this.showAddForm.set(false);
        this.load();
        this.isSaving.set(false);
      },
      error: () => this.isSaving.set(false),
    });
  }

  startEdit(rt: RoomTypeListDto) {
    this.editingId.set(rt.roomTypeId);
    this.editForm.patchValue({
      roomTypeId: rt.roomTypeId,
      name: rt.name,
      description: rt.description,
      maxOccupancy: rt.maxOccupancy,
      amenities: rt.amenities,
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
      this.roomTypes.update(r =>
        r.map(x => x.roomTypeId === rt.roomTypeId ? { ...x, isActive: !x.isActive } : x)
      );
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
        this.toast.success('Rate added successfully.');
        this.showRateForm.set(null);
        this.rateForm.patchValue({ startDate: null, endDate: null, rate: 0 });
        this.isSaving.set(false);
      },
      error: () => this.isSaving.set(false),
    });
  }
}
