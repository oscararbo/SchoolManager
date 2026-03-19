import { Component, OnInit, computed, inject, signal, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
    SchoolApiService,
    CursoItem, AsignaturaItem, ProfesorListItem, EstudianteItem,
    UpdateProfesorData, UpdateEstudianteData, CsvImportResult
} from '../../../shared/services/school-api.service';
import { ToastService } from '../../../core/services/toast.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

type AdminTab = 'cursos' | 'asignaturas' | 'profesores' | 'estudiantes' | 'matriculas' | 'imparticiones' | 'importar';

@Component({
    selector: 'app-admin-home',
    standalone: true,
    imports: [CommonModule, FormsModule, ConfirmDialogComponent],
    templateUrl: './admin-home.html',
    styleUrl: './admin-home.scss'
})
export class AdminHomeComponent implements OnInit {
    @ViewChild(ConfirmDialogComponent) confirmDialog!: ConfirmDialogComponent;

    private api = inject(SchoolApiService);
    private toast = inject(ToastService);

    tabActiva = signal<AdminTab>('cursos');
    cargando = signal(false);

    cursos = signal<CursoItem[]>([]);
    asignaturas = signal<AsignaturaItem[]>([]);
    profesores = signal<ProfesorListItem[]>([]);
    estudiantes = signal<EstudianteItem[]>([]);

    nuevoCursoNombre = '';
    editandoCursoId: number | null = null;
    editCursoNombre = '';
    busquedaCursos = signal('');

    nuevaAsignaturaNombre = '';
    nuevaAsignaturaCursoId: number | null = null;
    filtroAsignaturasCursoId = signal<number | null>(null);
    editandoAsignaturaId: number | null = null;
    editAsignaturaNombre = '';
    editAsignaturaCursoId: number | null = null;
    busquedaAsignaturas = signal('');

    nuevoProfesorNombre = '';
    nuevoProfesorCorreo = '';
    nuevoProfesorContrasena = '';
    filtroProfesoresCursoId = signal<number | null>(null);
    editandoProfesorId: number | null = null;
    editProfesorNombre = '';
    editProfesorCorreo = '';
    editProfesorContrasena = '';
    busquedaProfesores = signal('');

    nuevoEstudianteNombre = '';
    nuevoEstudianteCorreo = '';
    nuevoEstudianteContrasena = '';
    nuevoEstudianteCursoId: number | null = null;
    filtroEstudiantesCursoId = signal<number | null>(null);
    editandoEstudianteId: number | null = null;
    editEstudianteNombre = '';
    editEstudianteCorreo = '';
    editEstudianteCursoId: number | null = null;
    editEstudianteContrasena = '';
    busquedaEstudiantes = signal('');

    matriculaEstudianteId = signal<number | null>(null);
    matriculaAsignaturaId = signal<number | null>(null);
    filtroMatriculasCursoId = signal<number | null>(null);

    imparticionProfesorId = signal<number | null>(null);
    imparticionAsignaturaId = signal<number | null>(null);
    imparticionCursoId = signal<number | null>(null);
    filtroImparticionesCursoId = signal<number | null>(null);

    // Vista de tareas para admin
    mostrarModalTareas = signal(false);
    tareasConNotas = signal<any[]>([]);
    tareaEnDetalle = signal<any | null>(null);
    cargandoTareas = signal(false);

    csvCursosFile: File | null = null;
    csvAsignaturasFile: File | null = null;
    csvProfesoresFile: File | null = null;
    csvEstudiantesFile: File | null = null;
    csvResultado: CsvImportResult | null = null;
    csvEntidadActual: string | null = null;
    csvCargando = false;

    async ngOnInit(): Promise<void> {
        await this.cargarTodo();
    }

    /**
     * Activa la pestana indicada y cancela cualquier edicion en curso.
     *
     * @param tab - Pestana a seleccionar.
     */
    cambiarTab(tab: AdminTab): void {
        this.tabActiva.set(tab);
        this.cancelarEdicion();
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
            this.mostrarError(e, 'No se pudieron cargar los datos iniciales.');
        } finally {
            this.cargando.set(false);
        }
    }

    /**
     * Muestra de forma consistente los errores de operaciones CRUD en el panel de admin.
     *
     * @param error - Error capturado en la operacion.
     * @param fallback - Mensaje de respaldo cuando no hay detalle disponible.
     */
    private mostrarError(error: unknown, fallback = 'No se pudo completar la operacion.'): void {
        const message = error instanceof Error ? error.message : fallback;
        this.toast.show(message || fallback, 'error');
    }

    /** Cancela cualquier edicion activa en todas las entidades. */
    cancelarEdicion(): void {
        this.editandoCursoId = null;
        this.editandoAsignaturaId = null;
        this.editandoProfesorId = null;
        this.editandoEstudianteId = null;
    }

    /**
     * Devuelve la lista de cursos filtrada por el texto de busqueda.
     *
     * @returns Cursos que coinciden con el filtro actual.
     */
    cursosVista = computed<CursoItem[]>(() => {
        const q = this.busquedaCursos().trim().toLowerCase();
        const cursos = this.cursos();
        return q ? cursos.filter(c => c.nombre.toLowerCase().includes(q)) : cursos;
    });

    /** Valida el formulario y crea un nuevo curso. Actualiza la lista local si tiene exito. */
    async crearCurso(): Promise<void> {
        if (!this.nuevoCursoNombre.trim()) { this.toast.show('El nombre del curso es obligatorio.', 'warning'); return; }
        try {
            const c = await this.api.createCurso(this.nuevoCursoNombre.trim());
            this.cursos.set([...this.cursos(), c]);
            this.nuevoCursoNombre = '';
            this.toast.show(`Curso "${c.nombre}" creado.`, 'success');
        } catch (e) { this.mostrarError(e); }
    }

    /**
     * Activa el modo edicion para el curso indicado.
     *
     * @param c - Curso que se va a editar.
     */
    iniciarEditarCurso(c: CursoItem): void {
        this.editandoCursoId = c.id;
        this.editCursoNombre = c.nombre;
    }

    /** Guarda los cambios del curso en edicion y actualiza la lista local. */
    async guardarCurso(): Promise<void> {
        if (!this.editandoCursoId || !this.editCursoNombre.trim()) return;
        try {
            const updated = await this.api.updateCurso(this.editandoCursoId, this.editCursoNombre.trim());
            this.cursos.update(list => list.map(c => c.id === updated.id ? updated : c));
            this.editandoCursoId = null;
            this.toast.show('Curso actualizado.', 'success');
        } catch (e) { this.mostrarError(e); }
    }

    /**
     * Pide confirmacion y elimina el curso con sus dependencias.
     *
     * @param id - Identificador del curso.
     * @param nombre - Nombre del curso (para el dialogo de confirmacion).
     */
    async eliminarCurso(id: number, nombre: string): Promise<void> {
        const confirmado = await this.confirmDialog.show(
            'Eliminar curso',
            `¿Eliminar el curso "${nombre}"? Esta accion no se puede deshacer.`
        );
        if (!confirmado) return;
        try {
            await this.api.deleteCurso(id);
            this.cursos.update(list => list.filter(c => c.id !== id));
            this.toast.show(`Curso "${nombre}" eliminado.`, 'success');
        } catch (e) { this.mostrarError(e); }
    }

    /**
     * Devuelve las asignaturas filtradas por curso y texto de busqueda.
     *
     * @returns Asignaturas que cumplen los filtros activos.
     */
    asignaturasVista = computed<AsignaturaItem[]>(() => {
        let result = this.asignaturas();
        const cursoId = this.filtroAsignaturasCursoId();
        if (cursoId)
            result = result.filter(a => a.curso.id === Number(cursoId));
        const q = this.busquedaAsignaturas().trim().toLowerCase();
        if (q) result = result.filter(a => a.nombre.toLowerCase().includes(q));
        return result;
    });

    /** Valida el formulario y crea una nueva asignatura vinculada al curso seleccionado. */
    async crearAsignatura(): Promise<void> {
        if (!this.nuevaAsignaturaNombre.trim() || !this.nuevaAsignaturaCursoId) {
            this.toast.show('Nombre y curso son obligatorios.', 'warning'); return;
        }
        try {
            const a = await this.api.createAsignatura(this.nuevaAsignaturaNombre.trim(), this.nuevaAsignaturaCursoId);
            this.asignaturas.set([...this.asignaturas(), a]);
            this.nuevaAsignaturaNombre = '';
            this.nuevaAsignaturaCursoId = null;
            this.toast.show(`Asignatura "${a.nombre}" creada.`, 'success');
        } catch (e) { this.mostrarError(e); }
    }

    /**
     * Activa el modo edicion para la asignatura indicada.
     *
     * @param a - Asignatura que se va a editar.
     */
    iniciarEditarAsignatura(a: AsignaturaItem): void {
        this.editandoAsignaturaId = a.id;
        this.editAsignaturaNombre = a.nombre;
        this.editAsignaturaCursoId = a.curso.id;
    }

    /** Guarda los cambios de la asignatura en edicion y actualiza la lista local. */
    async guardarAsignatura(): Promise<void> {
        if (!this.editandoAsignaturaId || !this.editAsignaturaNombre.trim() || !this.editAsignaturaCursoId) return;
        try {
            const updated = await this.api.updateAsignatura(
                this.editandoAsignaturaId, this.editAsignaturaNombre.trim(), this.editAsignaturaCursoId);
            this.asignaturas.update(list => list.map(a => a.id === updated.id ? updated : a));
            this.editandoAsignaturaId = null;
            this.toast.show('Asignatura actualizada.', 'success');
        } catch (e) { this.mostrarError(e); }
    }

    /**
     * Pide confirmacion y elimina la asignatura con sus tareas y notas.
     *
     * @param id - Identificador de la asignatura.
     * @param nombre - Nombre de la asignatura (para el dialogo de confirmacion).
     */
    async eliminarAsignatura(id: number, nombre: string): Promise<void> {
        const confirmado = await this.confirmDialog.show(
            'Eliminar asignatura',
            `¿Eliminar la asignatura "${nombre}"? Se eliminaran sus tareas y notas.`
        );
        if (!confirmado) return;
        try {
            await this.api.deleteAsignatura(id);
            this.asignaturas.update(list => list.filter(a => a.id !== id));
            this.toast.show(`Asignatura "${nombre}" eliminada.`, 'success');
        } catch (e) { this.mostrarError(e); }
    }

    /**
     * Devuelve los profesores filtrados por curso y texto de busqueda.
     *
     * @returns Profesores que cumplen los filtros activos.
     */
    profesoresVista = computed<ProfesorListItem[]>(() => {
        let result = this.profesores();
        const cursoIdFiltro = this.filtroProfesoresCursoId();
        if (cursoIdFiltro) {
            const cursoId = Number(cursoIdFiltro);
            result = result.filter(p => p.imparticiones.some(i => i.cursoId === cursoId));
        }
        const q = this.busquedaProfesores().trim().toLowerCase();
        if (q) result = result.filter(p => p.nombre.toLowerCase().includes(q) || p.correo.toLowerCase().includes(q));
        return result;
    });

    /** Valida el formulario y crea un nuevo profesor. Limpia el formulario si tiene exito. */
    async crearProfesor(): Promise<void> {
        if (!this.nuevoProfesorNombre.trim() || !this.nuevoProfesorCorreo.trim() || !this.nuevoProfesorContrasena) {
            this.toast.show('Nombre, correo y contrasena son obligatorios.', 'warning'); return;
        }
        try {
            const p = await this.api.createProfesor({
                nombre: this.nuevoProfesorNombre.trim(),
                correo: this.nuevoProfesorCorreo.trim(),
                contrasena: this.nuevoProfesorContrasena
            });
            this.profesores.set([...this.profesores(), p]);
            this.nuevoProfesorNombre = '';
            this.nuevoProfesorCorreo = '';
            this.nuevoProfesorContrasena = '';
            this.toast.show(`Profesor "${p.nombre}" creado.`, 'success');
        } catch (e) { this.mostrarError(e); }
    }

    /**
     * Activa el modo edicion para el profesor indicado.
     *
     * @param p - Profesor que se va a editar.
     */
    iniciarEditarProfesor(p: ProfesorListItem): void {
        this.editandoProfesorId = p.id;
        this.editProfesorNombre = p.nombre;
        this.editProfesorCorreo = p.correo;
        this.editProfesorContrasena = '';
    }

    /** Guarda los cambios del profesor en edicion y actualiza la lista local. */
    async guardarProfesor(): Promise<void> {
        if (!this.editandoProfesorId || !this.editProfesorNombre.trim() || !this.editProfesorCorreo.trim()) return;
        const data: UpdateProfesorData = {
            nombre: this.editProfesorNombre.trim(),
            correo: this.editProfesorCorreo.trim(),
            nuevaContrasena: this.editProfesorContrasena || undefined
        };
        try {
            const updated = await this.api.updateProfesor(this.editandoProfesorId, data);
            this.profesores.update(list => list.map(p => p.id === updated.id ? updated : p));
            this.editandoProfesorId = null;
            this.toast.show('Profesor actualizado.', 'success');
        } catch (e) { this.mostrarError(e); }
    }

    /**
     * Pide confirmacion y elimina el profesor con sus imparticiones y tareas.
     *
     * @param id - Identificador del profesor.
     * @param nombre - Nombre del profesor (para el dialogo de confirmacion).
     */
    async eliminarProfesor(id: number, nombre: string): Promise<void> {
        const confirmado = await this.confirmDialog.show(
            'Eliminar profesor',
            `¿Eliminar al profesor "${nombre}"? Se eliminaran sus imparticiones y tareas.`
        );
        if (!confirmado) return;
        try {
            await this.api.deleteProfesor(id);
            this.profesores.update(list => list.filter(p => p.id !== id));
            this.toast.show(`Profesor "${nombre}" eliminado.`, 'success');
        } catch (e) { this.mostrarError(e); }
    }

    /**
     * Devuelve los estudiantes filtrados por curso y texto de busqueda.
     *
     * @returns Estudiantes que cumplen los filtros activos.
     */
    estudiantesVista = computed<EstudianteItem[]>(() => {
        let result = this.estudiantes();
        const cursoId = this.filtroEstudiantesCursoId();
        if (cursoId)
            result = result.filter(e => e.cursoId === Number(cursoId));
        const q = this.busquedaEstudiantes().trim().toLowerCase();
        if (q) result = result.filter(e => e.nombre.toLowerCase().includes(q) || e.correo.toLowerCase().includes(q));
        return result;
    });

    /** Valida el formulario y crea un nuevo estudiante asignado al curso seleccionado. */
    async crearEstudiante(): Promise<void> {
        if (!this.nuevoEstudianteNombre.trim() || !this.nuevoEstudianteCorreo.trim() ||
            !this.nuevoEstudianteContrasena || !this.nuevoEstudianteCursoId) {
            this.toast.show('Todos los campos del estudiante son obligatorios.', 'warning'); return;
        }
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
            this.toast.show(`Estudiante "${e.nombre}" creado.`, 'success');
        } catch (e) { this.mostrarError(e); }
    }

    /**
     * Activa el modo edicion para el estudiante indicado.
     *
     * @param e - Estudiante que se va a editar.
     */
    iniciarEditarEstudiante(e: EstudianteItem): void {
        this.editandoEstudianteId = e.id;
        this.editEstudianteNombre = e.nombre;
        this.editEstudianteCorreo = e.correo;
        this.editEstudianteCursoId = e.cursoId;
        this.editEstudianteContrasena = '';
    }

    /** Guarda los cambios del estudiante en edicion y actualiza la lista local. */
    async guardarEstudiante(): Promise<void> {
        if (!this.editandoEstudianteId || !this.editEstudianteNombre.trim() ||
            !this.editEstudianteCorreo.trim() || !this.editEstudianteCursoId) return;
        const data: UpdateEstudianteData = {
            nombre: this.editEstudianteNombre.trim(),
            correo: this.editEstudianteCorreo.trim(),
            cursoId: this.editEstudianteCursoId,
            nuevaContrasena: this.editEstudianteContrasena || undefined
        };
        try {
            const updated = await this.api.updateEstudiante(this.editandoEstudianteId, data);
            this.estudiantes.update(list => list.map(e => e.id === updated.id ? updated : e));
            this.editandoEstudianteId = null;
            this.toast.show('Estudiante actualizado.', 'success');
        } catch (e) { this.mostrarError(e); }
    }

    /**
     * Pide confirmacion y elimina el estudiante con sus matriculas y notas.
     *
     * @param id - Identificador del estudiante.
     * @param nombre - Nombre del estudiante (para el dialogo de confirmacion).
     */
    async eliminarEstudiante(id: number, nombre: string): Promise<void> {
        const confirmado = await this.confirmDialog.show(
            'Eliminar estudiante',
            `¿Eliminar al estudiante "${nombre}"? Se eliminaran sus matriculas y notas.`
        );
        if (!confirmado) return;
        try {
            await this.api.deleteEstudiante(id);
            this.estudiantes.update(list => list.filter(e => e.id !== id));
            this.toast.show(`Estudiante "${nombre}" eliminado.`, 'success');
        } catch (e) { this.mostrarError(e); }
    }

    /**
     * Devuelve las asignaturas disponibles para matricular segun el curso del estudiante seleccionado.
     *
     * @returns Asignaturas del curso del estudiante, o todas si no hay estudiante seleccionado.
     */
    asignaturasFiltradas = computed<AsignaturaItem[]>(() => {
        const estudianteId = this.matriculaEstudianteId();
        if (!estudianteId) return this.asignaturas();
        const est = this.estudiantes().find(e => e.id === Number(estudianteId));
        if (!est) return this.asignaturas();
        return this.asignaturas().filter(a => a.curso.id === est.cursoId);
    });

    /** Matricula al estudiante seleccionado en la asignatura seleccionada. */
    async matricularEstudiante(): Promise<void> {
        if (!this.matriculaEstudianteId() || !this.matriculaAsignaturaId()) {
            this.toast.show('Selecciona un estudiante y una asignatura.', 'warning'); return;
        }

        const estudianteId = Number(this.matriculaEstudianteId());
        const asignaturaId = Number(this.matriculaAsignaturaId());
        try {
            await this.api.matricularEstudiante(estudianteId, asignaturaId);

            const estudiante = this.estudiantes().find(e => e.id === estudianteId);
            if (estudiante) {
                this.asignaturas.update(list => list.map(asignatura => {
                    if (asignatura.id !== asignaturaId) {
                        return asignatura;
                    }

                    if (asignatura.alumnos.some(alumno => alumno.id === estudianteId)) {
                        return asignatura;
                    }

                    return {
                        ...asignatura,
                        alumnos: [...asignatura.alumnos, { id: estudiante.id, nombre: estudiante.nombre }]
                    };
                }));
            }

            this.matriculaAsignaturaId.set(null);
            this.toast.show('Matricula realizada correctamente.', 'success');
        } catch (e) { this.mostrarError(e); }
    }

    /**
     * Devuelve el resumen de matriculas agrupado por estudiante y filtrado por curso.
     *
     * @returns Lista de estudiantes con sus asignaturas matriculadas.
     */
    matriculasVista = computed<Array<{ estudianteId: number; estudiante: string; curso: string | null; asignaturas: string[] }>>(() => {
        const cursoFiltroRaw = this.filtroMatriculasCursoId();
        const cursoFiltro = cursoFiltroRaw ? Number(cursoFiltroRaw) : null;
        const estudiantes = this.estudiantes().filter(e => !cursoFiltro || e.cursoId === cursoFiltro);
        return estudiantes
            .map(e => ({
                estudianteId: e.id,
                estudiante: e.nombre,
                curso: e.curso,
                asignaturas: this.asignaturas()
                    .filter(a => a.alumnos.some(al => al.id === e.id))
                    .map(a => a.nombre)
                    .sort((a, b) => a.localeCompare(b))
            }))
            .sort((a, b) => a.estudiante.localeCompare(b.estudiante));
    });

    /**
     * Devuelve las asignaturas disponibles para imparticion segun el curso seleccionado.
     *
     * @returns Asignaturas del curso seleccionado, o todas si no hay curso seleccionado.
     */
    asignaturasDeImparticion = computed<AsignaturaItem[]>(() => {
        const cursoId = this.imparticionCursoId();
        if (!cursoId) return this.asignaturas();
        return this.asignaturas().filter(a => a.curso.id === Number(cursoId));
    });

    /** Asigna al profesor seleccionado la imparticion de la asignatura y curso elegidos. */
    async asignarImparticion(): Promise<void> {
        if (!this.imparticionProfesorId() || !this.imparticionAsignaturaId() || !this.imparticionCursoId()) {
            this.toast.show('Selecciona profesor, asignatura y curso.', 'warning'); return;
        }

        const profesorId = Number(this.imparticionProfesorId());
        const asignaturaId = Number(this.imparticionAsignaturaId());
        const cursoId = Number(this.imparticionCursoId());
        try {
            await this.api.asignarImparticion(profesorId, asignaturaId, cursoId);

            const asignatura = this.asignaturas().find(a => a.id === asignaturaId);
            const curso = this.cursos().find(c => c.id === cursoId);

            if (asignatura && curso) {
                this.profesores.update(list => list.map(profesor => {
                    if (profesor.id !== profesorId) {
                        return profesor;
                    }

                    const yaExiste = profesor.imparticiones.some(i => i.asignaturaId === asignaturaId && i.cursoId === cursoId);
                    if (yaExiste) {
                        return profesor;
                    }

                    return {
                        ...profesor,
                        imparticiones: [
                            ...profesor.imparticiones,
                            {
                                asignaturaId,
                                asignatura: asignatura.nombre,
                                cursoId,
                                curso: curso.nombre
                            }
                        ]
                    };
                }));
            }

            this.imparticionAsignaturaId.set(null);
            this.toast.show('Imparticion asignada correctamente.', 'success');
        } catch (e) { this.mostrarError(e); }
    }

    /**
     * Devuelve el listado de imparticiones activas filtrado por curso.
     *
     * @returns Imparticiones que coinciden con el filtro de curso activo.
     */
    imparticionesVista = computed<Array<{ profesorId: number; profesor: string; asignatura: string; cursoId: number; curso: string }>>(() => {
        const cursoFiltroRaw = this.filtroImparticionesCursoId();
        const cursoFiltro = cursoFiltroRaw ? Number(cursoFiltroRaw) : null;
        return this.profesores()
            .flatMap(p => p.imparticiones.map(i => ({
                profesorId: p.id,
                profesor: p.nombre,
                asignatura: i.asignatura,
                cursoId: i.cursoId,
                curso: i.curso
            })))
            .filter(x => !cursoFiltro || x.cursoId === cursoFiltro)
            .sort((a, b) => a.curso.localeCompare(b.curso) || a.asignatura.localeCompare(b.asignatura));
    });

    /**
     * Captura el archivo seleccionado en el input de tipo file y lo almacena segun la entidad.
     *
     * @param event - Evento del input file.
     * @param entidad - Tipo de entidad: `'cursos'`, `'asignaturas'`, `'profesores'` o `'estudiantes'`.
     */
    onCsvFileChange(event: Event, entidad: string): void {
        const input = event.target as HTMLInputElement;
        const file = input.files?.[0] ?? null;
        if (entidad === 'cursos') this.csvCursosFile = file;
        else if (entidad === 'asignaturas') this.csvAsignaturasFile = file;
        else if (entidad === 'profesores') this.csvProfesoresFile = file;
        else this.csvEstudiantesFile = file;
    }

    /**
     * Sube el archivo CSV seleccionado para la entidad indicada y muestra el resultado.
     *
     * @param entidad - Tipo de entidad a importar: `'cursos'`, `'asignaturas'`, `'profesores'` o `'estudiantes'`.
     */
    async importarCsv(entidad: string): Promise<void> {
        const file = entidad === 'cursos' ? this.csvCursosFile
            : entidad === 'asignaturas' ? this.csvAsignaturasFile
            : entidad === 'profesores' ? this.csvProfesoresFile
            : this.csvEstudiantesFile;

        if (!file) { this.toast.show('Selecciona un archivo CSV.', 'warning'); return; }

        this.csvCargando = true;
        this.csvResultado = null;
        this.csvEntidadActual = entidad;
        try {
            const resultado = await this.api.importarCsv(entidad, file);
            this.csvResultado = resultado;
            this.toast.show(`Importacion de ${entidad}: ${resultado.creados} creados.`, 'success');
            await this.cargarTodo();
        } catch (e) {
            this.mostrarError(e, `No se pudo importar el CSV de ${entidad}.`);
        } finally {
            this.csvCargando = false;
        }
    }

    /**
     * Genera y descarga una plantilla CSV de ejemplo para la entidad indicada.
     *
     * @param entidad - Tipo de entidad: `'cursos'`, `'asignaturas'`, `'profesores'` o `'estudiantes'`.
     */
    descargarPlantilla(entidad: string): void {
        const plantillas: Record<string, string> = {
            cursos: 'nombre\n1\u00baA\n1\u00baB\n2\u00baA',
            asignaturas: 'nombre,cursoNombre\nMatematicas,1\u00baA\nLengua,1\u00baA\nCiencias,1\u00baB',
            profesores: 'nombre,correo,contrasena\nJuan Garcia,juan@colegio.es,Pass123',
            estudiantes: 'nombre,correo,contrasena,cursoNombre\nLucia Perez,lucia@colegio.es,Pass123,1\u00baA'
        };
        const csv = plantillas[entidad];
        const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `plantilla_${entidad}.csv`;
        link.click();
        URL.revokeObjectURL(url);
    }

    /** Abre el modal para ver las tareas de una asignatura con todas las notas. */
    async verTareasAsignatura(asignaturaId: number): Promise<void> {
        this.cargandoTareas.set(true);
        try {
            const tareas = await this.api.getTareasConNotas(asignaturaId);
            this.tareasConNotas.set(tareas);
            this.mostrarModalTareas.set(true);
        } catch (e) {
            this.mostrarError(e, 'No se pudieron cargar las tareas.');
        } finally {
            this.cargandoTareas.set(false);
        }
    }

    /** Selecciona una tarea para verla en detalle en el modal. */
    seleccionarTarea(tarea: any): void {
        this.tareaEnDetalle.set(tarea);
    }

    /** Cierra el modal de tareas. */
    cerrarModalTareas(): void {
        this.mostrarModalTareas.set(false);
        this.tareaEnDetalle.set(null);
        this.tareasConNotas.set([]);
    }
}

