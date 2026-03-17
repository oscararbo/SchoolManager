import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { SessionService } from '../../core/services/session.service';

//#region Interfaces
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

export interface ProfesorPanel {
    id: number;
    nombre: string;
    cursos: Array<{
        cursoId: number;
        curso: string;
        asignaturas: Array<{ asignaturaId: number; nombre: string }>;
    }>;
}

export interface TareaResumen {
    tareaId: number;
    nombre: string;
    trimestre: number;
}

export interface TareaDetalle {
    id: number;
    nombre: string;
    trimestre: number;
    asignaturaId: number;
    asignatura: string;
}

export interface MediasTrimestrales {
    t1: number | null;
    t2: number | null;
    t3: number | null;
}

export interface AsignaturaAlumnoNota {
    tareaId: number;
    valor: number | null;
}

export interface AsignaturaAlumno {
    estudianteId: number;
    alumno: string;
    notas: AsignaturaAlumnoNota[];
    medias: MediasTrimestrales;
    notaFinal: number | null;
}

export interface AsignaturaAlumnos {
    asignatura: { id: number; nombre: string; cursoId: number; curso: string };
    tareas: TareaResumen[];
    alumnos: AsignaturaAlumno[];
}

export interface AlumnoTarea {
    tareaId: number;
    nombre: string;
    trimestre: number;
    valor: number | null;
}

export interface AlumnoMateria {
    asignaturaId: number;
    asignatura: string;
    profesor: string | null;
    notas: AlumnoTarea[];
    medias: MediasTrimestrales;
    notaFinal: number | null;
}

export interface AlumnoPanel {
    id: number;
    nombre: string;
    curso: { cursoId: number; curso: string };
    materias: AlumnoMateria[];
}

export interface CursoItem {
    id: number;
    nombre: string;
}

export interface AsignaturaItem {
    id: number;
    nombre: string;
    curso: { id: number; nombre: string };
    profesores: Array<{ profesorId: number; nombre: string }>;
    alumnos: Array<{ id: number; nombre: string }>;
}

export interface ProfesorImparticion {
    asignaturaId: number;
    asignatura: string;
    cursoId: number;
    curso: string;
}

export interface ProfesorListItem {
    id: number;
    nombre: string;
    correo: string;
    esAdmin: boolean;
    imparticiones: ProfesorImparticion[];
}

export interface EstudianteItem {
    id: number;
    nombre: string;
    correo: string;
    cursoId: number;
    curso: string | null;
}

export interface CreateProfesorData {
    nombre: string;
    correo: string;
    contrasena: string;
    esAdmin?: boolean;
}

export interface CreateEstudianteData {
    nombre: string;
    correo: string;
    contrasena: string;
    cursoId: number;
}
//#endregion

@Injectable({ providedIn: 'root' })
export class SchoolApiService {
    private readonly apiUrl = 'http://localhost:5014/api';
    private http = inject(HttpClient);
    private sessionService = inject(SessionService);

    private extractError(error: unknown): Error {
        if (error instanceof HttpErrorResponse) {
            const msg = typeof error.error === 'string' ? error.error : (error.message ?? 'Error de servidor.');
            return new Error(msg);
        }
        return error instanceof Error ? error : new Error('Error desconocido.');
    }

    //#region Auth

    async login(correo: string, contrasena: string): Promise<LoginResponse> {
        try {
            return await firstValueFrom(
                this.http.post<LoginResponse>(`${this.apiUrl}/auth/login`, { correo, contrasena })
            );
        } catch (e) { throw this.extractError(e); }
    }

    async logout(): Promise<void> {
        const session = this.sessionService.getSession();
        if (!session?.refreshToken) { this.sessionService.clearSession(); return; }
        try {
            await firstValueFrom(
                this.http.post<void>(`${this.apiUrl}/auth/logout`, { refreshToken: session.refreshToken })
            );
        } finally {
            this.sessionService.clearSession();
        }
    }
    //#endregion

    //#region ProfesorPanel

    async getPanelProfesor(profesorId: number): Promise<ProfesorPanel> {
        try {
            return await firstValueFrom(
                this.http.get<ProfesorPanel>(`${this.apiUrl}/profesores/${profesorId}/panel`)
            );
        } catch (e) { throw this.extractError(e); }
    }

    async getAlumnosDeAsignatura(profesorId: number, asignaturaId: number): Promise<AsignaturaAlumnos> {
        try {
            return await firstValueFrom(
                this.http.get<AsignaturaAlumnos>(
                    `${this.apiUrl}/profesores/${profesorId}/asignaturas/${asignaturaId}/alumnos`
                )
            );
        } catch (e) { throw this.extractError(e); }
    }

    async ponerNota(profesorId: number, estudianteId: number, tareaId: number, valor: number): Promise<void> {
        try {
            await firstValueFrom(
                this.http.post<void>(`${this.apiUrl}/profesores/${profesorId}/notas`, { tareaId, estudianteId, valor })
            );
        } catch (e) { throw this.extractError(e); }
    }

    async crearTarea(profesorId: number, nombre: string, trimestre: number, asignaturaId: number): Promise<TareaDetalle> {
        try {
            return await firstValueFrom(
                this.http.post<TareaDetalle>(
                    `${this.apiUrl}/profesores/${profesorId}/tareas`, { nombre, trimestre, asignaturaId }
                )
            );
        } catch (e) { throw this.extractError(e); }
    }
    //#endregion

    //#region AlumnoPanel

    async getPanelAlumno(estudianteId: number): Promise<AlumnoPanel> {
        try {
            return await firstValueFrom(
                this.http.get<AlumnoPanel>(`${this.apiUrl}/estudiantes/${estudianteId}/panel`)
            );
        } catch (e) { throw this.extractError(e); }
    }
    //#endregion

    //#region AdminCursos

    async getCursos(): Promise<CursoItem[]> {
        try {
            return await firstValueFrom(this.http.get<CursoItem[]>(`${this.apiUrl}/cursos`));
        } catch (e) { throw this.extractError(e); }
    }

    async createCurso(nombre: string): Promise<CursoItem> {
        try {
            return await firstValueFrom(this.http.post<CursoItem>(`${this.apiUrl}/cursos`, { nombre }));
        } catch (e) { throw this.extractError(e); }
    }
    //#endregion

    //#region AdminAsignaturas

    async getAsignaturas(): Promise<AsignaturaItem[]> {
        try {
            return await firstValueFrom(this.http.get<AsignaturaItem[]>(`${this.apiUrl}/asignaturas`));
        } catch (e) { throw this.extractError(e); }
    }

    async createAsignatura(nombre: string, cursoId: number): Promise<AsignaturaItem> {
        try {
            return await firstValueFrom(
                this.http.post<AsignaturaItem>(`${this.apiUrl}/asignaturas`, { nombre, cursoId })
            );
        } catch (e) { throw this.extractError(e); }
    }
    //#endregion

    //#region AdminProfesores

    async getProfesores(): Promise<ProfesorListItem[]> {
        try {
            return await firstValueFrom(this.http.get<ProfesorListItem[]>(`${this.apiUrl}/profesores`));
        } catch (e) { throw this.extractError(e); }
    }

    async createProfesor(data: CreateProfesorData): Promise<ProfesorListItem> {
        try {
            return await firstValueFrom(this.http.post<ProfesorListItem>(`${this.apiUrl}/profesores`, data));
        } catch (e) { throw this.extractError(e); }
    }
    //#endregion

    //#region AdminEstudiantes

    async getEstudiantes(): Promise<EstudianteItem[]> {
        try {
            return await firstValueFrom(this.http.get<EstudianteItem[]>(`${this.apiUrl}/estudiantes`));
        } catch (e) { throw this.extractError(e); }
    }

    async createEstudiante(data: CreateEstudianteData): Promise<EstudianteItem> {
        try {
            return await firstValueFrom(this.http.post<EstudianteItem>(`${this.apiUrl}/estudiantes`, data));
        } catch (e) { throw this.extractError(e); }
    }

    async matricularEstudiante(estudianteId: number, asignaturaId: number): Promise<void> {
        try {
            await firstValueFrom(
                this.http.post<void>(
                    `${this.apiUrl}/estudiantes/${estudianteId}/asignaturas/${asignaturaId}`, {}
                )
            );
        } catch (e) { throw this.extractError(e); }
    }
    //#endregion

    //#region AdminImparticiones

    async asignarImparticion(profesorId: number, asignaturaId: number, cursoId: number): Promise<void> {
        try {
            await firstValueFrom(
                this.http.post<void>(
                    `${this.apiUrl}/profesores/${profesorId}/imparticiones`, { asignaturaId, cursoId }
                )
            );
        } catch (e) { throw this.extractError(e); }
    }
    //#endregion
}
