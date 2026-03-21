import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { UserService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { UserProfileResponseDto } from '../../../core/models/models';

@Component({
  selector: 'app-guest-profile',
  standalone: true,
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule],
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

  form = this.fb.group({
    name:           [''],
    phoneNumber:    ['', [Validators.maxLength(15)]],
    address:        [''],
    state:          [''],
    city:           [''],
    pincode:        [''],
    profileImageUrl:[''],
  });

  ngOnInit() {
    this.userService.getProfile().subscribe(p => {
      this.profile.set(p);
      this.form.patchValue({
        name: p.name, phoneNumber: p.phoneNumber,
        address: p.address, state: p.state,
        city: p.city, pincode: p.pincode,
        profileImageUrl: p.profileImageUrl ?? '',
      });
    });
  }

  save() {
    this.isSaving.set(true);
    this.userService.updateProfile(this.form.value as any).subscribe({
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
