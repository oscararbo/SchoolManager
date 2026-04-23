import { CsvImportEntity } from '../../../../../shared/services/school-api.service';

export type CsvErrorGroupKey =
    | 'curso'
    | 'asignatura'
    | 'profesor'
    | 'estudiante'
    | 'duplicado'
    | 'formato'
    | 'otros';

export type CsvErrorGroup = {
    key: CsvErrorGroupKey;
    label: string;
    errors: string[];
};

export const CSV_ERROR_PREVIEW_COUNT = 5;
export const MAX_CSV_FILE_SIZE_BYTES = 10 * 1024 * 1024;

export const CSV_IMPORT_ITEMS: Array<{ entidad: CsvImportEntity; titulo: string; descripcion: string; orden: string }> = [
    { entidad: 'cursos', titulo: 'Cursos', descripcion: 'Alta masiva de cursos.', orden: '1' },
    { entidad: 'asignaturas', titulo: 'Asignaturas', descripcion: 'Alta masiva de asignaturas ligadas a curso.', orden: '2' },
    { entidad: 'profesores', titulo: 'Profesores', descripcion: 'Alta masiva de profesores.', orden: '3' },
    { entidad: 'estudiantes', titulo: 'Estudiantes', descripcion: 'Alta masiva de estudiantes con su curso.', orden: '4' },
    { entidad: 'imparticiones', titulo: 'Imparticiones', descripcion: 'Relaciona profesor, asignatura y curso.', orden: '5' },
    { entidad: 'tareas', titulo: 'Tareas', descripcion: 'Crea tareas por profesor, asignatura, curso y trimestre.', orden: '6' },
    { entidad: 'matriculas', titulo: 'Matriculas', descripcion: 'Relaciona estudiante con asignaturas de su curso.', orden: '7' },
    { entidad: 'notas', titulo: 'Notas', descripcion: 'Carga masiva de calificaciones sobre tareas ya existentes.', orden: '8' }
];

export const CSV_PLANTILLAS: Record<CsvImportEntity, string> = {
    cursos: 'nombre\n1°A\n1°B\n2°A',
    asignaturas: 'nombre,cursoNombre\nMatematicas,1°A\nLengua,1°A\nCiencias,1°B',
    profesores: 'nombre,correo,contrasena\nJuan Garcia,juan@colegio.es,Pass123',
    estudiantes: 'nombre,correo,contrasena,cursoNombre\nLucia Perez,lucia@colegio.es,Pass123,1°A',
    tareas: 'profesorCorreo,asignaturaNombre,cursoNombre,trimestre,tareaNombre\njuan@colegio.es,Matematicas,1°A,1,Examen T1',
    matriculas: 'estudianteCorreo,asignaturaNombre,cursoNombre\nlucia@colegio.es,Matematicas,1°A',
    imparticiones: 'profesorCorreo,asignaturaNombre,cursoNombre\njuan@colegio.es,Matematicas,1°A',
    notas: 'profesorCorreo,estudianteCorreo,asignaturaNombre,cursoNombre,trimestre,tareaNombre,valor\njuan@colegio.es,lucia@colegio.es,Matematicas,1°A,1,Examen T1,7.50'
};

export function clasificarCsvError(error: string): CsvErrorGroupKey {
    const text = error.toLowerCase();

    if (text.includes('curso no encontrado') || text.includes('su curso')) return 'curso';
    if (text.includes('asignatura no encontrada') || text.includes('asignatura')) return 'asignatura';
    if (text.includes('profesor no encontrado') || text.includes('profesor')) return 'profesor';
    if (text.includes('estudiante no encontrado') || text.includes('estudiante')) return 'estudiante';
    if (text.includes('duplicado') || text.includes('ya existe') || text.includes('ya esta matriculado') || text.includes('ya tiene un profesor asignado')) return 'duplicado';
    if (text.includes('se esperaban') || text.includes('obligatori') || text.includes('datos incompletos') || text.includes('columnas')) return 'formato';

    return 'otros';
}

export function agruparErroresCsv(errores: string[]): CsvErrorGroup[] {
    if (errores.length === 0) return [];

    const labels: Record<CsvErrorGroupKey, string> = {
        curso: 'Curso no valido o no encontrado',
        asignatura: 'Asignatura no valida o no encontrada',
        profesor: 'Profesor no valido o no encontrado',
        estudiante: 'Estudiante no valido o no encontrado',
        duplicado: 'Registros duplicados o ya existentes',
        formato: 'Formato o datos incompletos',
        otros: 'Otros errores'
    };

    const grouped = new Map<CsvErrorGroupKey, string[]>();
    for (const err of errores) {
        const key = clasificarCsvError(err);
        const current = grouped.get(key) ?? [];
        current.push(err);
        grouped.set(key, current);
    }

    const order: CsvErrorGroupKey[] = ['curso', 'asignatura', 'profesor', 'estudiante', 'duplicado', 'formato', 'otros'];
    return order
        .filter(key => grouped.has(key))
        .map(key => ({
            key,
            label: labels[key],
            errors: grouped.get(key) ?? []
        }));
}
