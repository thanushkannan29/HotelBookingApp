import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { UserService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { AuthService } from '../../../core/services/auth.service';
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
  private auth = inject(AuthService);
  private fb = inject(FormBuilder);

  profile = signal<UserProfileResponseDto | null>(null);
  isEditing = signal(false);
  isSaving = signal(false);
  readonly reviewRewardPoints = 10;

  // F2D: separate FormControl for city autocomplete
  cityControl = new FormControl('');

  get stateFormControl() { return this.form.get('state') as FormControl; }

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
      this.cityControl.setValue(p.city ?? '');
      this.auth.updateProfileImage(p.profileImageUrl ?? null);
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
        if (updated.name) this.auth.updateUserName(updated.name);
        this.auth.updateProfileImage(updated.profileImageUrl ?? null);
      },
      error: () => this.isSaving.set(false),
    });
  }
}