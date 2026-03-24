export interface ApiLoginResponse {
  rol: 'profesor' | 'alumno' | 'admin';
  id: number;
  nombre: string;
  correo: string;
  token: string;
  refreshToken: string;
  cursoId?: number;
  curso?: string;
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
  correo: string;
  imparticiones: ApiProfesorImparticion[];
}

export interface ApiEstudianteItem {
  id: number;
  nombre: string;
  correo: string;
  cursoId: number;
  curso: string | null;
}
