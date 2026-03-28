import { Component, inject, signal } from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatStepperModule } from '@angular/material/stepper';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { CityAutocompleteComponent } from '../../../shared/components/city-autocomplete/city-autocomplete.component';

@Component({
  selector: 'app-register-admin',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterLink,
    MatFormFieldModule, MatInputModule, MatButtonModule,
    MatIconModule, MatStepperModule,
    CityAutocompleteComponent,
  ],
  templateUrl: './register-admin.component.html',
  styleUrl: './register-admin.component.scss'
})
export class RegisterAdminComponent {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);
  private toast = inject(ToastService);

  hidePassword = signal(true);
  isLoading = signal(false);

  // F2D: separate FormControl for city autocomplete
  cityControl = new FormControl('', [Validators.required]);
  stateControl = new FormControl('');

  adminForm = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(150)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [
      Validators.required,
      Validators.minLength(8),
      Validators.pattern(/^(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]).{8,}$/)
    ]],
  });

  hotelForm = this.fb.group({
    hotelName: ['', [Validators.required]],
    address: ['', [Validators.required]],
    description: [''],
    contactNumber: ['', [Validators.required, Validators.maxLength(15)]],
  });

  submit() {
    if (this.adminForm.invalid || this.hotelForm.invalid || this.cityControl.invalid) {
      this.adminForm.markAllAsTouched();
      this.hotelForm.markAllAsTouched();
      this.cityControl.markAsTouched();
      return;
    }
    this.isLoading.set(true);
    const payload = {
      ...this.adminForm.value,
      ...this.hotelForm.value,
      city: this.cityControl.value,
      state: this.stateControl.value,
    } as any;
    this.auth.registerHotelAdmin(payload).subscribe({
      next: () => {
        this.toast.success('Hotel registered! Your dashboard is ready.');
        this.router.navigate(['/admin/dashboard']);
      },
      error: () => this.isLoading.set(false),
      complete: () => this.isLoading.set(false),
    });
  }
}