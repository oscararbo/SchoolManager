import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { SessionService, UserSession } from '../../core/services/session.service';
import { AuthStateService } from '../../core/services/auth-state.service';
import { ProfesorHomeComponent } from '../../features/home/profesor-home/profesor-home';
import { AlumnoHomeComponent } from '../../features/home/alumno-home/alumno-home';
import { AdminHomeComponent } from '../../features/home/admin-home/admin-home';
import { SuperusuarioHomeComponent } from '../../features/home/superusuario-home/superusuario-home.component';
import { LogoutButtonComponent } from '../../shared/components/logout-button/logout-button.component';
import { SchoolApiService } from '../../shared/services/school-api.service';
import { SessionExpiredDialogComponent } from '../../shared/components/session-expired-dialog/session-expired-dialog.component';

@Component({
  selector: 'app-home-layout',
  standalone: true,
  imports: [CommonModule, ProfesorHomeComponent, AlumnoHomeComponent, AdminHomeComponent, SuperusuarioHomeComponent, LogoutButtonComponent, SessionExpiredDialogComponent],
  templateUrl: './home-layout.html',
  styleUrl: './home-layout.scss',
})
export class HomeLayout implements OnInit {
  session = signal<UserSession | null>(null);

  private router = inject(Router);
  private sessionService = inject(SessionService);
  private schoolApiService = inject(SchoolApiService);
  protected authState = inject(AuthStateService);

  panelTitulo = computed(() => {
    const currentSession = this.session();
    if (!currentSession) {
      return 'Panel';
    }

    if (currentSession.rol === 'admin') {
      return 'Panel de administracion';
    }

    if (currentSession.rol === 'superusuario') {
      return 'Panel de superusuario';
    }

    if (currentSession.rol === 'profesor') {
      return 'Panel de profesor';
    }

    return 'Panel de alumno';
  });

  /**
   * Recupera la sesion activa y la asigna al Signal de vista.
   * Redirige al login si no hay sesion.
   */
  ngOnInit(): void {
    const currentSession = this.sessionService.getSession();

    if (!currentSession) {
      this.router.navigate(['']);
      return;
    }

    this.session.set(currentSession);
  }

  /**
   * Invalida el refreshToken en el servidor, limpia la sesion local
   * y redirige al login.
   */
  async cerrarSesion(): Promise<void> {
    try {
      await this.schoolApiService.logout();
    } catch (e) {
      console.error('Error al cerrar sesion:', e);
    } finally {
      this.router.navigate(['']);
    }
  }
}

