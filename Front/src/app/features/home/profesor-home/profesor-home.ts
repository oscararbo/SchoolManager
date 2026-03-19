import { CommonModule } from '@angular/common';
import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
    SchoolApiService,
    ProfesorPanel,
    AsignaturaAlumnos,
    AsignaturaAlumno,
    TareaResumen,
} from '../../../shared/services/school-api.service';

type ProfesorDetalleView = 'resumen' | 'calificar';

@Component({
    selector: 'app-profesor-home',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './profesor-home.html',
    styleUrl: './profesor-home.scss'
})
export class ProfesorHomeComponent implements OnInit {
    @Input({ required: true }) profesorId!: number;
    @Input({ required: true }) profesorNombre!: string;

    cargando = signal(true);
    error = signal<string | null>(null);

    panel = signal<ProfesorPanel | null>(null);
    detalleAsignatura = signal<AsignaturaAlumnos | null>(null);
    asignaturaActivaId = signal<number | null>(null);
    vistaDetalle = signal<ProfesorDetalleView>('resumen');

    // Task creation form
    nuevaTareaNombre = signal('');
    nuevaTareaTrimestre = signal<number>(1);
    creandoTarea = signal(false);

    // Active task for grading
    tareaActiva = signal<TareaResumen | null>(null);
    // Map estudianteId -> nota value being edited
    notaInputs = signal<Record<number, number | null>>({});
    guardandoNota = signal(false);
    alumnosExpandidos = new Set<number>();

    private api = inject(SchoolApiService);

    /** Carga el panel del profesor al inicializar el componente. */
    async ngOnInit(): Promise<void> {
        await this.cargarPanel();
    }

    /** Obtiene el panel del profesor (cursos y asignaturas) desde la API. */
    async cargarPanel(): Promise<void> {
        this.cargando.set(true);
        this.error.set(null);
        try {
            const data = await this.api.getPanelProfesor(this.profesorId);
            this.panel.set(data);
        } catch (e) {
            this.error.set((e as Error).message);
        } finally {
            this.cargando.set(false);
        }
    }

    /**
     * Selecciona una asignatura y carga sus alumnos con sus notas.
     * Resetea la vista al estado inicial (resumen, sin tarea activa).
     *
     * @param asignaturaId - Identificador de la asignatura a activar.
     */
    async cargarAlumnos(asignaturaId: number): Promise<void> {
        this.error.set(null);
        this.asignaturaActivaId.set(asignaturaId);
        this.vistaDetalle.set('resumen');
        this.tareaActiva.set(null);
        this.notaInputs.set({});
        this.alumnosExpandidos.clear();
        try {
            const data = await this.api.getAlumnosDeAsignatura(this.profesorId, asignaturaId);
            this.detalleAsignatura.set(data);
        } catch (e) {
            this.error.set((e as Error).message);
        }
    }

    /**
     * Activa el modo calificacion para una tarea y precarga las notas existentes de los alumnos.
     *
     * @param tarea - Tarea que se va a calificar.
     */
    seleccionarTarea(tarea: TareaResumen): void {
        this.vistaDetalle.set('calificar');
        this.tareaActiva.set(tarea);
        const detalle = this.detalleAsignatura();
        if (!detalle) return;
        const inputs: Record<number, number | null> = {};
        for (const alumno of detalle.alumnos) {
            const nota = alumno.notas.find(n => n.tareaId === tarea.tareaId);
            inputs[alumno.estudianteId] = nota?.valor ?? null;
        }
        this.notaInputs.set(inputs);
    }

    /** Cambia la vista al modo resumen por alumno. */
    mostrarResumen(): void {
        this.vistaDetalle.set('resumen');
    }

    /**
     * Cambia la vista al modo calificacion.
     * Si aun no hay tarea activa, selecciona automaticamente la primera disponible.
     */
    mostrarCalificacion(): void {
        this.vistaDetalle.set('calificar');
        const detalle = this.detalleAsignatura();
        if (!this.tareaActiva() && detalle?.tareas.length) {
            this.seleccionarTarea(detalle.tareas[0]);
        }
    }

    /**
     * Devuelve el valor introducido por el profesor para la nota de un alumno.
     *
     * @param estudianteId - Identificador del alumno.
     */
    getNotaInput(estudianteId: number): number | null {
        return this.notaInputs()[estudianteId] ?? null;
    }

    /**
     * Actualiza el valor del input de nota para un alumno concreto.
     *
     * @param estudianteId - Identificador del alumno.
     * @param valor - Nuevo valor a guardar (puede ser `null` para vaciar el campo).
     */
    setNotaInput(estudianteId: number, valor: number | null): void {
        this.notaInputs.update(m => ({ ...m, [estudianteId]: valor }));
    }

    /**
     * Valida y persiste la nota del alumno para la tarea activa.
     * Recarga los datos de la asignatura tras guardar correctamente.
     *
     * @param estudianteId - Identificador del alumno a calificar.
     */
    async guardarNota(estudianteId: number): Promise<void> {
        const tarea = this.tareaActiva();
        const asignaturaId = this.asignaturaActivaId();
        const valor = this.notaInputs()[estudianteId];

        if (!tarea || valor === null || valor === undefined || Number.isNaN(valor)) {
            this.error.set('Introduce una nota valida.');
            return;
        }
        if (valor < 0 || valor > 10) {
            this.error.set('La nota debe estar entre 0 y 10.');
            return;
        }

        this.guardandoNota.set(true);
        this.error.set(null);
        try {
            await this.api.ponerNota(this.profesorId, estudianteId, tarea.tareaId, valor);
            if (asignaturaId) await this.cargarAlumnos(asignaturaId);
            if (tarea) this.seleccionarTarea(tarea);
        } catch (e) {
            this.error.set((e as Error).message);
        } finally {
            this.guardandoNota.set(false);
        }
    }

    /**
     * Valida el formulario y crea una nueva tarea en la asignatura activa.
     * Resetea el formulario y recarga los alumnos tras la creacion exitosa.
     */
    async crearTarea(): Promise<void> {
        const nombre = this.nuevaTareaNombre().trim();
        const trimestre = this.nuevaTareaTrimestre();
        const asignaturaId = this.asignaturaActivaId();

        if (!nombre || !asignaturaId) {
            this.error.set('Rellena el nombre de la tarea.');
            return;
        }

        this.creandoTarea.set(true);
        this.error.set(null);
        try {
            await this.api.crearTarea(this.profesorId, nombre, trimestre, asignaturaId);
            this.nuevaTareaNombre.set('');
            this.nuevaTareaTrimestre.set(1);
            await this.cargarAlumnos(asignaturaId);
        } catch (e) {
            this.error.set((e as Error).message);
        } finally {
            this.creandoTarea.set(false);
        }
    }

    /**
     * Devuelve la nota de un alumno para una tarea concreta.
     *
     * @param alumno - Alumno del que consultar la nota.
     * @param tareaId - Identificador de la tarea.
     * @returns Valor de la nota o `null` si no esta calificada.
     */
    notaDeAlumno(alumno: AsignaturaAlumno, tareaId: number): number | null {
        return alumno.notas.find(n => n.tareaId === tareaId)?.valor ?? null;
    }

    /**
     * Formatea un valor numerico de nota para la vista.
     * Devuelve `'-'` si la nota no esta calificada.
     *
     * @param n - Valor numerico o `null`.
     */
    formatNota(n: number | null): string {
        return n !== null ? n.toFixed(2) : '-';
    }

    /**
     * Alterna el estado expandido/contraido del panel de detalle de un alumno.
     *
     * @param alumnoId - Identificador del alumno.
     */
    toggleAlumno(alumnoId: number): void {
        if (this.alumnosExpandidos.has(alumnoId)) {
            this.alumnosExpandidos.delete(alumnoId);
            return;
        }

        this.alumnosExpandidos.add(alumnoId);
    }

    /**
     * Indica si el panel de detalle de un alumno esta actualmente expandido.
     *
     * @param alumnoId - Identificador del alumno.
     */
    alumnoExpandido(alumnoId: number): boolean {
        return this.alumnosExpandidos.has(alumnoId);
    }

    /**
     * Obtiene las tareas del trimestre indicado con la nota actual del alumno.
     *
     * @param alumno - Alumno del que obtener las notas.
     * @param trimestre - Numero de trimestre (1, 2 o 3).
     * @returns Lista de tareas con el valor de nota del alumno para cada una.
     */
    tareasPorTrimestre(alumno: AsignaturaAlumno, trimestre: number): Array<{ tareaId: number; nombre: string; valor: number | null }> {
        const detalle = this.detalleAsignatura();
        if (!detalle) {
            return [];
        }

        return detalle.tareas
            .filter(tarea => tarea.trimestre === trimestre)
            .map(tarea => ({
                tareaId: tarea.tareaId,
                nombre: tarea.nombre,
                valor: this.notaDeAlumno(alumno, tarea.tareaId)
            }));
    }

    /**
     * Indica si la asignatura activa tiene tareas asignadas al trimestre dado.
     *
     * @param trimestre - Numero de trimestre (1, 2 o 3).
     */
    tieneTareasEnTrimestre(trimestre: number): boolean {
        return this.detalleAsignatura()?.tareas.some(tarea => tarea.trimestre === trimestre) ?? false;
    }
}
