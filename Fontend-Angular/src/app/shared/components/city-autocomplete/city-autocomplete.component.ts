import { Component, Input, inject, OnInit, OnDestroy } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { AsyncPipe } from '@angular/common';
import { Subject, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, takeUntil } from 'rxjs/operators';
import { CityService } from '../../../core/services/city.service';
import { CityDto } from '../../../core/models/models';

@Component({
  selector: 'app-city-autocomplete',
  standalone: true,
  imports: [
    ReactiveFormsModule, AsyncPipe,
    MatAutocompleteModule, MatFormFieldModule, MatInputModule,
  ],
  template: `
    <mat-form-field appearance="outline" style="width:100%">
      <mat-label>📍 City</mat-label>
      <input
        matInput
        [formControl]="control"
        [matAutocomplete]="cityAuto"
        placeholder="Search city..."
      />
      <mat-autocomplete
        #cityAuto="matAutocomplete"
        [displayWith]="displayFn"
        (optionSelected)="onOptionSelected($event.option.value)"
      >
        @for (city of filteredCities; track city.cityId) {
          <mat-option [value]="city">
            {{ city.cityName }} — {{ city.stateName }}
          </mat-option>
        }
      </mat-autocomplete>
    </mat-form-field>
  `,
})
export class CityAutocompleteComponent implements OnInit, OnDestroy {
  @Input() control!: FormControl;

  private cityService = inject(CityService);
  private destroy$ = new Subject<void>();

  filteredCities: CityDto[] = [];

  ngOnInit() {
    this.control.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap((value: string | CityDto) => {
        const query = typeof value === 'string' ? value : value?.cityName ?? '';
        if (!query || query.length < 2) return of([]);
        return this.cityService.search(query);
      }),
      takeUntil(this.destroy$)
    ).subscribe(cities => {
      this.filteredCities = cities;
    });
  }

  displayFn(city: CityDto | string): string {
    if (!city) return '';
    return typeof city === 'string' ? city : city.cityName;
  }

  onOptionSelected(city: CityDto) {
    this.control.setValue(city.cityName, { emitEvent: false });
    this.filteredCities = [];
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
