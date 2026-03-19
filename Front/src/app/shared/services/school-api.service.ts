import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { SessionService } from '../../core/services/session.service';
import { environment } from '../../../environments/environment';

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

            const msg = error.error?.detail ?? error.error?.title ?? error.message ?? 'Error de servidor.';
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
            return await firstValueFrom(
                this.http.post<LoginResponse>(`${this.apiUrl}/auth/login`, { correo, contrasena })
            );
        } catch (e) { throw this.extractError(e); }
    }

    /**
     * Invalida el refreshToken en el servidor y limpia la sesion local.
     * Si no hay refreshToken en sesion, solo limpia el almacenamiento local.
     */
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
    //#endregion

    //#region AdminCursos

    /** Devuelve todos los cursos registrados en el sistema. */
    async getCursos(): Promise<CursoItem[]> {
        try {
            return await firstValueFrom(this.http.get<CursoItem[]>(`${this.apiUrl}/cursos`));
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
            return await firstValueFrom(this.http.get<AsignaturaItem[]>(`${this.apiUrl}/asignaturas`));
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
            return await firstValueFrom(this.http.get<ProfesorListItem[]>(`${this.apiUrl}/profesores`));
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
            return await firstValueFrom(this.http.get<EstudianteItem[]>(`${this.apiUrl}/estudiantes`));
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
    //#endregion

    //#region AdminCsvImport

    /**
     * Importa entidades desde un archivo CSV multiparte.
     *
     * @param entidad - Tipo de entidad: `'cursos'`, `'asignaturas'`, `'profesores'` o `'estudiantes'`.
     * @param file - Archivo CSV a importar.
     * @returns Resumen de la importacion: entidades creadas, omitidas y errores por linea.
     */
    async importarCsv(entidad: string, file: File): Promise<CsvImportResult> {
        const formData = new FormData();
        formData.append('file', file);
        try {
            return await firstValueFrom(
                this.http.post<CsvImportResult>(`${this.apiUrl}/admin/csv/${entidad}`, formData)
            );
        } catch (e) { throw this.extractError(e); }
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
