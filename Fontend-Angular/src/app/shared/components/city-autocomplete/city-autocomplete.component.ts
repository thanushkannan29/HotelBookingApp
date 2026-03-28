import { Component, Input, inject, OnInit, OnDestroy } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';
import { LocationService } from '../../../core/services/location.service';
import { ICity } from 'country-state-city';

@Component({
  selector: 'app-city-autocomplete',
  standalone: true,
  imports: [ReactiveFormsModule, MatAutocompleteModule, MatFormFieldModule, MatInputModule],
  template: `
    <mat-form-field appearance="outline" style="width:100%">
      <mat-label>📍 City</mat-label>
      <input matInput [formControl]="control" [matAutocomplete]="cityAuto" placeholder="Search city..." />
      <mat-autocomplete #cityAuto="matAutocomplete" [displayWith]="displayFn"
                        (optionSelected)="onOptionSelected($event.option.value)">
        @for (city of filteredCities; track city.name) {
          <mat-option [value]="city">{{ city.name }} — {{ city.stateCode }}</mat-option>
        }
      </mat-autocomplete>
    </mat-form-field>
  `,
})
export class CityAutocompleteComponent implements OnInit, OnDestroy {
  @Input() control!: FormControl;
  /** Optional: if provided, auto-fills with the state name when a city is selected */
  @Input() stateControl?: FormControl;

  private locationService = inject(LocationService);
  private destroy$ = new Subject<void>();

  filteredCities: ICity[] = [];

  ngOnInit() {
    this.control.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe((value: string | ICity) => {
      const query = typeof value === 'string' ? value : value?.name ?? '';
      this.filteredCities = this.locationService.searchCities(query);
    });
  }

  displayFn(city: ICity | string): string {
    if (!city) return '';
    return typeof city === 'string' ? city : city.name;
  }

  onOptionSelected(city: ICity) {
    this.control.setValue(city.name, { emitEvent: false });
    this.filteredCities = [];
    // Auto-fill state if stateControl provided
    if (this.stateControl) {
      const stateName = this.locationService.getStateNameByCity(city.name);
      if (stateName) this.stateControl.setValue(stateName, { emitEvent: false });
    }
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
