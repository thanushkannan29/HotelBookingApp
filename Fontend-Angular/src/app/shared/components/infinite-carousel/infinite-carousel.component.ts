import {
  Component, Input, OnChanges, OnDestroy, ElementRef, ViewChild, AfterViewInit, SimpleChanges
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { HotelListItemDto } from '../../../core/models/models';
import { HotelCardComponent } from '../../../features/hotel/hotel-card/hotel-card.component';

@Component({
  selector: 'app-infinite-carousel',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, HotelCardComponent],
  template: `
    <div class="carousel-wrapper">
      <button class="carousel-btn" mat-icon-button (click)="scrollLeft()" aria-label="Scroll left">
        <mat-icon>chevron_left</mat-icon>
      </button>

      <div class="carousel-track" #track>
        @for (h of displayItems; track $index) {
          <div class="carousel-item">
            <app-hotel-card [hotel]="h" />
          </div>
        }
      </div>

      <button class="carousel-btn" mat-icon-button (click)="scrollRight()" aria-label="Scroll right">
        <mat-icon>chevron_right</mat-icon>
      </button>
    </div>
  `,
  styles: [`
    .carousel-wrapper {
      position: relative;
      display: flex;
      align-items: center;
      gap: 4px;
    }
    .carousel-track {
      display: flex;
      gap: 16px;
      overflow-x: auto;
      scroll-behavior: smooth;
      scrollbar-width: none;
      -ms-overflow-style: none;
      flex: 1;
      min-width: 0;
      padding: 4px 0 12px;
      &::-webkit-scrollbar { display: none; }
    }
    .carousel-item { flex: 0 0 280px; min-width: 280px; min-height: 380px; display: flex; flex-direction: column; }
    .carousel-btn {
      flex-shrink: 0;
      background: var(--color-surface) !important;
      border: 1px solid var(--color-border) !important;
      box-shadow: 0 2px 8px rgba(0,0,0,0.12) !important;
      transition: all 0.2s;
      &:hover {
        background: var(--color-primary) !important;
        color: white !important;
        border-color: var(--color-primary) !important;
      }
    }
  `]
})
export class InfiniteCarouselComponent implements OnChanges, AfterViewInit, OnDestroy {
  @Input({ required: true }) hotels: HotelListItemDto[] = [];
  @ViewChild('track') trackRef!: ElementRef<HTMLDivElement>;

  displayItems: HotelListItemDto[] = [];
  private readonly CARD_WIDTH = 296; // 280px + 16px gap
  private autoTimer: any;
  private viewInitialized = false;

  ngOnChanges(changes: SimpleChanges) {
    if (changes['hotels'] && this.hotels.length > 0) {
      // Triple the list so we always have cards on both sides to scroll into
      this.displayItems = [...this.hotels, ...this.hotels, ...this.hotels];
      if (this.viewInitialized) {
        // Wait for DOM to render the new items before jumping
        setTimeout(() => this.jumpToMiddle(), 50);
      }
    }
  }

  ngAfterViewInit() {
    this.viewInitialized = true;
    if (this.hotels.length > 0) {
      setTimeout(() => this.jumpToMiddle(), 100);
    }
    this.autoTimer = setInterval(() => this.autoScroll(), 3500);
  }

  ngOnDestroy() {
    if (this.autoTimer) clearInterval(this.autoTimer);
  }

  private jumpToMiddle() {
    const el = this.trackRef?.nativeElement;
    if (!el || this.hotels.length === 0) return;
    el.style.scrollBehavior = 'auto';
    // Start at the beginning of the middle copy
    el.scrollLeft = this.hotels.length * this.CARD_WIDTH;
    // Re-enable smooth after the jump settles
    requestAnimationFrame(() => { el.style.scrollBehavior = 'smooth'; });
  }

  private autoScroll() {
    const el = this.trackRef?.nativeElement;
    if (!el || this.hotels.length === 0) return;
    el.scrollLeft += this.CARD_WIDTH;
    requestAnimationFrame(() => this.wrapIfNeeded(el));
  }

  scrollLeft() {
    const el = this.trackRef?.nativeElement;
    if (!el) return;
    el.scrollLeft -= this.CARD_WIDTH * 2;
    requestAnimationFrame(() => this.wrapIfNeeded(el));
  }

  scrollRight() {
    const el = this.trackRef?.nativeElement;
    if (!el) return;
    el.scrollLeft += this.CARD_WIDTH * 2;
    requestAnimationFrame(() => this.wrapIfNeeded(el));
  }

  private wrapIfNeeded(el: HTMLDivElement) {
    const single = this.hotels.length * this.CARD_WIDTH;
    if (single === 0) return;
    const max = single * 2; // end of middle copy
    const min = single;     // start of middle copy

    if (el.scrollLeft >= max) {
      // Scrolled into the last copy — jump back to middle copy silently
      el.style.scrollBehavior = 'auto';
      el.scrollLeft -= single;
      requestAnimationFrame(() => { el.style.scrollBehavior = 'smooth'; });
    } else if (el.scrollLeft < min - this.CARD_WIDTH * 2) {
      // Scrolled into the first copy — jump forward to middle copy silently
      el.style.scrollBehavior = 'auto';
      el.scrollLeft += single;
      requestAnimationFrame(() => { el.style.scrollBehavior = 'smooth'; });
    }
  }
}
