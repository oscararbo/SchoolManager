import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AlumnoMateriaDetalle, AlumnoPanel, AlumnoPanelResumen } from './school-api.types';
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
}
