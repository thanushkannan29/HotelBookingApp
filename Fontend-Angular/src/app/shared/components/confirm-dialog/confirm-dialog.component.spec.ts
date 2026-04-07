import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { ConfirmDialogComponent, ConfirmDialogData } from './confirm-dialog.component';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

function makeData(overrides: Partial<ConfirmDialogData> = {}): ConfirmDialogData {
  return { title: 'Delete Item', message: 'Are you sure?', ...overrides };
}

describe('ConfirmDialogComponent', () => {
  let component: ConfirmDialogComponent;
  let fixture: ComponentFixture<ConfirmDialogComponent>;
  let dialogRefSpy: jasmine.SpyObj<MatDialogRef<ConfirmDialogComponent>>;

  async function setup(data: ConfirmDialogData = makeData()) {
    dialogRefSpy = jasmine.createSpyObj('MatDialogRef', ['close']);

    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [ConfirmDialogComponent],
      providers: [
        provideAnimationsAsync(),
        { provide: MatDialogRef, useValue: dialogRefSpy },
        { provide: MAT_DIALOG_DATA, useValue: data },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ConfirmDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  beforeEach(async () => await setup());

  it('should create', () => expect(component).toBeTruthy());

  it('data — should expose injected data', () => {
    expect(component.data.title).toBe('Delete Item');
    expect(component.data.message).toBe('Are you sure?');
  });

  it('data — should use default confirmLabel "Confirm" when not provided', () => {
    expect(component.data.confirmLabel).toBeUndefined();
  });

  it('data — should use provided confirmLabel', async () => {
    await setup(makeData({ confirmLabel: 'Delete', confirmColor: 'warn' }));
    expect(component.data.confirmLabel).toBe('Delete');
    expect(component.data.confirmColor).toBe('warn');
  });

  it('data — should expose icon when provided', async () => {
    await setup(makeData({ icon: 'delete_forever' }));
    expect(component.data.icon).toBe('delete_forever');
  });

  it('dialogRef — should be injected', () => {
    expect(component.dialogRef).toBeTruthy();
  });
});
