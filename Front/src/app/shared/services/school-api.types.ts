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
    apellidos: string;
    dni: string;
    telefono: string;
    especialidad: string;
    correo: string;
    imparticiones: ProfesorImparticion[];
}

export interface EstudianteItem {
    id: number;
    nombre: string;
    apellidos: string;
    dni: string;
    telefono: string;
    fechaNacimiento: string;
    correo: string;
    cursoId: number;
    curso: string | null;
}

export interface CreateProfesorData {
    nombre: string;
    apellidos: string;
    dni: string;
    telefono: string;
    especialidad: string;
    correo: string;
    contrasena: string;
}

export interface UpdateProfesorData {
    nombre: string;
    apellidos: string;
    dni: string;
    telefono: string;
    especialidad: string;
    correo: string;
    nuevaContrasena?: string;
}

export interface CreateEstudianteData {
    nombre: string;
    apellidos: string;
    dni: string;
    telefono: string;
    fechaNacimiento: string;
    correo: string;
    contrasena: string;
    cursoId: number;
}

export interface UpdateEstudianteData {
    nombre: string;
    apellidos: string;
    dni: string;
    telefono: string;
    fechaNacimiento: string;
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

export type CsvImportEntity =
    | 'cursos'
    | 'asignaturas'
    | 'profesores'
    | 'estudiantes'
    | 'tareas'
    | 'matriculas'
    | 'imparticiones'
    | 'notas';
