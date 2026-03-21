import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class LoadingService {
  private _count = 0;
  private _loading = signal(false);
  readonly isLoading = this._loading.asReadonly();

  show(): void {
    this._count++;
    this._loading.set(true);
  }

  hide(): void {
    this._count = Math.max(0, this._count - 1);
    if (this._count === 0) {
      this._loading.set(false);
    }
  }
}
