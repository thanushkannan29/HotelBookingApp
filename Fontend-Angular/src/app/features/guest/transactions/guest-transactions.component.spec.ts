import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GuestTransactionsComponent } from './guest-transactions.component';

describe('GuestTransactionsComponent', () => {
  let component: GuestTransactionsComponent;
  let fixture: ComponentFixture<GuestTransactionsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GuestTransactionsComponent],
    }).compileComponents();

    fixture   = TestBed.createComponent(GuestTransactionsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
