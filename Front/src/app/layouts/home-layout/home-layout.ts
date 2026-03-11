import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { SessionService, UserSession } from '../../core/services/session.service';
import { ProfesorHomeComponent } from '../../features/home/profesor-home/profesor-home';
import { AlumnoHomeComponent } from '../../features/home/alumno-home/alumno-home';
import { LogoutButton } from '../../shared/components/logout-button/logout-button';

@Component({
  selector: 'app-home-layout',
  standalone: true,
  imports: [CommonModule, ProfesorHomeComponent, AlumnoHomeComponent, LogoutButton],
  templateUrl: './home-layout.html',
  styleUrl: './home-layout.scss',
})
export class HomeLayout implements OnInit {
  session = signal<UserSession | null>(null);

  private router = inject(Router);
  private sessionService = inject(SessionService);

  ngOnInit(): void {
    const currentSession = this.sessionService.getSession();

    if (!currentSession) {
      this.router.navigate(['']);
      return;
    }

    this.session.set(currentSession);
  }

  cerrarSesion(): void {
    this.sessionService.clearSession();
    this.router.navigate(['']);
  }
}
