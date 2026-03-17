import { Injectable } from '@angular/core';
import { SessionService } from '../../core/services/session.service';

export interface LoginResponse {
    rol: 'profesor' | 'alumno' | 'admin';
    id: number;
    nombre: string;
    correo: string;
    token: string;
    refreshToken: string;
    cursoId?: number;
    curso?: string;
}

interface RefreshResponse {
    token: string;
    refreshToken: string;
}

export interface ProfesorPanel {
    id: number;
    nombre: string;
    cursos: Array<{
        cursoId: number;
        curso: string;
        asignaturas: Array<{
            asignaturaId: number;
            nombre: string;
        }>;
    }>;
}

export interface AsignaturaAlumnos {
    asignatura: {
        id: number;
        nombre: string;
        cursoId: number;
        curso: string;
    };
    alumnos: Array<{
        estudianteId: number;
        alumno: string;
        nota: number | null;
    }>;
}

export interface AlumnoPanel {
    id: number;
    nombre: string;
    curso: {
        cursoId: number;
        curso: string;
    };
    materias: Array<{
        asignaturaId: number;
        asignatura: string;
        profesor: string | null;
        nota: number | null;
    }>;
}

@Injectable({ providedIn: 'root' })
export class SchoolApiService {
    private readonly apiUrl = 'http://localhost:5014/api';

    constructor(private readonly sessionService: SessionService) {}

    private getAuthHeaders(): HeadersInit {
        const session = this.sessionService.getSession();
        if (!session?.token) {
            throw new Error('Sesion no valida. Inicia sesion de nuevo.');
        }

        return {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${session.token}`
        };
    }

    private async refreshAccessToken(): Promise<boolean> {
        const session = this.sessionService.getSession();

        if (!session?.refreshToken) {
            this.sessionService.clearSession();
            return false;
        }

        const response = await fetch(`${this.apiUrl}/auth/refresh`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ refreshToken: session.refreshToken })
        });

        if (!response.ok) {
            this.sessionService.clearSession();
            return false;
        }

        const data = (await response.json()) as RefreshResponse;
        this.sessionService.setSession({
            ...session,
            token: data.token,
            refreshToken: data.refreshToken
        });

        return true;
    }

    private async fetchWithAuth(url: string, options?: RequestInit): Promise<Response> {
        const firstTry = await fetch(url, {
            ...(options ?? {}),
            headers: {
                ...(options?.headers ?? {}),
                ...this.getAuthHeaders()
            }
        });

        if (firstTry.status !== 401) {
            return firstTry;
        }

        const refreshed = await this.refreshAccessToken();
        if (!refreshed) {
            return firstTry;
        }

        return await fetch(url, {
            ...(options ?? {}),
            headers: {
                ...(options?.headers ?? {}),
                ...this.getAuthHeaders()
            }
        });
    }

    private async ensureAuthorized(response: Response): Promise<void> {
        if (response.status === 401) {
            this.sessionService.clearSession();
            throw new Error('Tu sesion ha expirado. Inicia sesion de nuevo.');
        }
    }

    async login(correo: string, contrasena: string): Promise<LoginResponse> {
        const response = await fetch(`${this.apiUrl}/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ correo, contrasena })
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'No se ha podido iniciar sesion.');
        }

        return (await response.json()) as LoginResponse;
    }

    async logout(): Promise<void> {
        const session = this.sessionService.getSession();

        if (!session?.refreshToken) {
            this.sessionService.clearSession();
            return;
        }

        try {
            const response = await this.fetchWithAuth(`${this.apiUrl}/auth/logout`, {
                method: 'POST',
                body: JSON.stringify({ refreshToken: this.sessionService.getSession()?.refreshToken })
            });

            if (!response.ok && response.status !== 401) {
                const errorText = await response.text();
                throw new Error(errorText || 'No se pudo cerrar sesion.');
            }
        } finally {
            this.sessionService.clearSession();
        }
    }

    async getPanelProfesor(profesorId: number): Promise<ProfesorPanel> {
        const response = await this.fetchWithAuth(`${this.apiUrl}/profesores/${profesorId}/panel`);

        await this.ensureAuthorized(response);

        if (!response.ok) {
            throw new Error('No se pudo cargar el panel del profesor.');
        }

        return (await response.json()) as ProfesorPanel;
    }

    async getAlumnosDeAsignatura(profesorId: number, asignaturaId: number): Promise<AsignaturaAlumnos> {
        const response = await this.fetchWithAuth(`${this.apiUrl}/profesores/${profesorId}/asignaturas/${asignaturaId}/alumnos`);

        await this.ensureAuthorized(response);

        if (!response.ok) {
            throw new Error('No se pudieron cargar los alumnos de la asignatura.');
        }

        return (await response.json()) as AsignaturaAlumnos;
    }

    async ponerNota(profesorId: number, estudianteId: number, asignaturaId: number, valor: number): Promise<void> {
        const response = await this.fetchWithAuth(`${this.apiUrl}/profesores/${profesorId}/notas`, {
            method: 'POST',
            body: JSON.stringify({ estudianteId, asignaturaId, valor })
        });

        await this.ensureAuthorized(response);

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'No se pudo guardar la nota.');
        }
    }

    async getPanelAlumno(estudianteId: number): Promise<AlumnoPanel> {
        const response = await this.fetchWithAuth(`${this.apiUrl}/estudiantes/${estudianteId}/panel`);

        await this.ensureAuthorized(response);

        if (!response.ok) {
            throw new Error('No se pudo cargar el panel del alumno.');
        }

        return (await response.json()) as AlumnoPanel;
    }
}
