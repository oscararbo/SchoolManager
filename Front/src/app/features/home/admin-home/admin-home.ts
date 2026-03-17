import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
    SchoolApiService,
    CursoItem, AsignaturaItem, ProfesorListItem, EstudianteItem
} from '../../../shared/services/school-api.service';

type AdminTab = 'cursos' | 'asignaturas' | 'profesores' | 'estudiantes' | 'matriculas' | 'imparticiones';

@Component({
    selector: 'app-admin-home',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './admin-home.html',
    styleUrl: './admin-home.scss'
})
export class AdminHomeComponent implements OnInit {
    private api = inject(SchoolApiService);

    tabActiva = signal<AdminTab>('cursos');
    cargando = signal(false);
    error = signal<string | null>(null);
    exito = signal<string | null>(null);

    // Data
    cursos = signal<CursoItem[]>([]);
    asignaturas = signal<AsignaturaItem[]>([]);
    profesores = signal<ProfesorListItem[]>([]);
    estudiantes = signal<EstudianteItem[]>([]);

    // Curso form
    nuevoCursoNombre = '';

    // Asignatura form
    nuevaAsignaturaNombre = '';
    nuevaAsignaturaCursoId: number | null = null;
    filtroAsignaturasCursoId: number | null = null;

    // Profesor form
    nuevoProfesorNombre = '';
    nuevoProfesorCorreo = '';
    nuevoProfesorContrasena = '';
    nuevoProfesorEsAdmin = false;
    filtroProfesoresCursoId: number | null = null;

    // Estudiante form
    nuevoEstudianteNombre = '';
    nuevoEstudianteCorreo = '';
    nuevoEstudianteContrasena = '';
    nuevoEstudianteCursoId: number | null = null;
    filtroEstudiantesCursoId: number | null = null;

    // Matrícula form
    matriculaEstudianteId: number | null = null;
    matriculaAsignaturaId: number | null = null;
    filtroMatriculasCursoId: number | null = null;

    // Impartición form
    imparticionProfesorId: number | null = null;
    imparticionAsignaturaId: number | null = null;
    imparticionCursoId: number | null = null;
    filtroImparticionesCursoId: number | null = null;

    async ngOnInit(): Promise<void> {
        await this.cargarTodo();
    }

    cambiarTab(tab: AdminTab): void {
        this.tabActiva.set(tab);
        this.error.set(null);
        this.exito.set(null);
    }

    private async cargarTodo(): Promise<void> {
        this.cargando.set(true);
        try {
            const [cursos, asignaturas, profesores, estudiantes] = await Promise.all([
                this.api.getCursos(),
                this.api.getAsignaturas(),
                this.api.getProfesores(),
                this.api.getEstudiantes()
            ]);
            this.cursos.set(cursos);
            this.asignaturas.set(asignaturas);
            this.profesores.set(profesores);
            this.estudiantes.set(estudiantes);
        } catch (e) {
            this.error.set((e as Error).message);
        } finally {
            this.cargando.set(false);
        }
    }

    private mostrarExito(msg: string): void {
        this.exito.set(msg);
        this.error.set(null);
        setTimeout(() => this.exito.set(null), 3000);
    }

    // ── Cursos ────────────────────────────────────────────────────────────────

    async crearCurso(): Promise<void> {
        if (!this.nuevoCursoNombre.trim()) {
            this.error.set('El nombre del curso es obligatorio.');
            return;
        }
        this.error.set(null);
        try {
            const c = await this.api.createCurso(this.nuevoCursoNombre.trim());
            this.cursos.set([...this.cursos(), c]);
            this.nuevoCursoNombre = '';
            this.mostrarExito(`Curso "${c.nombre}" creado.`);
        } catch (e) { this.error.set((e as Error).message); }
    }

    // ── Asignaturas ───────────────────────────────────────────────────────────

    async crearAsignatura(): Promise<void> {
        if (!this.nuevaAsignaturaNombre.trim() || !this.nuevaAsignaturaCursoId) {
            this.error.set('Nombre y curso son obligatorios para la asignatura.');
            return;
        }
        this.error.set(null);
        try {
            const a = await this.api.createAsignatura(this.nuevaAsignaturaNombre.trim(), this.nuevaAsignaturaCursoId);
            this.asignaturas.set([...this.asignaturas(), a]);
            this.nuevaAsignaturaNombre = '';
            this.nuevaAsignaturaCursoId = null;
            this.mostrarExito(`Asignatura "${a.nombre}" creada.`);
        } catch (e) { this.error.set((e as Error).message); }
    }

    asignaturasVista(): AsignaturaItem[] {
        if (!this.filtroAsignaturasCursoId) return this.asignaturas();
        return this.asignaturas().filter(a => a.curso.id === Number(this.filtroAsignaturasCursoId));
    }

    // ── Profesores ────────────────────────────────────────────────────────────

    async crearProfesor(): Promise<void> {
        if (!this.nuevoProfesorNombre.trim() || !this.nuevoProfesorCorreo.trim() || !this.nuevoProfesorContrasena) {
            this.error.set('Nombre, correo y contraseña son obligatorios.');
            return;
        }
        this.error.set(null);
        try {
            const p = await this.api.createProfesor({
                nombre: this.nuevoProfesorNombre.trim(),
                correo: this.nuevoProfesorCorreo.trim(),
                contrasena: this.nuevoProfesorContrasena,
                esAdmin: this.nuevoProfesorEsAdmin
            });
            this.profesores.set([...this.profesores(), p]);
            this.nuevoProfesorNombre = '';
            this.nuevoProfesorCorreo = '';
            this.nuevoProfesorContrasena = '';
            this.nuevoProfesorEsAdmin = false;
            this.mostrarExito(`Profesor "${p.nombre}" creado.`);
        } catch (e) { this.error.set((e as Error).message); }
    }

    profesoresVista(): ProfesorListItem[] {
        if (!this.filtroProfesoresCursoId) return this.profesores();
        const cursoId = Number(this.filtroProfesoresCursoId);
        return this.profesores().filter(p => p.imparticiones.some(i => i.cursoId === cursoId));
    }

    // ── Estudiantes ───────────────────────────────────────────────────────────

    async crearEstudiante(): Promise<void> {
        if (!this.nuevoEstudianteNombre.trim() || !this.nuevoEstudianteCorreo.trim() ||
            !this.nuevoEstudianteContrasena || !this.nuevoEstudianteCursoId) {
            this.error.set('Todos los campos del estudiante son obligatorios.');
            return;
        }
        this.error.set(null);
        try {
            const e = await this.api.createEstudiante({
                nombre: this.nuevoEstudianteNombre.trim(),
                correo: this.nuevoEstudianteCorreo.trim(),
                contrasena: this.nuevoEstudianteContrasena,
                cursoId: this.nuevoEstudianteCursoId
            });
            this.estudiantes.set([...this.estudiantes(), e]);
            this.nuevoEstudianteNombre = '';
            this.nuevoEstudianteCorreo = '';
            this.nuevoEstudianteContrasena = '';
            this.nuevoEstudianteCursoId = null;
            this.mostrarExito(`Estudiante "${e.nombre}" creado.`);
        } catch (e) { this.error.set((e as Error).message); }
    }

    estudiantesVista(): EstudianteItem[] {
        if (!this.filtroEstudiantesCursoId) return this.estudiantes();
        return this.estudiantes().filter(e => e.cursoId === Number(this.filtroEstudiantesCursoId));
    }

    // ── Matrículas ────────────────────────────────────────────────────────────

    asignaturasFiltradas(): AsignaturaItem[] {
        if (!this.matriculaEstudianteId) return this.asignaturas();
        const est = this.estudiantes().find(e => e.id === Number(this.matriculaEstudianteId));
        if (!est) return this.asignaturas();
        return this.asignaturas().filter(a => a.curso.id === est.cursoId);
    }

    async matricularEstudiante(): Promise<void> {
        if (!this.matriculaEstudianteId || !this.matriculaAsignaturaId) {
            this.error.set('Selecciona un estudiante y una asignatura.');
            return;
        }
        this.error.set(null);
        try {
            await this.api.matricularEstudiante(
                Number(this.matriculaEstudianteId), Number(this.matriculaAsignaturaId)
            );
            this.matriculaAsignaturaId = null;
            this.mostrarExito('Matrícula realizada correctamente.');
            const asignaturas = await this.api.getAsignaturas();
            this.asignaturas.set(asignaturas);
        } catch (e) { this.error.set((e as Error).message); }
    }

    matriculasVista(): Array<{ estudianteId: number; estudiante: string; curso: string | null; asignaturas: string[] }> {
        const cursoFiltro = this.filtroMatriculasCursoId ? Number(this.filtroMatriculasCursoId) : null;
        const estudiantes = this.estudiantes().filter(e => !cursoFiltro || e.cursoId === cursoFiltro);

        return estudiantes
            .map(e => {
                const asignaturas = this.asignaturas()
                    .filter(a => a.alumnos.some(al => al.id === e.id))
                    .map(a => a.nombre)
                    .sort((a, b) => a.localeCompare(b));

                return {
                    estudianteId: e.id,
                    estudiante: e.nombre,
                    curso: e.curso,
                    asignaturas
                };
            })
            .sort((a, b) => a.estudiante.localeCompare(b.estudiante));
    }

    // ── Imparticiones ─────────────────────────────────────────────────────────

    asignaturasDeImparticion(): AsignaturaItem[] {
        if (!this.imparticionCursoId) return this.asignaturas();
        return this.asignaturas().filter(a => a.curso.id === Number(this.imparticionCursoId));
    }

    async asignarImparticion(): Promise<void> {
        if (!this.imparticionProfesorId || !this.imparticionAsignaturaId || !this.imparticionCursoId) {
            this.error.set('Selecciona profesor, asignatura y curso.');
            return;
        }
        this.error.set(null);
        try {
            await this.api.asignarImparticion(
                Number(this.imparticionProfesorId),
                Number(this.imparticionAsignaturaId),
                Number(this.imparticionCursoId)
            );
            this.imparticionAsignaturaId = null;
            this.mostrarExito('Impartición asignada correctamente.');
            const profesores = await this.api.getProfesores();
            this.profesores.set(profesores);
        } catch (e) { this.error.set((e as Error).message); }
    }

    imparticionesVista(): Array<{ profesorId: number; profesor: string; asignatura: string; cursoId: number; curso: string }> {
        const cursoFiltro = this.filtroImparticionesCursoId ? Number(this.filtroImparticionesCursoId) : null;

        return this.profesores()
            .flatMap(p => p.imparticiones.map(i => ({
                profesorId: p.id,
                profesor: p.nombre,
                asignatura: i.asignatura,
                cursoId: i.cursoId,
                curso: i.curso
            })))
            .filter(x => !cursoFiltro || x.cursoId === cursoFiltro)
            .sort((a, b) => a.curso.localeCompare(b.curso) || a.asignatura.localeCompare(b.asignatura) || a.profesor.localeCompare(b.profesor));
    }
}
