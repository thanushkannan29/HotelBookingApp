import {
  Component, Input, OnChanges, OnDestroy, ElementRef, ViewChild, AfterViewInit, SimpleChanges,
  ChangeDetectionStrategy
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { HotelListItemDto } from '../../../core/models/models';
import { HotelCardComponent } from '../../../features/hotel/hotel-card/hotel-card.component';

@Component({
  selector: 'app-infinite-carousel',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, MatIconModule, MatButtonModule, HotelCardComponent],
  template: `
    <div class="carousel-outer">
      <button class="nav-btn prev" mat-icon-button (click)="prev()" aria-label="Previous">
        <mat-icon>chevron_left</mat-icon>
      </button>

      <div class="track-viewport">
        <div class="track" #track>
          @for (h of displayItems; track $index) {
            <div class="card-slot">
              <app-hotel-card [hotel]="h" />
            </div>
          }
        </div>
      </div>

      <button class="nav-btn next" mat-icon-button (click)="next()" aria-label="Next">
        <mat-icon>chevron_right</mat-icon>
      </button>
    </div>
  `,
  styles: [`
    :host { display: block; width: 100%; }

    .carousel-outer {
      display: flex;
      align-items: center;
      gap: 8px;
      width: 100%;
    }

    .track-viewport {
      flex: 1;
      min-width: 0;
      overflow: hidden;
    }

    .track {
      display: flex;
      gap: 16px;
      transition: transform 0.4s ease;
      will-change: transform;
    }

    .card-slot {
      flex: 0 0 280px;
      width: 280px;
    }

    .card-slot app-hotel-card {
      display: block;
      width: 100%;
      height: 100%;
    }

    .nav-btn {
      flex-shrink: 0;
      background: var(--color-surface) !important;
      border: 1px solid var(--color-border) !important;
      box-shadow: 0 2px 8px rgba(0,0,0,0.12) !important;
      transition: all 0.2s;
      z-index: 1;
    }
    .nav-btn:hover {
      background: var(--color-primary) !important;
      color: white !important;
      border-color: var(--color-primary) !important;
    }
  `]
})
export class InfiniteCarouselComponent implements OnChanges, AfterViewInit, OnDestroy {
  @Input({ required: true }) hotels: HotelListItemDto[] = [];
  @ViewChild('track') trackRef!: ElementRef<HTMLDivElement>;

  displayItems: HotelListItemDto[] = [];

  private readonly CARD_WIDTH = 296; // 280px card + 16px gap
  private offset = 0;          // current translate offset in px (negative = scrolled right)
  private autoTimer: any;

  ngOnChanges(changes: SimpleChanges) {
    if (changes['hotels'] && this.hotels.length > 0) {
      this.displayItems = [...this.hotels, ...this.hotels, ...this.hotels];
      // Start at the middle copy
      this.offset = -(this.hotels.length * this.CARD_WIDTH);
      setTimeout(() => this.applyTransform(false), 0);
    }
  }

  ngAfterViewInit() {
    this.autoTimer = setInterval(() => this.autoAdvance(), 3500);
  }

  ngOnDestroy() {
    clearInterval(this.autoTimer);
  }

  prev() {
    this.offset += this.CARD_WIDTH * 2;
    this.applyTransform(true);
    setTimeout(() => this.wrapIfNeeded(), 420);
  }

  next() {
    this.offset -= this.CARD_WIDTH * 2;
    this.applyTransform(true);
    setTimeout(() => this.wrapIfNeeded(), 420);
  }

  private autoAdvance() {
    if (!this.hotels.length) return;
    this.offset -= this.CARD_WIDTH;
    this.applyTransform(true);
    setTimeout(() => this.wrapIfNeeded(), 420);
  }

  private applyTransform(animate: boolean) {
    const el = this.trackRef?.nativeElement;
    if (!el) return;
    el.style.transition = animate ? 'transform 0.4s ease' : 'none';
    el.style.transform = `translateX(${this.offset}px)`;
  }

  private wrapIfNeeded() {
    const single = this.hotels.length * this.CARD_WIDTH;
    if (single === 0) return;
    // offset is negative when scrolled right
    // middle copy range: -single*2 to -single
    if (this.offset <= -(single * 2)) {
      this.offset += single;
      this.applyTransform(false);
    } else if (this.offset >= 0) {
      this.offset -= single;
      this.applyTransform(false);
    }
  }
}
