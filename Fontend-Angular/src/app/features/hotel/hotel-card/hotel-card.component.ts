import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { DecimalPipe } from '@angular/common';
import { HotelListItemDto } from '../../../core/models/models';

@Component({
  selector: 'app-hotel-card',
  standalone: true,
  imports: [RouterLink, MatIconModule, MatButtonModule, DecimalPipe],
  templateUrl: './hotel-card.component.html',
  styleUrl: './hotel-card.component.scss'
})
export class HotelCardComponent {
  @Input({ required: true }) hotel!: HotelListItemDto;

  get stars(): number[] {
    return Array.from({ length: 5 }, (_, i) => i + 1);
  }

  get ratingClass(): string {
    const r = this.hotel.averageRating;
    if (r >= 4.5) return 'excellent';
    if (r >= 4.0) return 'great';
    if (r >= 3.0) return 'good';
    return 'fair';
  }

  get ratingLabel(): string {
    const r = this.hotel.averageRating;
    if (r >= 4.5) return 'Excellent';
    if (r >= 4.0) return 'Great';
    if (r >= 3.0) return 'Good';
    return 'Fair';
  }

  get imagePlaceholder(): string {
    const colors = ['#2d3a8c', '#1a4d5c', '#3d2b1f', '#1a3a2d', '#3d1a3a'];
    const idx = this.hotel.name.charCodeAt(0) % colors.length;
    return colors[idx];
  }
}
