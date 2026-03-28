import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { HotelService } from '../../../core/services/hotel.service';
import { DashboardService, RoomTypeService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { AdminDashboardDto } from '../../../core/models/models';
import { CityAutocompleteComponent } from '../../../shared/components/city-autocomplete/city-autocomplete.component';

@Component({
  selector: 'app-hotel-management',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink,
    MatFormFieldModule, MatInputModule, MatButtonModule,
    MatIconModule, MatProgressSpinnerModule,
    CityAutocompleteComponent,
  ],
  templateUrl: './hotel-management.component.html',
  styleUrl: './hotel-management.component.scss'
})
export class HotelManagementComponent implements OnInit {
  private hotelService     = inject(HotelService);
  private dashboardService = inject(DashboardService);
  private roomTypeService  = inject(RoomTypeService);
  private toast            = inject(ToastService);
  private fb               = inject(FormBuilder);

  isSaving    = signal(false);
  isSavingGst = signal(false);
  isLoading   = signal(true);
  dashboard   = signal<AdminDashboardDto | null>(null);

  cityControl = new FormControl('', [Validators.required]);
  stateControl = new FormControl('');

  form = this.fb.group({
    name:          ['', [Validators.required, Validators.maxLength(200)]],
    address:       ['', [Validators.required, Validators.maxLength(500)]],
    description:   [''],
    contactNumber: ['', [Validators.required, Validators.pattern(/^\d{10}$/)]],
    imageUrl:      [''],
    upiId:         ['', Validators.pattern(/^[a-zA-Z0-9._-]+@[a-zA-Z]+$/)],
  });

  gstForm = this.fb.group({
    gstPercent: [0, [Validators.required, Validators.min(0), Validators.max(28)]],
  });

  ngOnInit() {
    this.dashboardService.getAdminDashboard().subscribe(d => {
      this.dashboard.set(d);
      this.hotelService.getHotelDetails(d.hotelId).subscribe(hotel => {
        this.form.patchValue({
          name:          hotel.name,
          address:       hotel.address,
          description:   hotel.description,
          contactNumber: hotel.contactNumber,
          imageUrl:      hotel.imageUrl,
          upiId:         hotel.upiId ?? '',
        });
        this.gstForm.patchValue({ gstPercent: hotel.gstPercent ?? 0 });
        this.cityControl.setValue(hotel.city);
        this.stateControl.setValue(hotel.state ?? '');
        this.isLoading.set(false);
      });
    });
  }

  save() {
    if (this.form.invalid || this.cityControl.invalid) {
      this.form.markAllAsTouched();
      this.cityControl.markAsTouched();
      return;
    }
    this.isSaving.set(true);
    const payload = { ...this.form.value, city: this.cityControl.value, state: this.stateControl.value };
    this.hotelService.updateHotel(payload as any).subscribe({
      next: () => { this.toast.success('Hotel updated successfully.'); this.isSaving.set(false); },
      error: () => this.isSaving.set(false),
    });
  }

  saveGst() {
    if (this.gstForm.invalid) return;
    this.isSavingGst.set(true);
    this.roomTypeService.updateHotelGst(this.gstForm.value.gstPercent!).subscribe({
      next: () => { this.toast.success('GST updated!'); this.isSavingGst.set(false); },
      error: () => this.isSavingGst.set(false),
    });
  }
}