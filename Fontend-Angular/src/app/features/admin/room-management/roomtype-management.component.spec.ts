import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RoomTypeManagementComponent } from './roomtype-management.component';

describe('RoomTypeManagementComponent', () => {
  let component: RoomTypeManagementComponent;
  let fixture: ComponentFixture<RoomTypeManagementComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RoomTypeManagementComponent],
    }).compileComponents();

    fixture   = TestBed.createComponent(RoomTypeManagementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
