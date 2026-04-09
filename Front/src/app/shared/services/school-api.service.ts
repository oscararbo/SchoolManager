import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { SessionService } from '../../core/services/session.service';
import { environment } from '../../../environments/environment';
import type {
    ApiAsignaturaItem,
    ApiCursoItem,
    ApiEstudianteItem,
    ApiLoginResponse,
    ApiProfesorListItem
} from './school-api.contracts';
import {
    mapAsignaturaItem,
    mapCursoItem,
    mapEstudianteItem,
    mapLoginResponse,
    mapProfesorListItem
} from './school-api.mappers';

//#region Interfaces
export interface LoginResponse {
    rol: 'profesor' | 'alumno' | 'admin';
    id: number;
    nombre: string;
    correo: string;
    token: string;
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

export interface AsignaturaAlumnoResumen {
    estudianteId: number;
    alumno: string;
    medias: MediasTrimestrales;
    notaFinal: number | null;
}

export interface AsignaturaAlumnos {
    asignatura: { id: number; nombre: string; cursoId: number; curso: string };
    tareas: TareaResumen[];
    alumnos: AsignaturaAlumno[];
}

export interface AsignaturaAlumnosResumen {
    asignatura: { id: number; nombre: string; cursoId: number; curso: string };
    tareas: TareaResumen[];
    alumnos: AsignaturaAlumnoResumen[];
}

export interface AsignaturaCalificacionTarea {
    estudianteId: number;
    alumno: string;
    valor: number | null;
}

export interface AsignaturaCalificacionesTarea {
    tareaId: number;
    tarea: string;
    trimestre: number;
    calificaciones: AsignaturaCalificacionTarea[];
}

export interface NotaAlumnoTarea {
    estudianteId: number;
    alumno: string;
    valor: number | null;
}

export interface TareaConNotas {
    tareaId: number;
    nombre: string;
    trimestre: number;
    asignaturaId: number;
    asignatura: string;
    notas: NotaAlumnoTarea[];
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

export interface AlumnoMateriaResumen {
    asignaturaId: number;
    asignatura: string;
    profesor: string | null;
}

export interface AlumnoPanelResumen {
    id: number;
    nombre: string;
    curso: { cursoId: number; curso: string };
    materias: AlumnoMateriaResumen[];
}

export interface AlumnoMateriaDetalle {
    asignaturaId: number;
    asignatura: string;
    profesor: string | null;
    notas: AlumnoTarea[];
    medias: MediasTrimestrales;
    notaFinal: number | null;
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
}

export interface UpdateProfesorData {
    nombre: string;
    correo: string;
    nuevaContrasena?: string;
}

export interface CreateEstudianteData {
    nombre: string;
    correo: string;
    contrasena: string;
    cursoId: number;
}

export interface UpdateEstudianteData {
    nombre: string;
    correo: string;
    cursoId: number;
    nuevaContrasena?: string;
}

export interface CsvImportResult {
    creados: number;
    errores: string[];
    omitidos?: number;
    detalles?: string[];
}

export interface AdminCursoStats {
    curso: string;
    estudiantes: number;
    asignaturas: number;
}

export interface AdminStats {
    totalCursos: number;
    totalAsignaturas: number;
    totalProfesores: number;
    totalEstudiantes: number;
    totalMatriculas: number;
    totalTareas: number;
    porCurso: AdminCursoStats[];
}

export interface AdminAsignaturaNotasStats {
    asignaturaId: number;
    asignatura: string;
    totalAlumnos: number;
    aprobados: number;
    suspensos: number;
    sinNota: number;
    media: number | null;
}

export interface AdminCursoStatsSelector {
    cursoId: number;
    curso: string;
    totalEstudiantes: number;
    totalAsignaturas: number;
}

export interface AdminCursoNotasStats {
    cursoId: number;
    curso: string;
    mediaGlobalCurso: number | null;
    totalAlumnos: number;
    aprobados: number;
    suspensos: number;
    sinNota: number;
    asignaturas: AdminAsignaturaNotasStats[];
}

export interface AdminCursoComparacionItem {
    cursoId: number;
    curso: string;
    mediaGlobalCurso: number | null;
    totalAlumnos: number;
    aprobados: number;
    suspensos: number;
    sinNota: number;
}

export interface AdminComparacionCursos {
    cursos: AdminCursoComparacionItem[];
}

export interface AdminMatriculaAsignaturaItem {
    asignaturaId: number;
    asignatura: string;
}

export interface AdminMatriculaListItem {
    estudianteId: number;
    estudiante: string;
    cursoId: number;
    curso: string | null;
    asignaturas: AdminMatriculaAsignaturaItem[];
}

export interface AdminImparticionListItem {
    profesorId: number;
    profesor: string;
    asignaturaId: number;
    asignatura: string;
    cursoId: number;
    curso: string;
}

export interface ProfesorTareaStats {
    tareaId: number;
    nombre: string;
    trimestre: number;
    media: number | null;
    totalNotas: number;
    sinNota: number;
    notaMax: number | null;
    notaMin: number | null;
}

export interface ProfesorAsignaturaStats {
    asignaturaId: number;
    asignatura: string;
    curso: string;
    totalAlumnos: number;
    aprobados: number;
    suspensos: number;
    sinNota: number;
    media: number | null;
    porTarea: ProfesorTareaStats[];
}

export interface ProfesorStats {
    profesorId: number;
    nombre: string;
    mediaGlobal: number | null;
    asignaturas: ProfesorAsignaturaStats[];
}

export class CsvImportError extends Error {
    constructor(message: string, public readonly result?: CsvImportResult) {
        super(message);
        this.name = 'CsvImportError';
    }
}

export type CsvImportEntity = 'cursos' | 'asignaturas' | 'profesores' | 'estudiantes' | 'tareas' | 'matriculas' | 'imparticiones' | 'notas';
//#endregion

@Injectable({ providedIn: 'root' })
export class SchoolApiService {
    private readonly apiUrl = environment.apiBaseUrl;
    private http = inject(HttpClient);
    private sessionService = inject(SessionService);

    /**
     * Normaliza cualquier error HTTP o de red en un objeto `Error` con mensaje legible.
     * Prioriza texto plano, luego errores de validacion, luego ProblemDetails.
     *
     * @param error - Excepcion capturada (puede ser {@link HttpErrorResponse} u otro tipo).
     * @returns `Error` con el mensaje mas descriptivo disponible.
     */
    private extractError(error: unknown): Error {
        if (error instanceof HttpErrorResponse) {
            if (error.status === 0) {
                return new Error('No se pudo conectar con el servidor. Comprueba que el backend este en ejecucion.');
            }

            if (typeof error.error === 'string' && error.error.trim()) {
                return new Error(error.error);
            }

            const validationErrors = error.error?.errors;
            if (validationErrors && typeof validationErrors === 'object') {
                const firstMessage = Object.values(validationErrors)
                    .flatMap(value => Array.isArray(value) ? value : [String(value)])
                    .find(Boolean);

                if (firstMessage) {
                    return new Error(String(firstMessage));
                }
            }

            const csvErrors = error.error?.errores;
            if (Array.isArray(csvErrors) && csvErrors.length > 0) {
                const firstCsvError = String(csvErrors[0]);
                const base = error.error?.detail ?? error.error?.mensaje ?? 'La operacion ha fallado.';
                return new Error(`${base} ${firstCsvError}`);
            }

            const msg = error.error?.detail ?? error.error?.mensaje ?? error.error?.title ?? error.message ?? 'Error de servidor.';
            return new Error(msg);
        }
        return error instanceof Error ? error : new Error('Error desconocido.');
    }

    //#region Auth

    /**
     * Autentica al usuario y devuelve los datos de sesion con token y rol.
     *
     * @param correo - Correo electronico del usuario.
     * @param contrasena - Contrasena en texto plano.
     * @returns Datos de sesion: token, refreshToken, rol, nombre e id.
     * @throws {Error} Si las credenciales son incorrectas o el servidor no responde.
     */
    async login(correo: string, contrasena: string): Promise<LoginResponse> {
        try {
            const response = await firstValueFrom(
                this.http.post<ApiLoginResponse>(`${this.apiUrl}/auth/login`, { correo, contrasena })
            );
            return mapLoginResponse(response);
        } catch (e) { throw this.extractError(e); }
    }

    /**
     * Invalida el refreshToken en el servidor y limpia la sesion local.
     * Si no hay refreshToken en sesion, solo limpia el almacenamiento local.
     */
    async logout(): Promise<void> {
        try {
            await firstValueFrom(
                this.http.post<void>(`${this.apiUrl}/auth/logout`, {})
            );
        } finally {
            this.sessionService.clearSession();
        }
    }
    //#endregion

    //#region ProfesorPanel

    /**
     * Obtiene el panel del profesor con sus cursos y asignaturas asignadas.
     *
     * @param profesorId - Identificador del profesor.
     * @returns Estructura del panel con los cursos e imparticiones del profesor.
     */
    async getPanelProfesor(profesorId: number): Promise<ProfesorPanel> {
        try {
            return await firstValueFrom(
                this.http.get<ProfesorPanel>(`${this.apiUrl}/profesores/${profesorId}/panel`)
            );
        } catch (e) { throw this.extractError(e); }
    }

    async getProfesorStats(profesorId: number): Promise<ProfesorStats> {
        try {
            return await firstValueFrom(
                this.http.get<ProfesorStats>(`${this.apiUrl}/profesores/${profesorId}/stats`)
            );
        } catch (e) { throw this.extractError(e); }
    }

    /**
     * Devuelve los alumnos matriculados en una asignatura con sus notas por tarea.
     *
     * @param profesorId - Identificador del profesor propietario de la asignatura.
     * @param asignaturaId - Identificador de la asignatura.
     * @returns Alumnos, lista de tareas y notas actuales.
     */
    async getAlumnosDeAsignatura(profesorId: number, asignaturaId: number): Promise<AsignaturaAlumnos> {
        try {
            return await firstValueFrom(
                this.http.get<AsignaturaAlumnos>(
                    `${this.apiUrl}/profesores/${profesorId}/asignaturas/${asignaturaId}/alumnos`
                )
            );
        } catch (e) { throw this.extractError(e); }
    }

    /**
     * Devuelve el resumen de alumnos (medias y nota final) de una asignatura.
     */
    async getAlumnosResumenDeAsignatura(profesorId: number, asignaturaId: number): Promise<AsignaturaAlumnosResumen> {
        try {
            return await firstValueFrom(
                this.http.get<AsignaturaAlumnosResumen>(
                    `${this.apiUrl}/profesores/${profesorId}/asignaturas/${asignaturaId}/alumnos-resumen`
                )
            );
        } catch (e) { throw this.extractError(e); }
    }

    /**
     * Devuelve el detalle de notas de un alumno concreto en una asignatura.
     */
    async getAlumnoDetalleDeAsignatura(profesorId: number, asignaturaId: number, estudianteId: number): Promise<AsignaturaAlumno> {
        try {
            return await firstValueFrom(
                this.http.get<AsignaturaAlumno>(
                    `${this.apiUrl}/profesores/${profesorId}/asignaturas/${asignaturaId}/alumnos/${estudianteId}/detalle`
                )
            );
        } catch (e) { throw this.extractError(e); }
    }

    /**
     * Devuelve las calificaciones de una tarea para todos los alumnos de la asignatura.
     */
    async getCalificacionesDeTarea(profesorId: number, asignaturaId: number, tareaId: number): Promise<AsignaturaCalificacionesTarea> {
        try {
            return await firstValueFrom(
                this.http.get<AsignaturaCalificacionesTarea>(
                    `${this.apiUrl}/profesores/${profesorId}/asignaturas/${asignaturaId}/tareas/${tareaId}/calificaciones`
                )
            );
        } catch (e) { throw this.extractError(e); }
    }

    /**
     * Registra o actualiza la nota de un estudiante para una tarea concreta.
     *
     * @param profesorId - Identificador del profesor.
     * @param estudianteId - Identificador del estudiante.
     * @param tareaId - Identificador de la tarea.
     * @param valor - Calificacion entre 0 y 10.
     */
    async ponerNota(profesorId: number, estudianteId: number, tareaId: number, valor: number): Promise<void> {
        try {
            await firstValueFrom(
                this.http.post<void>(`${this.apiUrl}/profesores/${profesorId}/notas`, { tareaId, estudianteId, valor })
            );
        } catch (e) { throw this.extractError(e); }
    }

    /**
     * Crea una nueva tarea en una asignatura del profesor.
     *
     * @param profesorId - Identificador del profesor.
     * @param nombre - Nombre descriptivo de la tarea.
     * @param trimestre - Trimestre al que pertenece (1, 2 o 3).
     * @param asignaturaId - Identificador de la asignatura.
     * @returns Detalle de la tarea recien creada.
     */
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

    /**
     * Obtiene el panel del alumno con sus materias, notas y medias trimestrales.
     *
     * @param estudianteId - Identificador del alumno.
     * @returns Panel completo con materias y progreso academico.
     */
    async getPanelAlumno(estudianteId: number): Promise<AlumnoPanel> {
        try {
            return await firstValueFrom(
                this.http.get<AlumnoPanel>(`${this.apiUrl}/estudiantes/${estudianteId}/panel`)
            );
        } catch (e) { throw this.extractError(e); }
    }

    async getPanelAlumnoResumen(estudianteId: number): Promise<AlumnoPanelResumen> {
        try {
            return await firstValueFrom(
                this.http.get<AlumnoPanelResumen>(`${this.apiUrl}/estudiantes/${estudianteId}/panel-resumen`)
            );
        } catch (e) { throw this.extractError(e); }
    }

    async getMateriaDetalle(estudianteId: number, asignaturaId: number): Promise<AlumnoMateriaDetalle> {
        try {
            return await firstValueFrom(
                this.http.get<AlumnoMateriaDetalle>(`${this.apiUrl}/estudiantes/${estudianteId}/materias/${asignaturaId}/detalle`)
            );
        } catch (e) { throw this.extractError(e); }
    }
    //#endregion

    //#region AdminCursos

    async getAdminStats(): Promise<AdminStats> {
        try {
            return await firstValueFrom(this.http.get<AdminStats>(`${this.apiUrl}/admin/stats`));
        } catch (e) { throw this.extractError(e); }
    }

    async getAdminCursosStatsSelector(): Promise<AdminCursoStatsSelector[]> {
        try {
            return await firstValueFrom(this.http.get<AdminCursoStatsSelector[]>(`${this.apiUrl}/admin/stats/cursos`));
        } catch (e) { throw this.extractError(e); }
    }

    async getAdminStatsByCurso(cursoId: number): Promise<AdminCursoNotasStats> {
        try {
            return await firstValueFrom(this.http.get<AdminCursoNotasStats>(`${this.apiUrl}/admin/stats/cursos/${cursoId}`));
        } catch (e) { throw this.extractError(e); }
    }

    async compararCursos(cursoIds: number[]): Promise<AdminComparacionCursos> {
        try {
            return await firstValueFrom(this.http.post<AdminComparacionCursos>(`${this.apiUrl}/admin/stats/cursos/comparar`, { cursoIds }));
        } catch (e) { throw this.extractError(e); }
    }

    async getAdminMatriculas(): Promise<AdminMatriculaListItem[]> {
        try {
            return await firstValueFrom(this.http.get<AdminMatriculaListItem[]>(`${this.apiUrl}/admin/matriculas`));
        } catch (e) { throw this.extractError(e); }
    }

    async getAdminImparticiones(): Promise<AdminImparticionListItem[]> {
        try {
            return await firstValueFrom(this.http.get<AdminImparticionListItem[]>(`${this.apiUrl}/admin/imparticiones`));
        } catch (e) { throw this.extractError(e); }
    }

    /** Devuelve todos los cursos registrados en el sistema. */
    async getCursos(): Promise<CursoItem[]> {
        try {
            const response = await firstValueFrom(this.http.get<ApiCursoItem[]>(`${this.apiUrl}/cursos/simple`));
            return response.map(mapCursoItem);
        } catch (e) { throw this.extractError(e); }
    }

    /**
     * Crea un nuevo curso.
     *
     * @param nombre - Nombre del curso.
     */
    async createCurso(nombre: string): Promise<CursoItem> {
        try {
            return await firstValueFrom(this.http.post<CursoItem>(`${this.apiUrl}/cursos`, { nombre }));
        } catch (e) { throw this.extractError(e); }
    }

    /**
     * Actualiza el nombre de un curso existente.
     *
     * @param id - Identificador del curso.
     * @param nombre - Nuevo nombre.
     */
    async updateCurso(id: number, nombre: string): Promise<CursoItem> {
        try {
            return await firstValueFrom(this.http.put<CursoItem>(`${this.apiUrl}/cursos/${id}`, { nombre }));
        } catch (e) { throw this.extractError(e); }
    }

    /**
     * Elimina un curso y todas sus dependencias en cascada.
     *
     * @param id - Identificador del curso.
     */
    async deleteCurso(id: number): Promise<void> {
        try {
            await firstValueFrom(this.http.delete<void>(`${this.apiUrl}/cursos/${id}`));
        } catch (e) { throw this.extractError(e); }
    }
    //#endregion

    //#region AdminAsignaturas

    /** Devuelve todas las asignaturas con su curso y relaciones (profesores y alumnos). */
    async getAsignaturas(): Promise<AsignaturaItem[]> {
        try {
            const response = await firstValueFrom(this.http.get<ApiAsignaturaItem[]>(`${this.apiUrl}/asignaturas`));
            return response.map(mapAsignaturaItem);
        } catch (e) { throw this.extractError(e); }
    }

    /**
     * Crea una asignatura vinculada al curso indicado.
     *
     * @param nombre - Nombre de la asignatura.
     * @param cursoId - Identificador del curso al que pertenece.
     */
    async createAsignatura(nombre: string, cursoId: number): Promise<AsignaturaItem> {
        try {
            return await firstValueFrom(
                this.http.post<AsignaturaItem>(`${this.apiUrl}/asignaturas`, { nombre, cursoId })
            );
        } catch (e) { throw this.extractError(e); }
    }

    /**
     * Actualiza el nombre y el curso de una asignatura.
     *
     * @param id - Identificador de la asignatura.
     * @param nombre - Nuevo nombre.
     * @param cursoId - Identificador del nuevo curso.
     */
    async updateAsignatura(id: number, nombre: string, cursoId: number): Promise<AsignaturaItem> {
        try {
            return await firstValueFrom(
                this.http.put<AsignaturaItem>(`${this.apiUrl}/asignaturas/${id}`, { nombre, cursoId })
            );
        } catch (e) { throw this.extractError(e); }
    }

    /**
     * Elimina una asignatura junto con sus tareas y notas asociadas.
     *
     * @param id - Identificador de la asignatura.
     */
    async deleteAsignatura(id: number): Promise<void> {
        try {
            await firstValueFrom(this.http.delete<void>(`${this.apiUrl}/asignaturas/${id}`));
        } catch (e) { throw this.extractError(e); }
    }
    //#endregion

    //#region AdminProfesores

    /** Devuelve todos los profesores con sus imparticiones. */
    async getProfesores(): Promise<ProfesorListItem[]> {
        try {
            const response = await firstValueFrom(this.http.get<ApiProfesorListItem[]>(`${this.apiUrl}/profesores`));
            return response.map(mapProfesorListItem);
        } catch (e) { throw this.extractError(e); }
    }

    /**
     * Crea un nuevo profesor.
     *
     * @param data - Datos del nuevo profesor (nombre, correo, contrasena, esAdmin).
     */
    async createProfesor(data: CreateProfesorData): Promise<ProfesorListItem> {
        try {
            return await firstValueFrom(this.http.post<ProfesorListItem>(`${this.apiUrl}/profesores`, data));
        } catch (e) { throw this.extractError(e); }
    }

    /**
     * Actualiza los datos de un profesor existente.
     *
     * @param id - Identificador del profesor.
     * @param data - Nuevos datos (puede incluir nueva contrasena opcional).
     */
    async updateProfesor(id: number, data: UpdateProfesorData): Promise<ProfesorListItem> {
        try {
            return await firstValueFrom(this.http.put<ProfesorListItem>(`${this.apiUrl}/profesores/${id}`, data));
        } catch (e) { throw this.extractError(e); }
    }

    /**
     * Elimina un profesor y sus imparticiones y tareas asociadas.
     *
     * @param id - Identificador del profesor.
     */
    async deleteProfesor(id: number): Promise<void> {
        try {
            await firstValueFrom(this.http.delete<void>(`${this.apiUrl}/profesores/${id}`));
        } catch (e) { throw this.extractError(e); }
    }
    //#endregion

    //#region AdminEstudiantes

    /** Devuelve todos los estudiantes con su curso de pertenencia. */
    async getEstudiantes(): Promise<EstudianteItem[]> {
        try {
            const response = await firstValueFrom(this.http.get<ApiEstudianteItem[]>(`${this.apiUrl}/estudiantes`));
            return response.map(mapEstudianteItem);
        } catch (e) { throw this.extractError(e); }
    }

    /**
     * Crea un nuevo estudiante y lo asigna al curso indicado.
     *
     * @param data - Datos del estudiante (nombre, correo, contrasena, cursoId).
     */
    async createEstudiante(data: CreateEstudianteData): Promise<EstudianteItem> {
        try {
            return await firstValueFrom(this.http.post<EstudianteItem>(`${this.apiUrl}/estudiantes`, data));
        } catch (e) { throw this.extractError(e); }
    }

    /**
     * Actualiza los datos de un estudiante existente.
     *
     * @param id - Identificador del estudiante.
     * @param data - Nuevos datos (puede incluir nueva contrasena y cambio de curso).
     */
    async updateEstudiante(id: number, data: UpdateEstudianteData): Promise<EstudianteItem> {
        try {
            return await firstValueFrom(this.http.put<EstudianteItem>(`${this.apiUrl}/estudiantes/${id}`, data));
        } catch (e) { throw this.extractError(e); }
    }

    /**
     * Elimina un estudiante y sus matriculas y notas asociadas.
     *
     * @param id - Identificador del estudiante.
     */
    async deleteEstudiante(id: number): Promise<void> {
        try {
            await firstValueFrom(this.http.delete<void>(`${this.apiUrl}/estudiantes/${id}`));
        } catch (e) { throw this.extractError(e); }
    }

    /**
     * Matricula a un estudiante en una asignatura de su curso.
     *
     * @param estudianteId - Identificador del estudiante.
     * @param asignaturaId - Identificador de la asignatura.
     */
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

    /**
     * Asigna a un profesor la imparticion de una asignatura en un curso.
     *
     * @param profesorId - Identificador del profesor.
     * @param asignaturaId - Identificador de la asignatura.
     * @param cursoId - Identificador del curso.
     */
    async asignarImparticion(profesorId: number, asignaturaId: number, cursoId: number): Promise<void> {
        try {
            await firstValueFrom(
                this.http.post<void>(
                    `${this.apiUrl}/profesores/${profesorId}/imparticiones`, { asignaturaId, cursoId }
                )
            );
        } catch (e) { throw this.extractError(e); }
    }

    async desmatricularEstudiante(estudianteId: number, asignaturaId: number): Promise<void> {
        try {
            await firstValueFrom(
                this.http.delete<void>(`${this.apiUrl}/estudiantes/${estudianteId}/asignaturas/${asignaturaId}`)
            );
        } catch (e) { throw this.extractError(e); }
    }

    async eliminarImparticion(profesorId: number, asignaturaId: number, cursoId: number): Promise<void> {
        try {
            await firstValueFrom(
                this.http.delete<void>(`${this.apiUrl}/profesores/${profesorId}/imparticiones/${asignaturaId}/${cursoId}`)
            );
        } catch (e) { throw this.extractError(e); }
    }
    //#endregion

    //#region AdminCsvImport

    /**
     * Importa entidades desde un archivo CSV multiparte.
     *
    * @param entidad - Tipo de entidad: `'cursos'`, `'asignaturas'`, `'profesores'`, `'estudiantes'`, `'tareas'`, `'matriculas'`, `'imparticiones'` o `'notas'`.
     * @param file - Archivo CSV a importar.
     * @returns Resumen de la importacion: entidades creadas, omitidas y errores por linea.
     */
    async importarCsv(entidad: CsvImportEntity, file: File): Promise<CsvImportResult> {
        const formData = new FormData();
        formData.append('file', file);
        try {
            return await firstValueFrom(
                this.http.post<CsvImportResult>(`${this.apiUrl}/admin/csv/${entidad}`, formData)
            );
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

            throw this.extractError(e);
        }
    }
    //#endregion

    //#region AdminTareas

    /**
     * Obtiene todas las tareas de una asignatura con las notas de todos los alumnos.
     * Disponible solo para administradores.
     *
     * @param asignaturaId - Identificador de la asignatura.
     * @returns Lista de tareas con las notas de todos los alumnos.
     */
    async getTareasConNotas(asignaturaId: number): Promise<TareaConNotas[]> {
        try {
            return await firstValueFrom(
                this.http.get<TareaConNotas[]>(`${this.apiUrl}/profesores/asignaturas/${asignaturaId}/tareas-notas`)
            );
        } catch (e) { throw this.extractError(e); }
    }
    //#endregion
}
