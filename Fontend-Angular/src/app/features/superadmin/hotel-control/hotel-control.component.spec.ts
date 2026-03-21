import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HotelControlComponent } from './hotel-control.component';

describe('HotelControlComponent', () => {
  let component: HotelControlComponent;
  let fixture: ComponentFixture<HotelControlComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HotelControlComponent],
    }).compileComponents();

    fixture   = TestBed.createComponent(HotelControlComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
