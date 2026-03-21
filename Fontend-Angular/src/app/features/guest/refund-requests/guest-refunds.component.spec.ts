import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GuestRefundsComponent } from './guest-refunds.component';

describe('GuestRefundsComponent', () => {
  let component: GuestRefundsComponent;
  let fixture: ComponentFixture<GuestRefundsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GuestRefundsComponent],
    }).compileComponents();

    fixture   = TestBed.createComponent(GuestRefundsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
