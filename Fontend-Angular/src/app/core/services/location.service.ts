import { Injectable } from '@angular/core';
import { City, State, ICity, IState } from 'country-state-city';

@Injectable({ providedIn: 'root' })
export class LocationService {
  private readonly COUNTRY_CODE = 'IN';

  getStates(): IState[] {
    return State.getStatesOfCountry(this.COUNTRY_CODE);
  }

  getCitiesOfState(stateCode: string): ICity[] {
    return City.getCitiesOfState(this.COUNTRY_CODE, stateCode);
  }

  getStateByCode(stateCode: string): IState | undefined {
    return State.getStateByCodeAndCountry(stateCode, this.COUNTRY_CODE);
  }

  searchCities(query: string): ICity[] {
    if (!query || query.length < 2) return [];
    const all = City.getCitiesOfCountry(this.COUNTRY_CODE) || [];
    return all
      .filter(c => c.name.toLowerCase().startsWith(query.toLowerCase()))
      .slice(0, 20);
  }

  getStateNameByCity(cityName: string): string {
    const all = City.getCitiesOfCountry(this.COUNTRY_CODE) || [];
    const match = all.find(c => c.name.toLowerCase() === cityName.toLowerCase());
    if (!match) return '';
    const state = State.getStateByCodeAndCountry(match.stateCode, this.COUNTRY_CODE);
    return state?.name || '';
  }
}
