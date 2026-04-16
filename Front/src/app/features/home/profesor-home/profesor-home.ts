import { CommonModule } from '@angular/common';
import { Component, DestroyRef, Input, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
    SchoolApiService,
    ProfesorPanel,
    ProfesorStats,
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
    stats = signal<ProfesorStats | null>(null);
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
    private destroyRef = inject(DestroyRef);
    private isDestroyed = false;

    constructor() {
        this.destroyRef.onDestroy(() => {
            this.isDestroyed = true;
        });
    }

    async ngOnInit(): Promise<void> {
        await this.cargarPanel();
    }

    async cargarPanel(): Promise<void> {
        this.cargando.set(true);
        this.error.set(null);
        try {
            const [panel, stats] = await Promise.all([
                this.api.getPanelProfesor(this.profesorId),
                this.api.getProfesorStats(this.profesorId)
            ]);
            if (this.isDestroyed) {
                return;
            }
            this.panel.set(panel);
            this.stats.set(stats);
        } catch (e) {
            if (!this.isDestroyed) {
                this.error.set((e as Error).message);
            }
        } finally {
            if (!this.isDestroyed) {
                this.cargando.set(false);
            }
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
            if (this.isDestroyed) {
                return;
            }
            this.detalleAsignatura.set(data);
        } catch (e) {
            if (!this.isDestroyed) {
                this.error.set((e as Error).message);
            }
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
            if (this.isDestroyed) {
                return;
            }
            const inputs: Record<number, number | null> = {};
            const actuales: Record<number, number | null> = {};

            for (const c of result.calificaciones) {
                inputs[c.estudianteId] = c.valor;
                actuales[c.estudianteId] = c.valor;
            }

            this.notaInputs.set(inputs);
            this.calificacionesActuales.set(actuales);
        } catch (e) {
            if (!this.isDestroyed) {
                this.error.set((e as Error).message);
            }
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
            await this.recargarStatsSilencioso();
        } catch (e) {
            if (!this.isDestroyed) {
                this.error.set((e as Error).message);
            }
        } finally {
            if (!this.isDestroyed) {
                this.guardandoNota.set(false);
            }
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
            await this.recargarStatsSilencioso();
            this.toast.show(`Tarea "${nombre}" creada.`, 'success');
        } catch (e) {
            if (!this.isDestroyed) {
                this.error.set((e as Error).message);
            }
        } finally {
            if (!this.isDestroyed) {
                this.creandoTarea.set(false);
            }
        }
    }

    formatNota(n: number | null): string {
        return n !== null ? n.toFixed(2) : '-';
    }

    get statsCards(): Array<{ label: string; value: string; helper: string }> {
        const stats = this.stats();
        if (!stats) {
            return [];
        }

        const totalAlumnos = stats.asignaturas.reduce((acc, item) => acc + item.totalAlumnos, 0);
        const tareas = stats.asignaturas.flatMap(item => item.porTarea);
        const tareasConMedia = tareas.filter(tarea => tarea.media !== null);
        const tareaExigente = [...tareasConMedia].sort((a, b) => (a.media ?? 99) - (b.media ?? 99))[0];
        const pendientes = tareas.reduce((acc, item) => acc + item.sinNota, 0);

        return [
            {
                label: 'Media global',
                value: this.formatNota(stats.mediaGlobal),
                helper: 'Promedio de las medias finales en tus asignaturas.'
            },
            {
                label: 'Carga de alumnos',
                value: String(totalAlumnos),
                helper: `${stats.asignaturas.length} asignaturas activas en tu panel.`
            },
            {
                label: 'Notas pendientes',
                value: String(pendientes),
                helper: 'Suma de entregas sin calificar en tus tareas actuales.'
            },
            {
                label: 'Tarea mas exigente',
                value: tareaExigente?.nombre ?? '-',
                helper: tareaExigente ? `Media ${this.formatNota(tareaExigente.media)}` : 'Todavia no hay tareas con notas.'
            }
        ];
    }

    get asignaturasStatsOrdenadas() {
        return [...(this.stats()?.asignaturas ?? [])]
            .sort((a, b) => b.suspensos - a.suspensos || (a.media ?? 99) - (b.media ?? 99));
    }

    get tareasClave() {
        return [...(this.stats()?.asignaturas ?? [])]
            .flatMap(asignatura => asignatura.porTarea.map(tarea => ({
                ...tarea,
                asignatura: asignatura.asignatura,
                curso: asignatura.curso
            })))
            .sort((a, b) => b.sinNota - a.sinNota || (a.media ?? 99) - (b.media ?? 99))
            .slice(0, 6);
    }

    porcentaje(valor: number, total: number): number {
        return total > 0 ? Math.round((valor / total) * 100) : 0;
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
            if (this.isDestroyed) {
                return;
            }
            this.alumnoDetalles.update(m => ({ ...m, [estudianteId]: detalle }));
            this.actualizarResumenDesdeDetalle(detalle);
        } catch (e) {
            if (!this.isDestroyed) {
                this.error.set((e as Error).message);
            }
        } finally {
            if (!this.isDestroyed) {
                this.alumnoDetallesLoading.update(m => ({ ...m, [estudianteId]: false }));
            }
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

    private async recargarStatsSilencioso(): Promise<void> {
        try {
            const stats = await this.api.getProfesorStats(this.profesorId);
            if (!this.isDestroyed) {
                this.stats.set(stats);
            }
        } catch {
            // Si falla esta recarga no debe bloquear la operacion principal.
        }
    }
}
