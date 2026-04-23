import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SessionExpiredDialogComponent } from './shared/components/session-expired-dialog/session-expired-dialog.component';
import { ToastsComponent } from './shared/components/toasts/toasts.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ToastsComponent, SessionExpiredDialogComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('Front');
}
