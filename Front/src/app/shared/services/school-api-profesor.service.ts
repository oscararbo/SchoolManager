import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
    AsignaturaAlumno,
    AsignaturaAlumnos,
    AsignaturaAlumnosResumen,
    AsignaturaCalificacionesTarea,
    ProfesorPanel,
    ProfesorStats,
    TareaDetalle
} from './school-api.types';
import { extractSchoolApiError } from './school-api.error';

@Injectable({ providedIn: 'root' })
export class SchoolApiProfesorService {
    private readonly apiUrl = environment.apiBaseUrl;
    private http = inject(HttpClient);

    async getPanelProfesor(profesorId: number): Promise<ProfesorPanel> {
        try {
            return await firstValueFrom(this.http.get<ProfesorPanel>(`${this.apiUrl}/profesores/${profesorId}/panel`));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async getProfesorStats(profesorId: number): Promise<ProfesorStats> {
        try {
            return await firstValueFrom(this.http.get<ProfesorStats>(`${this.apiUrl}/profesores/${profesorId}/stats`));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async getAlumnosDeAsignatura(profesorId: number, asignaturaId: number): Promise<AsignaturaAlumnos> {
        try {
            return await firstValueFrom(this.http.get<AsignaturaAlumnos>(`${this.apiUrl}/profesores/${profesorId}/asignaturas/${asignaturaId}/alumnos`));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async getAlumnosResumenDeAsignatura(profesorId: number, asignaturaId: number): Promise<AsignaturaAlumnosResumen> {
        try {
            return await firstValueFrom(this.http.get<AsignaturaAlumnosResumen>(`${this.apiUrl}/profesores/${profesorId}/asignaturas/${asignaturaId}/alumnos-resumen`));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async getAlumnoDetalleDeAsignatura(profesorId: number, asignaturaId: number, estudianteId: number): Promise<AsignaturaAlumno> {
        try {
            return await firstValueFrom(this.http.get<AsignaturaAlumno>(`${this.apiUrl}/profesores/${profesorId}/asignaturas/${asignaturaId}/alumnos/${estudianteId}/detalle`));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async getCalificacionesDeTarea(profesorId: number, asignaturaId: number, tareaId: number): Promise<AsignaturaCalificacionesTarea> {
        try {
            return await firstValueFrom(this.http.get<AsignaturaCalificacionesTarea>(`${this.apiUrl}/profesores/${profesorId}/asignaturas/${asignaturaId}/tareas/${tareaId}/calificaciones`));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async ponerNota(profesorId: number, estudianteId: number, tareaId: number, valor: number): Promise<void> {
        try {
            await firstValueFrom(this.http.post<void>(`${this.apiUrl}/profesores/${profesorId}/notas`, { tareaId, estudianteId, valor }));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async crearTarea(profesorId: number, nombre: string, trimestre: number, asignaturaId: number): Promise<TareaDetalle> {
        try {
            return await firstValueFrom(this.http.post<TareaDetalle>(`${this.apiUrl}/profesores/${profesorId}/tareas`, { nombre, trimestre, asignaturaId }));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }
}
