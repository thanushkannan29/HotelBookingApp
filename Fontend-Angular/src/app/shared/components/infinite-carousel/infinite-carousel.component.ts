import {
  Component, Input, OnInit, OnDestroy, ElementRef, ViewChild,
  AfterViewInit, ChangeDetectionStrategy, NgZone
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
    <div class="carousel-wrapper">
      <button class="carousel-btn prev" mat-icon-button (click)="scrollLeft()" aria-label="Scroll left">
        <mat-icon>chevron_left</mat-icon>
      </button>

      <div class="carousel-track" #track>
        <!-- Duplicate items for seamless loop -->
        @for (h of displayItems; track h.hotelId + '_' + $index) {
          <div class="carousel-item">
            <app-hotel-card [hotel]="h" />
          </div>
        }
      </div>

      <button class="carousel-btn next" mat-icon-button (click)="scrollRight()" aria-label="Scroll right">
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
      padding: 4px 0 12px;

      &::-webkit-scrollbar { display: none; }
    }

    .carousel-item {
      flex: 0 0 280px;
    }

    .carousel-btn {
      flex-shrink: 0;
      background: var(--color-surface) !important;
      border: 1px solid var(--color-border) !important;
      box-shadow: 0 2px 8px rgba(0,0,0,0.12) !important;
      z-index: 2;
      transition: all 0.2s;

      &:hover {
        background: var(--color-primary) !important;
        color: white !important;
        border-color: var(--color-primary) !important;
      }
    }
  `]
})
export class InfiniteCarouselComponent implements OnInit, AfterViewInit, OnDestroy {
  @Input({ required: true }) hotels: HotelListItemDto[] = [];
  @ViewChild('track') trackRef!: ElementRef<HTMLDivElement>;

  displayItems: HotelListItemDto[] = [];
  private readonly CARD_WIDTH = 296; // 280px + 16px gap
  private autoTimer: any;

  ngOnInit() {
    // Triple the items so we always have content on both sides
    if (this.hotels.length > 0) {
      this.displayItems = [...this.hotels, ...this.hotels, ...this.hotels];
    }
  }

  ngAfterViewInit() {
    // Start at the middle copy so we can scroll both ways
    setTimeout(() => this.jumpToMiddle(), 50);
    // Auto-scroll every 3s
    this.autoTimer = setInterval(() => this.autoScroll(), 3000);
  }

  ngOnDestroy() {
    if (this.autoTimer) clearInterval(this.autoTimer);
  }

  private jumpToMiddle() {
    const el = this.trackRef?.nativeElement;
    if (!el || this.hotels.length === 0) return;
    el.scrollLeft = this.hotels.length * this.CARD_WIDTH;
  }

  private autoScroll() {
    const el = this.trackRef?.nativeElement;
    if (!el) return;
    el.scrollLeft += this.CARD_WIDTH;
    this.checkLoop(el);
  }

  scrollLeft() {
    const el = this.trackRef?.nativeElement;
    if (!el) return;
    el.scrollLeft -= this.CARD_WIDTH * 2;
    this.checkLoop(el);
  }

  scrollRight() {
    const el = this.trackRef?.nativeElement;
    if (!el) return;
    el.scrollLeft += this.CARD_WIDTH * 2;
    this.checkLoop(el);
  }

  private checkLoop(el: HTMLDivElement) {
    const singleWidth = this.hotels.length * this.CARD_WIDTH;
    // If we've scrolled past the last copy, jump back to middle
    if (el.scrollLeft >= singleWidth * 2) {
      el.style.scrollBehavior = 'auto';
      el.scrollLeft -= singleWidth;
      el.style.scrollBehavior = 'smooth';
    }
    // If we've scrolled before the first copy, jump forward to middle
    if (el.scrollLeft <= 0) {
      el.style.scrollBehavior = 'auto';
      el.scrollLeft += singleWidth;
      el.style.scrollBehavior = 'smooth';
    }
  }
}
