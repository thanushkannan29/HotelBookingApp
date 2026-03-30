import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';
import { SupportRequestService } from '../../core/services/support-request.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [
    RouterLink, MatIconModule, MatButtonModule,
    MatFormFieldModule, MatInputModule, MatSelectModule,
    MatProgressSpinnerModule, ReactiveFormsModule,
  ],
  templateUrl: './contact.component.html',
  styleUrl: './contact.component.scss'
})
export class ContactComponent {
  auth = inject(AuthService);
  private supportService = inject(SupportRequestService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);

  submitting = signal(false);
  submitted = signal(false);

  readonly publicCategories = ['General Enquiry', 'Booking Help', 'Payment Issue', 'Other'];
  readonly guestCategories = ['Complaint', 'Billing', 'Refund', 'Room Issue', 'Service Issue', 'Other'];
  readonly adminCategories = ['Bug Report', 'Dashboard Issue', 'Payment Issue', 'Feature Request', 'Other'];

  publicForm = this.fb.group({
    name:     ['', [Validators.required, Validators.maxLength(150)]],
    email:    ['', [Validators.required, Validators.email]],
    subject:  ['', [Validators.required, Validators.maxLength(100)]],
    category: ['General Enquiry', Validators.required],
    message:  ['', [Validators.required, Validators.maxLength(2000)]],
  });

  guestForm = this.fb.group({
    subject:         ['', [Validators.required, Validators.maxLength(100)]],
    category:        ['Complaint', Validators.required],
    reservationCode: [''],
    message:         ['', [Validators.required, Validators.maxLength(2000)]],
  });

  adminForm = this.fb.group({
    subject:  ['', [Validators.required, Validators.maxLength(100)]],
    category: ['Bug Report', Validators.required],
    message:  ['', [Validators.required, Validators.maxLength(2000)]],
  });

  submitPublic() {
    if (this.publicForm.invalid) { this.publicForm.markAllAsTouched(); return; }
    this.submitting.set(true);
    this.supportService.submitPublic(this.publicForm.value as any).subscribe({
      next: () => { this.submitted.set(true); this.submitting.set(false); this.toast.success('Request submitted!'); },
      error: () => this.submitting.set(false),
    });
  }

  submitGuest() {
    if (this.guestForm.invalid) { this.guestForm.markAllAsTouched(); return; }
    this.submitting.set(true);
    this.supportService.submitGuest(this.guestForm.value as any).subscribe({
      next: () => { this.submitted.set(true); this.submitting.set(false); this.toast.success('Support request submitted!'); },
      error: () => this.submitting.set(false),
    });
  }

  submitAdmin() {
    if (this.adminForm.invalid) { this.adminForm.markAllAsTouched(); return; }
    this.submitting.set(true);
    this.supportService.submitAdmin(this.adminForm.value as any).subscribe({
      next: () => { this.submitted.set(true); this.submitting.set(false); this.toast.success('Bug report submitted!'); },
      error: () => this.submitting.set(false),
    });
  }

  reset() { this.submitted.set(false); this.publicForm.reset(); this.guestForm.reset(); this.adminForm.reset(); }
}
