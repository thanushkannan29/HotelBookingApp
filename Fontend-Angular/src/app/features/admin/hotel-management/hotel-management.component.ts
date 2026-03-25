import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
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
  private hotelService = inject(HotelService);
  private dashboardService = inject(DashboardService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  isSaving  = signal(false);
  isLoading = signal(true);
  dashboard = signal<AdminDashboardDto | null>(null);

  // F2D: separate FormControl for city autocomplete
  cityControl = new FormControl('', [Validators.required]);

  form = this.fb.group({
    name:          ['', Validators.required],
    address:       ['', Validators.required],
    description:   [''],
    contactNumber: ['', Validators.required],
    imageUrl:      [''],
    // F7D: UPI ID field
    upiId:         [''],
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
          upiId:         (hotel as any).upiId ?? '',
        });
        // F2D: patch city control separately
        this.cityControl.setValue(hotel.city);
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
    const payload = {
      ...this.form.value,
      city: this.cityControl.value,
    };
    this.hotelService.updateHotel(payload as any).subscribe({
      next: () => {
        this.toast.success('Hotel updated successfully.');
        this.isSaving.set(false);
      },
      error: () => this.isSaving.set(false),
    });
  }
}