import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { UserService } from '../../../core/services/api.services';
import { ToastService } from '../../../core/services/toast.service';
import { AuthService } from '../../../core/services/auth.service';
import { UserProfileResponseDto } from '../../../core/models/models';

@Component({
  selector: 'app-superadmin-profile',
  standalone: true,
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatCardModule],
  template: `
    <div class="page-wrapper">
      <div class="container" style="max-width:600px;">
        <h1 class="section-title">👤 My Profile</h1>

        @if (profile(); as p) {
          <mat-card style="margin-bottom:24px;">
            <mat-card-content style="display:flex;align-items:center;gap:20px;padding:20px;">
              <div class="profile-avatar">
                @if (p.profileImageUrl) {
                  <img [src]="p.profileImageUrl" alt="profile" style="width:100%;height:100%;object-fit:cover;border-radius:50%;" />
                } @else {
                  <span>{{ p.name.charAt(0).toUpperCase() }}</span>
                }
              </div>
              <div>
                <div style="font-size:1.2rem;font-weight:700;">{{ p.name }}</div>
                <div style="color:#888;font-size:0.875rem;">{{ p.email }}</div>
                <div style="color:#1565c0;font-size:0.8rem;margin-top:4px;">SuperAdmin</div>
              </div>
            </mat-card-content>
          </mat-card>
        }

        @if (!isEditing()) {
          <button mat-flat-button color="primary" (click)="isEditing.set(true)">
            <mat-icon>edit</mat-icon> Edit Profile
          </button>
        } @else {
          <mat-card>
            <mat-card-content>
              <form [formGroup]="form" (ngSubmit)="save()" style="display:flex;flex-direction:column;gap:16px;padding:16px 0;">
                <mat-form-field appearance="outline">
                  <mat-label>Name</mat-label>
                  <input matInput formControlName="name" />
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>Profile Image URL</mat-label>
                  <input matInput formControlName="profileImageUrl" placeholder="https://..." />
                  <mat-hint>Paste a direct image URL</mat-hint>
                </mat-form-field>
                <div style="display:flex;gap:12px;">
                  <button mat-stroked-button type="button" (click)="isEditing.set(false)">Cancel</button>
                  <button mat-flat-button color="primary" type="submit" [disabled]="isSaving()">
                    {{ isSaving() ? 'Saving…' : 'Save Changes' }}
                  </button>
                </div>
              </form>
            </mat-card-content>
          </mat-card>
        }
      </div>
    </div>
  `,
  styles: [`
    .profile-avatar {
      width: 72px; height: 72px; border-radius: 50%;
      background: var(--color-primary); color: white;
      display: flex; align-items: center; justify-content: center;
      font-size: 2rem; font-weight: 700; flex-shrink: 0; overflow: hidden;
    }
  `]
})
export class SuperAdminProfileComponent implements OnInit {
  private userService = inject(UserService);
  private toast = inject(ToastService);
  private auth = inject(AuthService);
  private fb = inject(FormBuilder);

  profile = signal<UserProfileResponseDto | null>(null);
  isEditing = signal(false);
  isSaving = signal(false);

  form = this.fb.group({
    name: [''],
    profileImageUrl: [''],
  });

  ngOnInit() {
    this.userService.getProfile().subscribe(p => {
      this.profile.set(p);
      this.form.patchValue({ name: p.name, profileImageUrl: p.profileImageUrl ?? '' });
      this.auth.updateProfileImage(p.profileImageUrl ?? null);
    });
  }

  save() {
    this.isSaving.set(true);
    this.userService.updateProfile(this.form.value as any).subscribe({
      next: updated => {
        this.profile.set(updated);
        this.isEditing.set(false);
        this.isSaving.set(false);
        this.toast.success('Profile updated.');
        if (updated.name) this.auth.updateUserName(updated.name);
        this.auth.updateProfileImage(updated.profileImageUrl ?? null);
      },
      error: () => this.isSaving.set(false),
    });
  }
}
