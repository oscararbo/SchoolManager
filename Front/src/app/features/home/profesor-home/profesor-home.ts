import { CommonModule } from '@angular/common';
import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
    SchoolApiService,
    ProfesorPanel,
    AsignaturaAlumnosResumen,
    AsignaturaAlumnoResumen,
    AsignaturaAlumno,
    TareaResumen,
} from '../../../shared/services/school-api.service';
import { ToastService } from '../../../core/services/toast.service';

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
    detalleAsignatura = signal<AsignaturaAlumnosResumen | null>(null);
    asignaturaActivaId = signal<number | null>(null);
    vistaDetalle = signal<ProfesorDetalleView>('resumen');

    nuevaTareaNombre = signal('');
    nuevaTareaTrimestre = signal<number>(1);
    creandoTarea = signal(false);

    tareaActiva = signal<TareaResumen | null>(null);
    notaInputs = signal<Record<number, number | null>>({});
    guardandoNota = signal(false);
    alumnosExpandidos = new Set<number>();

    private alumnoDetalles = signal<Record<number, AsignaturaAlumno>>({});
    private alumnoDetallesLoading = signal<Record<number, boolean>>({});
    private calificacionesActuales = signal<Record<number, number | null>>({});

    private api = inject(SchoolApiService);
    private toast = inject(ToastService);

    async ngOnInit(): Promise<void> {
        await this.cargarPanel();
    }

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

    async cargarAlumnos(asignaturaId: number): Promise<void> {
        this.error.set(null);
        this.asignaturaActivaId.set(asignaturaId);
        this.vistaDetalle.set('resumen');
        this.tareaActiva.set(null);
        this.notaInputs.set({});
        this.calificacionesActuales.set({});
        this.alumnoDetalles.set({});
        this.alumnoDetallesLoading.set({});
        this.alumnosExpandidos.clear();

        try {
            const data = await this.api.getAlumnosResumenDeAsignatura(this.profesorId, asignaturaId);
            this.detalleAsignatura.set(data);
        } catch (e) {
            this.error.set((e as Error).message);
        }
    }

    async seleccionarTarea(tarea: TareaResumen): Promise<void> {
        this.vistaDetalle.set('calificar');
        this.tareaActiva.set(tarea);

        const asignaturaId = this.asignaturaActivaId();
        if (!asignaturaId) {
            return;
        }

        try {
            const result = await this.api.getCalificacionesDeTarea(this.profesorId, asignaturaId, tarea.tareaId);
            const inputs: Record<number, number | null> = {};
            const actuales: Record<number, number | null> = {};

            for (const c of result.calificaciones) {
                inputs[c.estudianteId] = c.valor;
                actuales[c.estudianteId] = c.valor;
            }

            this.notaInputs.set(inputs);
            this.calificacionesActuales.set(actuales);
        } catch (e) {
            this.error.set((e as Error).message);
        }
    }

    mostrarResumen(): void {
        this.vistaDetalle.set('resumen');
    }

    async mostrarCalificacion(): Promise<void> {
        this.vistaDetalle.set('calificar');
        const detalle = this.detalleAsignatura();
        if (!this.tareaActiva() && detalle?.tareas.length) {
            await this.seleccionarTarea(detalle.tareas[0]);
        }
    }

    getNotaInput(estudianteId: number): number | null {
        return this.notaInputs()[estudianteId] ?? null;
    }

    setNotaInput(estudianteId: number, valor: number | null): void {
        this.notaInputs.update(m => ({ ...m, [estudianteId]: valor }));
    }

    async guardarNota(estudianteId: number): Promise<void> {
        const tarea = this.tareaActiva();
        const valor = this.notaInputs()[estudianteId];

        if (!tarea || valor === null || valor === undefined || Number.isNaN(valor)) {
            this.error.set('Introduce una nota valida.');
            return;
        }
        if (valor < 0 || valor > 10) {
            this.error.set('La nota debe estar entre 0 y 10.');
            return;
        }

        const valorActual = this.calificacionesActuales()[estudianteId] ?? null;
        if (valorActual !== null && valorActual === valor) {
            this.toast.show('La nota no ha cambiado.', 'info');
            return;
        }

        this.guardandoNota.set(true);
        this.error.set(null);
        try {
            await this.api.ponerNota(this.profesorId, estudianteId, tarea.tareaId, valor);

            this.calificacionesActuales.update(m => ({ ...m, [estudianteId]: valor }));
            this.notaInputs.update(m => ({ ...m, [estudianteId]: valor }));

            const asignaturaId = this.asignaturaActivaId();
            if (asignaturaId) {
                const detalle = await this.api.getAlumnoDetalleDeAsignatura(this.profesorId, asignaturaId, estudianteId);
                this.alumnoDetalles.update(m => ({ ...m, [estudianteId]: detalle }));
                this.actualizarResumenDesdeDetalle(detalle);
            }
        } catch (e) {
            this.error.set((e as Error).message);
        } finally {
            this.guardandoNota.set(false);
        }
    }

    async crearTarea(): Promise<void> {
        const nombre = this.nuevaTareaNombre().trim();
        const trimestre = this.nuevaTareaTrimestre();
        const asignaturaId = this.asignaturaActivaId();

        if (!nombre || !asignaturaId) {
            this.error.set('Rellena el nombre de la tarea.');
            return;
        }

        const tareasDuplicadas = this.detalleAsignatura()?.tareas.filter(t =>
            t.nombre === nombre && t.trimestre === trimestre
        );

        if (tareasDuplicadas && tareasDuplicadas.length > 0) {
            this.toast.show(`Ya existe una tarea con el nombre "${nombre}" en este trimestre.`, 'warning');
            return;
        }

        this.creandoTarea.set(true);
        this.error.set(null);
        try {
            const tareaCreada = await this.api.crearTarea(this.profesorId, nombre, trimestre, asignaturaId);

            this.detalleAsignatura.update(detalle => {
                if (!detalle) {
                    return detalle;
                }

                const tareas = [...detalle.tareas, {
                    tareaId: tareaCreada.id,
                    nombre: tareaCreada.nombre,
                    trimestre: tareaCreada.trimestre
                }].sort((a, b) => a.trimestre - b.trimestre || a.nombre.localeCompare(b.nombre));

                return { ...detalle, tareas };
            });

            this.nuevaTareaNombre.set('');
            this.nuevaTareaTrimestre.set(1);
            this.toast.show(`Tarea "${nombre}" creada.`, 'success');
        } catch (e) {
            this.error.set((e as Error).message);
        } finally {
            this.creandoTarea.set(false);
        }
    }

    formatNota(n: number | null): string {
        return n !== null ? n.toFixed(2) : '-';
    }

    async toggleAlumno(alumnoId: number): Promise<void> {
        if (this.alumnosExpandidos.has(alumnoId)) {
            this.alumnosExpandidos.delete(alumnoId);
            return;
        }

        this.alumnosExpandidos.add(alumnoId);
        await this.cargarDetalleAlumno(alumnoId);
    }

    alumnoExpandido(alumnoId: number): boolean {
        return this.alumnosExpandidos.has(alumnoId);
    }

    alumnoDetalle(alumnoId: number): AsignaturaAlumno | null {
        return this.alumnoDetalles()[alumnoId] ?? null;
    }

    alumnoDetalleCargando(alumnoId: number): boolean {
        return this.alumnoDetallesLoading()[alumnoId] ?? false;
    }

    tareasPorTrimestre(alumno: AsignaturaAlumnoResumen, trimestre: number): Array<{ tareaId: number; nombre: string; valor: number | null }> {
        const detalle = this.detalleAsignatura();
        const detalleAlumno = this.alumnoDetalle(alumno.estudianteId);
        if (!detalle || !detalleAlumno) {
            return [];
        }

        return detalle.tareas
            .filter(t => t.trimestre === trimestre)
            .map(t => ({
                tareaId: t.tareaId,
                nombre: t.nombre,
                valor: detalleAlumno.notas.find(n => n.tareaId === t.tareaId)?.valor ?? null
            }));
    }

    tieneTareasEnTrimestre(trimestre: number): boolean {
        return this.detalleAsignatura()?.tareas.some(tarea => tarea.trimestre === trimestre) ?? false;
    }

    private async cargarDetalleAlumno(estudianteId: number): Promise<void> {
        if (this.alumnoDetalles()[estudianteId]) {
            return;
        }

        const asignaturaId = this.asignaturaActivaId();
        if (!asignaturaId) {
            return;
        }

        this.alumnoDetallesLoading.update(m => ({ ...m, [estudianteId]: true }));
        try {
            const detalle = await this.api.getAlumnoDetalleDeAsignatura(this.profesorId, asignaturaId, estudianteId);
            this.alumnoDetalles.update(m => ({ ...m, [estudianteId]: detalle }));
            this.actualizarResumenDesdeDetalle(detalle);
        } catch (e) {
            this.error.set((e as Error).message);
        } finally {
            this.alumnoDetallesLoading.update(m => ({ ...m, [estudianteId]: false }));
        }
    }

    private actualizarResumenDesdeDetalle(detalle: AsignaturaAlumno): void {
        this.detalleAsignatura.update(actual => {
            if (!actual) {
                return actual;
            }

            return {
                ...actual,
                alumnos: actual.alumnos.map(a =>
                    a.estudianteId === detalle.estudianteId
                        ? { ...a, medias: detalle.medias, notaFinal: detalle.notaFinal }
                        : a
                )
            };
        });
    }
}
