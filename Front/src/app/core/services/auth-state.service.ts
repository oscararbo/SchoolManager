import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { SessionService } from './session.service';

@Injectable({ providedIn: 'root' })
export class AuthStateService {
    private readonly _sessionExpired = signal(false);
    readonly sessionExpired = this._sessionExpired.asReadonly();

    private refreshPromise: Promise<boolean> | null = null;

    private sessionService = inject(SessionService);
    private router = inject(Router);

    tryRefresh(): Promise<boolean> {
        if (!this.refreshPromise) {
            this.refreshPromise = this._doRefresh().finally(() => {
                this.refreshPromise = null;
            });
        }
        return this.refreshPromise;
    }

    markExpired(): void {
        this.sessionService.clearSession();
        this._sessionExpired.set(true);
    }

    acceptExpired(): void {
        this._sessionExpired.set(false);
        this.router.navigate(['']);
    }

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
