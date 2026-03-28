import {
  Component, Input, OnInit, OnDestroy, ElementRef, ViewChild, AfterViewInit
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
      padding: 4px 0 12px;
      &::-webkit-scrollbar { display: none; }
    }
    .carousel-item { flex: 0 0 280px; }
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
export class InfiniteCarouselComponent implements OnInit, AfterViewInit, OnDestroy {
  @Input({ required: true }) hotels: HotelListItemDto[] = [];
  @ViewChild('track') trackRef!: ElementRef<HTMLDivElement>;

  displayItems: HotelListItemDto[] = [];
  private readonly CARD_WIDTH = 296; // 280 + 16 gap
  private autoTimer: any;

  ngOnInit() {
    if (this.hotels.length > 0) {
      // Triple for seamless infinite loop
      this.displayItems = [...this.hotels, ...this.hotels, ...this.hotels];
    }
  }

  ngAfterViewInit() {
    setTimeout(() => this.jumpToMiddle(), 100);
    this.autoTimer = setInterval(() => this.tick(), 3500);
  }

  ngOnDestroy() {
    if (this.autoTimer) clearInterval(this.autoTimer);
  }

  private jumpToMiddle() {
    const el = this.trackRef?.nativeElement;
    if (!el || this.hotels.length === 0) return;
    // Disable smooth scroll for the initial jump
    el.style.scrollBehavior = 'auto';
    el.scrollLeft = this.hotels.length * this.CARD_WIDTH;
    el.style.scrollBehavior = 'smooth';
  }

  private tick() {
    const el = this.trackRef?.nativeElement;
    if (!el) return;
    el.scrollLeft += this.CARD_WIDTH;
    this.loopCheck(el);
  }

  scrollLeft() {
    const el = this.trackRef?.nativeElement;
    if (!el) return;
    el.scrollLeft -= this.CARD_WIDTH * 2;
    this.loopCheck(el);
  }

  scrollRight() {
    const el = this.trackRef?.nativeElement;
    if (!el) return;
    el.scrollLeft += this.CARD_WIDTH * 2;
    this.loopCheck(el);
  }

  private loopCheck(el: HTMLDivElement) {
    const single = this.hotels.length * this.CARD_WIDTH;
    if (el.scrollLeft >= single * 2) {
      el.style.scrollBehavior = 'auto';
      el.scrollLeft -= single;
      // Re-enable smooth after a frame
      requestAnimationFrame(() => { el.style.scrollBehavior = 'smooth'; });
    }
    if (el.scrollLeft <= 0) {
      el.style.scrollBehavior = 'auto';
      el.scrollLeft += single;
      requestAnimationFrame(() => { el.style.scrollBehavior = 'smooth'; });
    }
  }
}
