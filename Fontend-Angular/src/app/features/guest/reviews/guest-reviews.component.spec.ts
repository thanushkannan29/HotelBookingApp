import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GuestReviewsComponent } from './guest-reviews.component';

describe('GuestReviewsComponent', () => {
  let component: GuestReviewsComponent;
  let fixture: ComponentFixture<GuestReviewsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GuestReviewsComponent],
    }).compileComponents();

    fixture   = TestBed.createComponent(GuestReviewsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
