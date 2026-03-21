import { Component, inject } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { LoadingService } from '../../../core/services/loading.service';

@Component({
  selector: 'app-spinner',
  standalone: true,
  imports: [MatProgressSpinnerModule],
  template: `
    @if (loading.isLoading()) {
      <div class="full-page-spinner">
        <mat-progress-spinner mode="indeterminate" diameter="40" />
        <span class="spinner-text">Loading...</span>
      </div>
    }
  `
})
export class SpinnerComponent {
  loading = inject(LoadingService);
}
