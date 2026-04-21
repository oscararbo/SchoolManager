import type {
  ApiAsignaturaItem,
  ApiCursoItem,
  ApiEstudianteItem,
  ApiLoginResponse,
  ApiProfesorListItem
} from './school-api.contracts';
import type {
  AsignaturaItem,
  CursoItem,
  EstudianteItem,
  LoginResponse,
  ProfesorListItem
} from './school-api.service';

function safeText(value: string | null | undefined): string {
  return (value ?? '').trim();
}

export function mapLoginResponse(api: ApiLoginResponse): LoginResponse {
  return {
    rol: api.rol,
    id: api.id,
    nombre: safeText(api.nombre),
    correo: safeText(api.correo).toLowerCase(),
    token: safeText(api.token),
    cursoId: api.cursoId,
    curso: api.curso ? safeText(api.curso) : undefined
  };
}

export function mapCursoItem(api: ApiCursoItem): CursoItem {
  return { id: api.id, nombre: safeText(api.nombre) };
}

export function mapAsignaturaItem(api: ApiAsignaturaItem): AsignaturaItem {
  return {
    id: api.id,
    nombre: safeText(api.nombre),
    curso: {
      id: api.curso.id,
      nombre: safeText(api.curso.nombre)
    },
    profesores: api.profesores.map(p => ({ profesorId: p.profesorId, nombre: safeText(p.nombre) })),
    alumnos: api.alumnos.map(a => ({ id: a.id, nombre: safeText(a.nombre) }))
  };
}

export function mapProfesorListItem(api: ApiProfesorListItem): ProfesorListItem {
  return {
    id: api.id,
    nombre: safeText(api.nombre),
    apellidos: safeText(api.apellidos),
    dni: safeText(api.dni),
    telefono: safeText(api.telefono),
    especialidad: safeText(api.especialidad),
    correo: safeText(api.correo).toLowerCase(),
    imparticiones: api.imparticiones.map(i => ({
      asignaturaId: i.asignaturaId,
      asignatura: safeText(i.asignatura),
      cursoId: i.cursoId,
      curso: safeText(i.curso)
    }))
  };
}

export function mapEstudianteItem(api: ApiEstudianteItem): EstudianteItem {
  return {
    id: api.id,
    nombre: safeText(api.nombre),
    apellidos: safeText(api.apellidos),
    dni: safeText(api.dni),
    telefono: safeText(api.telefono),
    fechaNacimiento: safeText(api.fechaNacimiento),
    correo: safeText(api.correo).toLowerCase(),
    cursoId: api.cursoId,
    curso: api.curso ? safeText(api.curso) : null
  };
}
