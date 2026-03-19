import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { SessionService } from './session.service';

@Injectable({ providedIn: 'root' })
export class AuthStateService {
    private readonly _sessionExpired = signal(false);
    private readonly _expiredMessage = signal('Necesitas iniciar sesion para acceder a este recurso.');
    readonly sessionExpired = this._sessionExpired.asReadonly();
    readonly expiredMessage = this._expiredMessage.asReadonly();

    private refreshPromise: Promise<boolean> | null = null;

    private sessionService = inject(SessionService);
    private router = inject(Router);

    /**
     * Inicia el flujo de refresco de token si no hay ya uno en curso.
     * Las peticiones concurrentes comparten la misma promesa para evitar llamadas duplicadas.
     *
     * @returns `true` si el token se refresco correctamente, `false` en caso contrario.
     */
    tryRefresh(): Promise<boolean> {
        if (!this.refreshPromise) {
            this.refreshPromise = this._doRefresh().finally(() => {
                this.refreshPromise = null;
            });
        }
        return this.refreshPromise;
    }

    /**
     * Marca la sesion como expirada, limpia el almacenamiento local
     * y activa el dialogo de sesion expirada con un mensaje contextual.
     *
     * @param message - Mensaje opcional del backend para mostrar al usuario.
     */
    markExpired(message?: string): void {
        this.sessionService.clearSession();
        this._expiredMessage.set(message?.trim() || 'Necesitas iniciar sesion para acceder a este recurso.');
        this._sessionExpired.set(true);
    }

    /**
     * Cierra el dialogo de sesion expirada, restaura el mensaje por defecto
     * y redirige al login.
     */
    acceptExpired(): void {
        this._sessionExpired.set(false);
        this._expiredMessage.set('Necesitas iniciar sesion para acceder a este recurso.');
        this.router.navigate(['']);
    }

    /**
     * Realiza la llamada al endpoint de refresco y actualiza el token en sesion si tiene exito.
     *
     * @returns `true` si el token se refresco, `false` si el refreshToken falta o el endpoint falla.
     */
    private async _doRefresh(): Promise<boolean> {
        const session = this.sessionService.getSession();
        if (!session?.refreshToken) {
            this.sessionService.clearSession();
            return false;
        }

        try {
            const resp = await fetch('http://localhost:5014/api/auth/refresh', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ refreshToken: session.refreshToken })
            });

            if (!resp.ok) {
                this.sessionService.clearSession();
                return false;
            }

            const data = await resp.json() as { token: string; refreshToken: string };
            this.sessionService.setSession({ ...session, token: data.token, refreshToken: data.refreshToken });
            return true;
        } catch {
            this.sessionService.clearSession();
            return false;
        }
    }
}
