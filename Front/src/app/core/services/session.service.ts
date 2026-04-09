import { Injectable } from '@angular/core';

export type SessionRole = 'profesor' | 'alumno' | 'admin';

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
    private _accessToken: string | null = null;

    /**
     * Lee y deserializa la sesion de usuario desde `localStorage`.
     *
     * @returns La sesion activa, o `null` si no existe o esta corrupta.
     */
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

    /**
     * Serializa y persiste la sesion en `localStorage`.
     *
     * @param session - Datos de sesion a guardar.
     */
    setSession(session: UserSession): void {
        localStorage.setItem(this.storageKey, JSON.stringify(session));
    }

    /** Elimina la sesion del almacenamiento local. */
    clearSession(): void {
        localStorage.removeItem(this.storageKey);
        this._accessToken = null;
    }

    /** Returns the in-memory access token (lost on page reload, restored via silent refresh). */
    getToken(): string | null {
        return this._accessToken;
    }

    /** Stores the access token in memory only — never persisted to localStorage. */
    setToken(token: string): void {
        this._accessToken = token;
    }

    /** Clears the in-memory access token without affecting the session data. */
    clearToken(): void {
        this._accessToken = null;
    }
}
