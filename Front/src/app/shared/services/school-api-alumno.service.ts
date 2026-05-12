import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AlumnoMateriaDetalle, AlumnoPanel, AlumnoPanelResumen, TareaSubmision } from './school-api.types';
import { extractSchoolApiError } from './school-api.error';

@Injectable({ providedIn: 'root' })
export class SchoolApiAlumnoService {
    private readonly apiUrl = environment.apiBaseUrl;
    private http = inject(HttpClient);

    async getPanelAlumno(estudianteId: number): Promise<AlumnoPanel> {
        try {
            return await firstValueFrom(this.http.get<AlumnoPanel>(`${this.apiUrl}/estudiantes/${estudianteId}/panel`));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async getPanelAlumnoResumen(estudianteId: number): Promise<AlumnoPanelResumen> {
        try {
            return await firstValueFrom(this.http.get<AlumnoPanelResumen>(`${this.apiUrl}/estudiantes/${estudianteId}/panel-resumen`));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async getMateriaDetalle(estudianteId: number, asignaturaId: number): Promise<AlumnoMateriaDetalle> {
        try {
            return await firstValueFrom(this.http.get<AlumnoMateriaDetalle>(`${this.apiUrl}/estudiantes/${estudianteId}/materias/${asignaturaId}/detalle`));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async subirSubmision(estudianteId: number, tareaId: number, archivo: File): Promise<TareaSubmision> {
        try {
            const formData = new FormData();
            formData.append('archivo', archivo);
            return await firstValueFrom(this.http.post<TareaSubmision>(`${this.apiUrl}/estudiantes/${estudianteId}/tareas/${tareaId}/submision`, formData));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async getSubmisiones(estudianteId: number, tareaId: number): Promise<TareaSubmision[]> {
        try {
            return await firstValueFrom(this.http.get<TareaSubmision[]>(`${this.apiUrl}/estudiantes/${estudianteId}/tareas/${tareaId}/submisiones`));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async deleteSubmision(estudianteId: number, submisionId: number): Promise<void> {
        try {
            await firstValueFrom(this.http.delete<void>(`${this.apiUrl}/estudiantes/${estudianteId}/submisiones/${submisionId}`));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async marcarHecha(estudianteId: number, tareaId: number): Promise<TareaSubmision> {
        try {
            return await firstValueFrom(this.http.post<TareaSubmision>(`${this.apiUrl}/estudiantes/${estudianteId}/tareas/${tareaId}/marcar-hecha`, {}));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }
}
