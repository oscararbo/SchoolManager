import { Injectable, inject } from '@angular/core';
import { SchoolApiAdminService } from './school-api-admin.service';
import { SchoolApiAlumnoService } from './school-api-alumno.service';
import { SchoolApiAuthService } from './school-api-auth.service';
import { SchoolApiProfesorService } from './school-api-profesor.service';
import type {
    AdminComparacionCursos,
    AdminCursoNotasStats,
    AdminCursoStatsSelector,
    AdminImparticionListItem,
    AdminMatriculaListItem,
    AdminStats,
    AlumnoMateriaDetalle,
    AlumnoPanel,
    AlumnoPanelResumen,
    AsignaturaAlumno,
    AsignaturaAlumnos,
    AsignaturaAlumnosResumen,
    AsignaturaCalificacionesTarea,
    AsignaturaItem,
    CreateEstudianteData,
    CreateProfesorData,
    CsvImportEntity,
    CsvImportResult,
    CursoItem,
    EstudianteItem,
    LoginResponse,
    ProfesorListItem,
    ProfesorPanel,
    ProfesorStats,
    TareaConNotas,
    TareaDetalle,
    UpdateEstudianteData,
    UpdateProfesorData
} from './school-api.types';

export type {
    LoginResponse,
    ProfesorPanel,
    TareaResumen,
    TareaDetalle,
    MediasTrimestrales,
    AsignaturaAlumnoNota,
    AsignaturaAlumno,
    AsignaturaAlumnoResumen,
    AsignaturaAlumnos,
    AsignaturaAlumnosResumen,
    AsignaturaCalificacionTarea,
    AsignaturaCalificacionesTarea,
    NotaAlumnoTarea,
    TareaConNotas,
    AlumnoTarea,
    AlumnoMateria,
    AlumnoPanel,
    AlumnoMateriaResumen,
    AlumnoPanelResumen,
    AlumnoMateriaDetalle,
    CursoItem,
    AsignaturaItem,
    ProfesorImparticion,
    ProfesorListItem,
    EstudianteItem,
    CreateProfesorData,
    UpdateProfesorData,
    CreateEstudianteData,
    UpdateEstudianteData,
    CsvImportResult,
    AdminCursoStats,
    AdminStats,
    AdminAsignaturaNotasStats,
    AdminCursoStatsSelector,
    AdminCursoNotasStats,
    AdminCursoComparacionItem,
    AdminComparacionCursos,
    AdminMatriculaAsignaturaItem,
    AdminMatriculaListItem,
    AdminImparticionListItem,
    ProfesorTareaStats,
    ProfesorAsignaturaStats,
    ProfesorStats,
    CsvImportEntity
} from './school-api.types';
export { CsvImportError } from './school-api.types';

@Injectable({ providedIn: 'root' })
export class SchoolApiService {
    private auth = inject(SchoolApiAuthService);
    private profesor = inject(SchoolApiProfesorService);
    private alumno = inject(SchoolApiAlumnoService);
    private admin = inject(SchoolApiAdminService);

    // Auth
    login(correo: string, contrasena: string): Promise<LoginResponse> { return this.auth.login(correo, contrasena); }
    logout(): Promise<void> { return this.auth.logout(); }

    // Profesor panel
    getPanelProfesor(profesorId: number): Promise<ProfesorPanel> { return this.profesor.getPanelProfesor(profesorId); }
    getProfesorStats(profesorId: number): Promise<ProfesorStats> { return this.profesor.getProfesorStats(profesorId); }
    getAlumnosDeAsignatura(profesorId: number, asignaturaId: number): Promise<AsignaturaAlumnos> { return this.profesor.getAlumnosDeAsignatura(profesorId, asignaturaId); }
    getAlumnosResumenDeAsignatura(profesorId: number, asignaturaId: number): Promise<AsignaturaAlumnosResumen> { return this.profesor.getAlumnosResumenDeAsignatura(profesorId, asignaturaId); }
    getAlumnoDetalleDeAsignatura(profesorId: number, asignaturaId: number, estudianteId: number): Promise<AsignaturaAlumno> { return this.profesor.getAlumnoDetalleDeAsignatura(profesorId, asignaturaId, estudianteId); }
    getCalificacionesDeTarea(profesorId: number, asignaturaId: number, tareaId: number): Promise<AsignaturaCalificacionesTarea> { return this.profesor.getCalificacionesDeTarea(profesorId, asignaturaId, tareaId); }
    ponerNota(profesorId: number, estudianteId: number, tareaId: number, valor: number): Promise<void> { return this.profesor.ponerNota(profesorId, estudianteId, tareaId, valor); }
    crearTarea(profesorId: number, nombre: string, trimestre: number, asignaturaId: number): Promise<TareaDetalle> { return this.profesor.crearTarea(profesorId, nombre, trimestre, asignaturaId); }

    // Alumno panel
    getPanelAlumno(estudianteId: number): Promise<AlumnoPanel> { return this.alumno.getPanelAlumno(estudianteId); }
    getPanelAlumnoResumen(estudianteId: number): Promise<AlumnoPanelResumen> { return this.alumno.getPanelAlumnoResumen(estudianteId); }
    getMateriaDetalle(estudianteId: number, asignaturaId: number): Promise<AlumnoMateriaDetalle> { return this.alumno.getMateriaDetalle(estudianteId, asignaturaId); }

    // Admin
    getAdminStats(): Promise<AdminStats> { return this.admin.getAdminStats(); }
    getAdminCursosStatsSelector(): Promise<AdminCursoStatsSelector[]> { return this.admin.getAdminCursosStatsSelector(); }
    getAdminStatsByCurso(cursoId: number): Promise<AdminCursoNotasStats> { return this.admin.getAdminStatsByCurso(cursoId); }
    compararCursos(cursoIds: number[]): Promise<AdminComparacionCursos> { return this.admin.compararCursos(cursoIds); }
    getAdminMatriculas(): Promise<AdminMatriculaListItem[]> { return this.admin.getAdminMatriculas(); }
    getAdminImparticiones(): Promise<AdminImparticionListItem[]> { return this.admin.getAdminImparticiones(); }
    getCursos(): Promise<CursoItem[]> { return this.admin.getCursos(); }
    createCurso(nombre: string): Promise<CursoItem> { return this.admin.createCurso(nombre); }
    updateCurso(id: number, nombre: string): Promise<CursoItem> { return this.admin.updateCurso(id, nombre); }
    deleteCurso(id: number): Promise<void> { return this.admin.deleteCurso(id); }
    getAsignaturas(): Promise<AsignaturaItem[]> { return this.admin.getAsignaturas(); }
    createAsignatura(nombre: string, cursoId: number): Promise<AsignaturaItem> { return this.admin.createAsignatura(nombre, cursoId); }
    updateAsignatura(id: number, nombre: string, cursoId: number): Promise<AsignaturaItem> { return this.admin.updateAsignatura(id, nombre, cursoId); }
    deleteAsignatura(id: number): Promise<void> { return this.admin.deleteAsignatura(id); }
    getProfesores(): Promise<ProfesorListItem[]> { return this.admin.getProfesores(); }
    createProfesor(data: CreateProfesorData): Promise<ProfesorListItem> { return this.admin.createProfesor(data); }
    updateProfesor(id: number, data: UpdateProfesorData): Promise<ProfesorListItem> { return this.admin.updateProfesor(id, data); }
    deleteProfesor(id: number): Promise<void> { return this.admin.deleteProfesor(id); }
    getEstudiantes(): Promise<EstudianteItem[]> { return this.admin.getEstudiantes(); }
    createEstudiante(data: CreateEstudianteData): Promise<EstudianteItem> { return this.admin.createEstudiante(data); }
    updateEstudiante(id: number, data: UpdateEstudianteData): Promise<EstudianteItem> { return this.admin.updateEstudiante(id, data); }
    deleteEstudiante(id: number): Promise<void> { return this.admin.deleteEstudiante(id); }
    matricularEstudiante(estudianteId: number, asignaturaId: number): Promise<void> { return this.admin.matricularEstudiante(estudianteId, asignaturaId); }
    desmatricularEstudiante(estudianteId: number, asignaturaId: number): Promise<void> { return this.admin.desmatricularEstudiante(estudianteId, asignaturaId); }
    asignarImparticion(profesorId: number, asignaturaId: number, cursoId: number): Promise<void> { return this.admin.asignarImparticion(profesorId, asignaturaId, cursoId); }
    eliminarImparticion(profesorId: number, asignaturaId: number, cursoId: number): Promise<void> { return this.admin.eliminarImparticion(profesorId, asignaturaId, cursoId); }
    importarCsv(entidad: CsvImportEntity, file: File): Promise<CsvImportResult> { return this.admin.importarCsv(entidad, file); }
    getTareasConNotas(asignaturaId: number): Promise<TareaConNotas[]> { return this.admin.getTareasConNotas(asignaturaId); }
}
