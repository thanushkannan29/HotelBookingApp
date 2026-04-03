import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { InputDialogComponent, InputDialogData } from './input-dialog.component';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

function makeData(overrides: Partial<InputDialogData> = {}): InputDialogData {
  return { title: 'Enter Reason', label: 'Reason', ...overrides };
}

describe('InputDialogComponent', () => {
  let component: InputDialogComponent;
  let fixture: ComponentFixture<InputDialogComponent>;
  let dialogRefSpy: jasmine.SpyObj<MatDialogRef<InputDialogComponent>>;

  async function setup(data: InputDialogData = makeData()) {
    dialogRefSpy = jasmine.createSpyObj('MatDialogRef', ['close']);

    await TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [InputDialogComponent],
      providers: [
        provideAnimationsAsync(),
        { provide: MatDialogRef, useValue: dialogRefSpy },
        { provide: MAT_DIALOG_DATA, useValue: data },
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(InputDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  beforeEach(async () => await setup());

  it('should create', () => expect(component).toBeTruthy());

  it('value — should start as empty string', () => {
    expect(component.value).toBe('');
  });

  it('data — should expose injected title and label', () => {
    expect(component.data.title).toBe('Enter Reason');
    expect(component.data.label).toBe('Reason');
  });

  it('data — should expose multiline flag', async () => {
    await setup(makeData({ multiline: true }));
    expect(component.data.multiline).toBeTrue();
  });

  it('data — should expose confirmLabel', async () => {
    await setup(makeData({ confirmLabel: 'Submit', confirmColor: 'warn' }));
    expect(component.data.confirmLabel).toBe('Submit');
    expect(component.data.confirmColor).toBe('warn');
  });

  it('value — can be set', () => {
    component.value = 'test reason';
    expect(component.value).toBe('test reason');
  });

  it('dialogRef — should be injected', () => {
    expect(component.dialogRef).toBeTruthy();
  });
});
