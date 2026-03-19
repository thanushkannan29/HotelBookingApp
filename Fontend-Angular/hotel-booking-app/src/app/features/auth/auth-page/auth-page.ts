import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-auth-page',
  standalone: true, // ✅ IMPORTANT
  imports: [CommonModule, FormsModule], // ✅ FIX ALL ERRORS
  templateUrl: './auth-page.html'
})
export class AuthPageComponent {

  mode: 'login' | 'guest' | 'admin' = 'login';

  form: any = {};

  constructor(private auth: AuthService) {}

  submit() {

    if (this.mode === 'login') {
      this.auth.login(this.form).subscribe();
    }

    if (this.mode === 'guest') {
      this.auth.registerGuest(this.form).subscribe();
    }

    if (this.mode === 'admin') {
      this.auth.registerAdmin(this.form).subscribe();
    }
  }
}
