import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Authencation } from './authencation';

describe('Authencation', () => {
  let component: Authencation;
  let fixture: ComponentFixture<Authencation>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Authencation],
    }).compileComponents();

    fixture = TestBed.createComponent(Authencation);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
