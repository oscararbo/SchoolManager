import type {
  ApiAsignaturaItem,
  ApiColegioAdminItem,
  ApiColegioItem,
  ApiCursoItem,
  ApiEstudianteItem,
  ApiLoginResponse,
  ApiProfesorListItem
} from './school-api.contracts';
import type {
  AsignaturaItem,
  ColegioAdminItem,
  ColegioItem,
  CursoItem,
  EstudianteItem,
  LoginResponse,
  ProfesorListItem
} from './school-api.types';

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
    colegioId: api.colegioId,
    colegio: api.colegio ? safeText(api.colegio) : undefined,
    colegioSlug: api.colegioSlug ? safeText(api.colegioSlug) : undefined,
    colegioLogoUrl: api.colegioLogoUrl ? safeText(api.colegioLogoUrl) : undefined,
    cursoId: api.cursoId,
    curso: api.curso ? safeText(api.curso) : undefined
  };
}

export function mapColegioItem(api: ApiColegioItem): ColegioItem {
  return {
    id: api.id,
    nombre: safeText(api.nombre),
    slug: safeText(api.slug),
    logoUrl: api.logoUrl ? safeText(api.logoUrl) : null,
    faviconUrl: api.faviconUrl ? safeText(api.faviconUrl) : null,
    colorPrimario: api.colorPrimario ? safeText(api.colorPrimario) : null,
    mensajeLogin: api.mensajeLogin ? safeText(api.mensajeLogin) : null,
    totalAdmins: api.totalAdmins,
    totalProfesores: api.totalProfesores,
    totalAlumnos: api.totalAlumnos,
    totalCursos: api.totalCursos
  };
}

export function mapColegioAdminItem(api: ApiColegioAdminItem): ColegioAdminItem {
  return {
    id: api.id,
    nombre: safeText(api.nombre),
    correo: safeText(api.correo).toLowerCase(),
    colegioId: api.colegioId,
    colegio: safeText(api.colegio),
    contrasenaTemporal: api.contrasenaTemporal ? safeText(api.contrasenaTemporal) : undefined
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
    contrasenaTemporal: api.contrasenaTemporal ? safeText(api.contrasenaTemporal) : undefined,
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
    contrasenaTemporal: api.contrasenaTemporal ? safeText(api.contrasenaTemporal) : undefined,
    cursoId: api.cursoId,
    curso: api.curso ? safeText(api.curso) : null
  };
}
