import { Injectable } from '@angular/core';

export type SessionRole = 'profesor' | 'alumno';

export interface UserSession {
    rol: SessionRole;
    id: number;
    nombre: string;
    correo: string;
    cursoId?: number;
    curso?: string;
}

@Injectable({ providedIn: 'root' })
export class SessionService {
    private readonly storageKey = 'school_session';

    getSession(): UserSession | null {
        const raw = localStorage.getItem(this.storageKey);
        if (!raw) {
            return null;
        }

        try {
            return JSON.parse(raw) as UserSession;
        } catch {
            this.clearSession();
            return null;
        }
    }

    setSession(session: UserSession): void {
        localStorage.setItem(this.storageKey, JSON.stringify(session));
    }

    clearSession(): void {
        localStorage.removeItem(this.storageKey);
    }
}
