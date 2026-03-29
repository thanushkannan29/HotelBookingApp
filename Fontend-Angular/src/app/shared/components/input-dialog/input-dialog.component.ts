import { Component, Inject } from '@angular/core';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { FormsModule } from '@angular/forms';

export interface InputDialogData {
  title: string;
  label: string;
  placeholder?: string;
  confirmLabel?: string;
  confirmColor?: 'primary' | 'warn' | 'accent';
  multiline?: boolean;
}

@Component({
  selector: 'app-input-dialog',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule, FormsModule],
  template: `
    <div style="min-width:360px;max-width:480px;">
      <h2 mat-dialog-title>{{ data.title }}</h2>
      <mat-dialog-content>
        <mat-form-field appearance="outline" style="width:100%;margin-top:8px;">
          <mat-label>{{ data.label }}</mat-label>
          @if (data.multiline) {
            <textarea matInput [(ngModel)]="value" [placeholder]="data.placeholder || ''" rows="3"></textarea>
          } @else {
            <input matInput [(ngModel)]="value" [placeholder]="data.placeholder || ''" />
          }
        </mat-form-field>
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-stroked-button [mat-dialog-close]="null">Cancel</button>
        <button mat-flat-button [color]="data.confirmColor || 'primary'"
                [disabled]="!value.trim()"
                [mat-dialog-close]="value">
          {{ data.confirmLabel || 'Submit' }}
        </button>
      </mat-dialog-actions>
    </div>
  `
})
export class InputDialogComponent {
  value = '';
  constructor(
    public dialogRef: MatDialogRef<InputDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: InputDialogData
  ) {}
}
