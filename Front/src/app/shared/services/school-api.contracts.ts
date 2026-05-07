export interface ApiLoginResponse {
  rol: 'profesor' | 'alumno' | 'admin' | 'superusuario';
  id: number;
  nombre: string;
  correo: string;
  token: string;
  colegioId?: number;
  colegio?: string;
  colegioSlug?: string;
  colegioLogoUrl?: string;
  cursoId?: number;
  curso?: string;
}

export interface ApiColegioItem {
  id: number;
  nombre: string;
  slug: string;
  logoUrl?: string | null;
  faviconUrl?: string | null;
  colorPrimario?: string | null;
  mensajeLogin?: string | null;
  totalAdmins?: number;
  totalProfesores?: number;
  totalAlumnos?: number;
  totalCursos?: number;
}

export interface ApiColegioAdminItem {
  id: number;
  nombre: string;
  correo: string;
  colegioId: number;
  colegio: string;
}

export interface ApiCursoItem {
  id: number;
  nombre: string;
}

export interface ApiAsignaturaItem {
  id: number;
  nombre: string;
  curso: { id: number; nombre: string };
  profesores: Array<{ profesorId: number; nombre: string }>;
  alumnos: Array<{ id: number; nombre: string }>;
}

export interface ApiProfesorImparticion {
  asignaturaId: number;
  asignatura: string;
  cursoId: number;
  curso: string;
}

export interface ApiProfesorListItem {
  id: number;
  nombre: string;
  apellidos: string;
  dni: string;
  telefono: string;
  especialidad: string;
  correo: string;
  imparticiones: ApiProfesorImparticion[];
}

export interface ApiEstudianteItem {
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
