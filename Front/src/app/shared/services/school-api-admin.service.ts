import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import type {
    ApiAsignaturaItem,
    ApiCursoItem,
    ApiEstudianteItem,
    ApiProfesorListItem
} from './school-api.contracts';
import {
    mapAsignaturaItem,
    mapCursoItem,
    mapEstudianteItem,
    mapProfesorListItem
} from './school-api.mappers';
import {
    AdminComparacionCursos,
    AdminCursoNotasStats,
    AdminCursoStatsSelector,
    AdminImparticionListItem,
    AdminMatriculaListItem,
    AdminStats,
    AsignaturaItem,
    CreateEstudianteData,
    CreateProfesorData,
    CsvImportEntity,
    CsvImportError,
    CsvImportResult,
    CursoItem,
    EstudianteItem,
    ProfesorListItem,
    TareaConNotas,
    UpdateEstudianteData,
    UpdateProfesorData
} from './school-api.types';
import { extractSchoolApiError } from './school-api.error';

@Injectable({ providedIn: 'root' })
export class SchoolApiAdminService {
    private readonly apiUrl = environment.apiBaseUrl;
    private http = inject(HttpClient);

    async getAdminStats(): Promise<AdminStats> {
        try {
            return await firstValueFrom(this.http.get<AdminStats>(`${this.apiUrl}/admin/stats`));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async getAdminCursosStatsSelector(): Promise<AdminCursoStatsSelector[]> {
        try {
            return await firstValueFrom(this.http.get<AdminCursoStatsSelector[]>(`${this.apiUrl}/admin/stats/cursos`));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async getAdminStatsByCurso(cursoId: number): Promise<AdminCursoNotasStats> {
        try {
            return await firstValueFrom(this.http.get<AdminCursoNotasStats>(`${this.apiUrl}/admin/stats/cursos/${cursoId}`));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async compararCursos(cursoIds: number[]): Promise<AdminComparacionCursos> {
        try {
            return await firstValueFrom(this.http.post<AdminComparacionCursos>(`${this.apiUrl}/admin/stats/cursos/comparar`, { cursoIds }));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async getAdminMatriculas(): Promise<AdminMatriculaListItem[]> {
        try {
            return await firstValueFrom(this.http.get<AdminMatriculaListItem[]>(`${this.apiUrl}/admin/matriculas`));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async getAdminImparticiones(): Promise<AdminImparticionListItem[]> {
        try {
            return await firstValueFrom(this.http.get<AdminImparticionListItem[]>(`${this.apiUrl}/admin/imparticiones`));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async getCursos(): Promise<CursoItem[]> {
        try {
            const response = await firstValueFrom(this.http.get<ApiCursoItem[]>(`${this.apiUrl}/cursos/simple`));
            return response.map(mapCursoItem);
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async createCurso(nombre: string): Promise<CursoItem> {
        try {
            return await firstValueFrom(this.http.post<CursoItem>(`${this.apiUrl}/cursos`, { nombre }));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async updateCurso(id: number, nombre: string): Promise<CursoItem> {
        try {
            return await firstValueFrom(this.http.put<CursoItem>(`${this.apiUrl}/cursos/${id}`, { nombre }));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async deleteCurso(id: number): Promise<void> {
        try {
            await firstValueFrom(this.http.delete<void>(`${this.apiUrl}/cursos/${id}`));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async getAsignaturas(): Promise<AsignaturaItem[]> {
        try {
            const response = await firstValueFrom(this.http.get<ApiAsignaturaItem[]>(`${this.apiUrl}/asignaturas`));
            return response.map(mapAsignaturaItem);
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async createAsignatura(nombre: string, cursoId: number): Promise<AsignaturaItem> {
        try {
            return await firstValueFrom(this.http.post<AsignaturaItem>(`${this.apiUrl}/asignaturas`, { nombre, cursoId }));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async updateAsignatura(id: number, nombre: string, cursoId: number): Promise<AsignaturaItem> {
        try {
            return await firstValueFrom(this.http.put<AsignaturaItem>(`${this.apiUrl}/asignaturas/${id}`, { nombre, cursoId }));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async deleteAsignatura(id: number): Promise<void> {
        try {
            await firstValueFrom(this.http.delete<void>(`${this.apiUrl}/asignaturas/${id}`));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async getProfesores(): Promise<ProfesorListItem[]> {
        try {
            const response = await firstValueFrom(this.http.get<ApiProfesorListItem[]>(`${this.apiUrl}/profesores`));
            return response.map(mapProfesorListItem);
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async createProfesor(data: CreateProfesorData): Promise<ProfesorListItem> {
        try {
            return await firstValueFrom(this.http.post<ProfesorListItem>(`${this.apiUrl}/profesores`, data));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async updateProfesor(id: number, data: UpdateProfesorData): Promise<ProfesorListItem> {
        try {
            return await firstValueFrom(this.http.put<ProfesorListItem>(`${this.apiUrl}/profesores/${id}`, data));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async deleteProfesor(id: number): Promise<void> {
        try {
            await firstValueFrom(this.http.delete<void>(`${this.apiUrl}/profesores/${id}`));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async getEstudiantes(): Promise<EstudianteItem[]> {
        try {
            const response = await firstValueFrom(this.http.get<ApiEstudianteItem[]>(`${this.apiUrl}/estudiantes`));
            return response.map(mapEstudianteItem);
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async createEstudiante(data: CreateEstudianteData): Promise<EstudianteItem> {
        try {
            return await firstValueFrom(this.http.post<EstudianteItem>(`${this.apiUrl}/estudiantes`, data));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async updateEstudiante(id: number, data: UpdateEstudianteData): Promise<EstudianteItem> {
        try {
            return await firstValueFrom(this.http.put<EstudianteItem>(`${this.apiUrl}/estudiantes/${id}`, data));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async deleteEstudiante(id: number): Promise<void> {
        try {
            await firstValueFrom(this.http.delete<void>(`${this.apiUrl}/estudiantes/${id}`));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async matricularEstudiante(estudianteId: number, asignaturaId: number): Promise<void> {
        try {
            await firstValueFrom(this.http.post<void>(`${this.apiUrl}/estudiantes/${estudianteId}/asignaturas/${asignaturaId}`, {}));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async desmatricularEstudiante(estudianteId: number, asignaturaId: number): Promise<void> {
        try {
            await firstValueFrom(this.http.delete<void>(`${this.apiUrl}/estudiantes/${estudianteId}/asignaturas/${asignaturaId}`));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async asignarImparticion(profesorId: number, asignaturaId: number, cursoId: number): Promise<void> {
        try {
            await firstValueFrom(this.http.post<void>(`${this.apiUrl}/profesores/${profesorId}/imparticiones`, { asignaturaId, cursoId }));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async eliminarImparticion(profesorId: number, asignaturaId: number, cursoId: number): Promise<void> {
        try {
            await firstValueFrom(this.http.delete<void>(`${this.apiUrl}/profesores/${profesorId}/imparticiones/${asignaturaId}/${cursoId}`));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async importarCsv(entidad: CsvImportEntity, file: File): Promise<CsvImportResult> {
        const formData = new FormData();
        formData.append('file', file);
        try {
            return await firstValueFrom(this.http.post<CsvImportResult>(`${this.apiUrl}/admin/csv/${entidad}`, formData));
        } catch (e) {
            if (e instanceof HttpErrorResponse) {
                const csvErrors = Array.isArray(e.error?.errores)
                    ? e.error.errores.map((x: unknown) => String(x))
                    : [];

                const result: CsvImportResult | undefined = csvErrors.length > 0
                    ? {
                        creados: Number(e.error?.creados ?? 0),
                        omitidos: Number(e.error?.omitidos ?? 0),
                        errores: csvErrors,
                        detalles: Array.isArray(e.error?.detalles)
                            ? e.error.detalles.map((x: unknown) => String(x))
                            : []
                    }
                    : undefined;

                if (result) {
                    const base = e.error?.detail ?? e.error?.mensaje ?? `La importacion de ${entidad} ha fallado.`;
                    throw new CsvImportError(`${base} ${result.errores[0]}`, result);
                }
            }

            throw extractSchoolApiError(e);
        }
    }

    async getTareasConNotas(asignaturaId: number): Promise<TareaConNotas[]> {
        try {
            return await firstValueFrom(this.http.get<TareaConNotas[]>(`${this.apiUrl}/profesores/asignaturas/${asignaturaId}/tareas-notas`));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }
}
