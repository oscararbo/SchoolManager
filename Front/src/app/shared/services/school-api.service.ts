import { Injectable } from '@angular/core';

export interface LoginResponse {
    rol: 'profesor' | 'alumno';
    id: number;
    nombre: string;
    correo: string;
    cursoId?: number;
    curso?: string;
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

    async getPanelProfesor(profesorId: number): Promise<ProfesorPanel> {
        const response = await fetch(`${this.apiUrl}/profesores/${profesorId}/panel`);

        if (!response.ok) {
            throw new Error('No se pudo cargar el panel del profesor.');
        }

        return (await response.json()) as ProfesorPanel;
    }

    async getAlumnosDeAsignatura(profesorId: number, asignaturaId: number): Promise<AsignaturaAlumnos> {
        const response = await fetch(`${this.apiUrl}/profesores/${profesorId}/asignaturas/${asignaturaId}/alumnos`);

        if (!response.ok) {
            throw new Error('No se pudieron cargar los alumnos de la asignatura.');
        }

        return (await response.json()) as AsignaturaAlumnos;
    }

    async ponerNota(profesorId: number, estudianteId: number, asignaturaId: number, valor: number): Promise<void> {
        const response = await fetch(`${this.apiUrl}/profesores/${profesorId}/notas`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ estudianteId, asignaturaId, valor })
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'No se pudo guardar la nota.');
        }
    }

    async getPanelAlumno(estudianteId: number): Promise<AlumnoPanel> {
        const response = await fetch(`${this.apiUrl}/estudiantes/${estudianteId}/panel`);

        if (!response.ok) {
            throw new Error('No se pudo cargar el panel del alumno.');
        }

        return (await response.json()) as AlumnoPanel;
    }
}
