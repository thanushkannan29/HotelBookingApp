import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Authencation } from './authencation/authencation';

@Component({
  selector: 'app-root',
  imports: [Authencation],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('HotelBookingAppFrontend');
}
