import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SessionExpiredDialogComponent, ToastsComponent } from './shared/components';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ToastsComponent, SessionExpiredDialogComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('Front');
}
