import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReservationManagementComponent } from './reservation-management.component';

describe('ReservationManagementComponent', () => {
  let component: ReservationManagementComponent;
  let fixture: ComponentFixture<ReservationManagementComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReservationManagementComponent],
    }).compileComponents();

    fixture   = TestBed.createComponent(ReservationManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
