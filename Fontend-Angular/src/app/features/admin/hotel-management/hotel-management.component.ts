import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { HotelService } from '../../../core/services/hotel.service';
import { DashboardService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { AdminDashboardDto } from '../../../core/models/models';

@Component({
  selector: 'app-hotel-management',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink,
    MatFormFieldModule, MatInputModule, MatButtonModule,
    MatIconModule, MatProgressSpinnerModule
  ],
  templateUrl: './hotel-management.component.html',
  styleUrl: './hotel-management.component.scss'
})
export class HotelManagementComponent implements OnInit {
  private hotelService = inject(HotelService);
  private dashboardService = inject(DashboardService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  isSaving  = signal(false);
  isLoading = signal(true);
  dashboard = signal<AdminDashboardDto | null>(null);

  form = this.fb.group({
    name:          ['', Validators.required],
    address:       ['', Validators.required],
    city:          ['', Validators.required],
    description:   [''],
    contactNumber: ['', Validators.required],
    imageUrl:      [''],
  });

  ngOnInit() {
    // Load dashboard to get hotel info, then fetch full details to pre-fill all fields
    this.dashboardService.getAdminDashboard().subscribe(d => {
      this.dashboard.set(d);
      // Fetch full hotel details to get address, description etc.
      this.hotelService.getHotelDetails(d.hotelId).subscribe(hotel => {
        this.form.patchValue({
          name:          hotel.name,
          address:       hotel.address,
          city:          hotel.city,
          description:   hotel.description,
          contactNumber: hotel.contactNumber,
          imageUrl:      hotel.imageUrl,
        });
        this.isLoading.set(false);
      });
    });
  }

  save() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.isSaving.set(true);
    this.hotelService.updateHotel(this.form.value as any).subscribe({
      next: () => {
        this.toast.success('Hotel updated successfully.');
        this.isSaving.set(false);
      },
      error: () => this.isSaving.set(false),
    });
  }
}
