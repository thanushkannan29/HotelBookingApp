import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { UserService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { UserProfileResponseDto } from '../../../core/models/models';
import { CityAutocompleteComponent } from '../../../shared/components/city-autocomplete/city-autocomplete.component';

@Component({
  selector: 'app-guest-profile',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule,
    CityAutocompleteComponent,
  ],
  templateUrl: './guest-profile.component.html',
  styleUrl: './guest-profile.component.scss'
})
export class GuestProfileComponent implements OnInit {
  private userService = inject(UserService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  profile = signal<UserProfileResponseDto | null>(null);
  isEditing = signal(false);
  isSaving = signal(false);

  // F2D: separate FormControl for city autocomplete
  cityControl = new FormControl('');

  form = this.fb.group({
    name:           [''],
    phoneNumber:    ['', [Validators.maxLength(15)]],
    address:        [''],
    state:          [''],
    pincode:        [''],
    profileImageUrl:[''],
  });

  ngOnInit() {
    this.userService.getProfile().subscribe(p => {
      this.profile.set(p);
      this.form.patchValue({
        name: p.name, phoneNumber: p.phoneNumber,
        address: p.address, state: p.state,
        pincode: p.pincode,
        profileImageUrl: p.profileImageUrl ?? '',
      });
      // F2D: patch city separately
      this.cityControl.setValue(p.city ?? '');
    });
  }

  save() {
    this.isSaving.set(true);
    const payload = {
      ...this.form.value,
      city: this.cityControl.value,
    };
    this.userService.updateProfile(payload as any).subscribe({
      next: updated => {
        this.profile.set(updated);
        this.isEditing.set(false);
        this.isSaving.set(false);
        this.toast.success('Profile updated successfully.');
      },
      error: () => this.isSaving.set(false),
    });
  }
}