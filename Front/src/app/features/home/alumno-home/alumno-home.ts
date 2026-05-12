import { ChangeDetectionStrategy, Component, Input, OnInit, inject, signal } from '@angular/core';
import {
    SchoolApiService,
    AlumnoPanelResumen,
    AlumnoMateriaResumen,
    AlumnoMateriaDetalle,
    TareaSubmision
} from '../../../shared/services/school-api.service';

type AlumnoSection = 'notas' | 'tareas' | 'horarios' | 'incidencias';

@Component({
    selector: 'app-alumno-home',
    standalone: true,
    imports: [],
    templateUrl: './alumno-home.html',
    styleUrl: './alumno-home.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AlumnoHomeComponent implements OnInit {
    @Input({ required: true }) estudianteId!: number;
    @Input({ required: true }) nombre!: string;

    cargando = signal(true);
    error = signal<string | null>(null);
    panel = signal<AlumnoPanelResumen | null>(null);
    seccionActiva = signal<AlumnoSection>('notas');
    tareaExpandida = signal<number | null>(null);
    submisionesMap = signal<Record<number, TareaSubmision[]>>({});
    submisionesCargando = signal<Record<number, boolean>>({});
    subiendoArchivo = signal<Record<number, boolean>>({});
    submisionError = signal<string | null>(null);
    tareasMarcadasHechas = signal<Record<number, boolean>>({});

    private materiaDetalles = signal<Record<number, AlumnoMateriaDetalle>>({});
    private materiaDetallesLoading = signal<Record<number, boolean>>({});
    private materiaTareasByTrimestre = signal<Record<number, Record<number, Array<{ tareaId: number; nombre: string; descripcion?: string | null; valor: number | null }>>>>({});
    expandidas = signal(new Set<number>());

    private api = inject(SchoolApiService);

    async ngOnInit(): Promise<void> {
        await this.cargarPanel();
    }

    async cargarPanel(): Promise<void> {
        this.cargando.set(true);
        this.error.set(null);
        try {
            const data = await this.api.getPanelAlumnoResumen(this.estudianteId);
            this.panel.set(data);
        } catch (e) {
            this.error.set((e as Error).message);
        } finally {
            this.cargando.set(false);
        }
    }

    cambiarSeccion(seccion: AlumnoSection): void {
        this.seccionActiva.set(seccion);
        this.tareaExpandida.set(null);
        if (seccion === 'tareas') {
            void this.precargarMateriasParaTareas();
        }
    }

    cambiarTarea(tareaId: number | null): void {
        if (this.tareaExpandida() === tareaId) {
            this.tareaExpandida.set(null);
        } else {
            this.tareaExpandida.set(tareaId);
            if (tareaId !== null && !this.submisionesMap()[tareaId]) {
                this.cargarSubmisiones(tareaId);
            }
        }
    }

    async cargarSubmisiones(tareaId: number): Promise<void> {
        this.submisionesCargando.update(m => ({ ...m, [tareaId]: true }));
        try {
            const lista = await this.api.getSubmisionesAlumno(this.estudianteId, tareaId);
            this.submisionesMap.update(m => ({ ...m, [tareaId]: lista }));
            if (lista.some(s => s.tamanoBytes === 0)) {
                this.tareasMarcadasHechas.update(m => ({ ...m, [tareaId]: true }));
            }
        } catch {
            // silencioso
        } finally {
            this.submisionesCargando.update(m => ({ ...m, [tareaId]: false }));
        }
    }

    async onArchivoSeleccionado(tareaId: number, event: Event): Promise<void> {
        const input = event.target as HTMLInputElement;
        const archivo = input.files?.[0];
        if (!archivo) return;

        this.submisionError.set(null);
        this.subiendoArchivo.update(m => ({ ...m, [tareaId]: true }));
        try {
            const saved = await this.api.subirSubmisionAlumno(this.estudianteId, tareaId, archivo);
            this.submisionesMap.update(m => ({ ...m, [tareaId]: [saved] }));
            input.value = '';
        } catch (e) {
            this.submisionError.set((e as Error).message);
        } finally {
            this.subiendoArchivo.update(m => ({ ...m, [tareaId]: false }));
        }
    }

    async eliminarSubmision(tareaId: number, submisionId: number): Promise<void> {
        this.submisionError.set(null);
        try {
            await this.api.deleteSubmisionAlumno(this.estudianteId, submisionId);
            this.submisionesMap.update(m => ({
                ...m,
                [tareaId]: (m[tareaId] ?? []).filter(s => s.id !== submisionId)
            }));
        } catch (e) {
            this.submisionError.set((e as Error).message);
        }
    }

    getSubmisiones(tareaId: number): TareaSubmision[] {
        return this.submisionesMap()[tareaId] ?? [];
    }

    submisionCargando(tareaId: number): boolean {
        return this.submisionesCargando()[tareaId] ?? false;
    }

    subiendoArchivoTarea(tareaId: number): boolean {
        return this.subiendoArchivo()[tareaId] ?? false;
    }

    esTareaPendiente(tareaId: number, valor: number | null): boolean {
        return valor === null && !this.tareasMarcadasHechas()[tareaId];
    }

    tareasPendientesPorTrimestre(asignaturaId: number, trimestre: number): Array<{ tareaId: number; nombre: string; descripcion?: string | null; valor: number | null }> {
        return this.tareasPorTrimestre(asignaturaId, trimestre)
            .filter(tarea => this.esTareaPendiente(tarea.tareaId, tarea.valor));
    }

    tieneTareasPendientesEnTrimestre(asignaturaId: number, trimestre: number): boolean {
        return this.tareasPendientesPorTrimestre(asignaturaId, trimestre).length > 0;
    }

    hayTareasPendientes(): boolean {
        const materias = this.panel()?.materias ?? [];
        for (const materia of materias) {
            for (const trimestre of [1, 2, 3]) {
                if (this.tieneTareasPendientesEnTrimestre(materia.asignaturaId, trimestre)) {
                    return true;
                }
            }
        }
        return false;
    }

    marcarTareaComoHecha(tareaId: number): void {
        this.tareasMarcadasHechas.update(m => ({ ...m, [tareaId]: true }));
        if (this.tareaExpandida() === tareaId) {
            this.tareaExpandida.set(null);
        }
        void this.api.marcarTareaHechaAlumno(this.estudianteId, tareaId)
            .then(saved => {
                this.submisionesMap.update(m => ({ ...m, [tareaId]: [saved] }));
            })
            .catch(() => {
                // Si falla, revertir el estado visual
                this.tareasMarcadasHechas.update(m => ({ ...m, [tareaId]: false }));
            });
    }

    formatBytes(bytes: number): string {
        if (bytes < 1024) return `${bytes} B`;
        if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
        return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
    }

    async toggleExpandir(asignaturaId: number): Promise<void> {
        if (this.expandidas().has(asignaturaId)) {
            this.expandidas.update(expanded => {
                const next = new Set(expanded);
                next.delete(asignaturaId);
                return next;
            });
            return;
        }

        this.expandidas.update(expanded => {
            const next = new Set(expanded);
            next.add(asignaturaId);
            return next;
        });
        await this.cargarMateriaDetalle(asignaturaId);
    }

    estaExpandida(asignaturaId: number): boolean {
        return this.expandidas().has(asignaturaId);
    }

    materiaDetalle(asignaturaId: number): AlumnoMateriaDetalle | null {
        return this.materiaDetalles()[asignaturaId] ?? null;
    }

    materiaDetalleCargando(asignaturaId: number): boolean {
        return this.materiaDetallesLoading()[asignaturaId] ?? false;
    }

    tareasPorTrimestre(asignaturaId: number, trimestre: number): Array<{ tareaId: number; nombre: string; descripcion?: string | null; valor: number | null }> {
        return this.materiaTareasByTrimestre()[asignaturaId]?.[trimestre] ?? [];
    }

    tieneTareasEnTrimestre(asignaturaId: number, trimestre: number): boolean {
        return (this.materiaTareasByTrimestre()[asignaturaId]?.[trimestre]?.length ?? 0) > 0;
    }

    formatNota(valor: number | null | undefined): string {
        return (valor === null || valor === undefined) ? '-' : valor.toFixed(2);
    }

    private async cargarMateriaDetalle(asignaturaId: number): Promise<void> {
        if (this.materiaDetalles()[asignaturaId]) return;

        this.materiaDetallesLoading.update(m => ({ ...m, [asignaturaId]: true }));
        try {
            const detalle = await this.api.getMateriaDetalle(this.estudianteId, asignaturaId);
            this.materiaDetalles.update(m => ({ ...m, [asignaturaId]: detalle }));
            this.materiaTareasByTrimestre.update(m => ({
                ...m,
                [asignaturaId]: this.buildTareasByTrimestre(detalle)
            }));
        } catch (e) {
            this.error.set((e as Error).message);
        } finally {
            this.materiaDetallesLoading.update(m => ({ ...m, [asignaturaId]: false }));
        }
    }

    private async precargarMateriasParaTareas(): Promise<void> {
        const materias = this.panel()?.materias ?? [];
        await Promise.all(
            materias
                .filter(m => !this.materiaDetalles()[m.asignaturaId])
                .map(m => this.cargarMateriaDetalle(m.asignaturaId))
        );
        // Pre-cargar submisiones de tareas pendientes para detectar las ya marcadas como hechas
        const tareaIds = materias.flatMap(m => {
            const detalle = this.materiaDetalles()[m.asignaturaId];
            if (!detalle) return [];
            return detalle.notas
                .filter(n => n.valor === null && !this.submisionesMap()[n.tareaId])
                .map(n => n.tareaId);
        });
        await Promise.all(tareaIds.map(tareaId => this.cargarSubmisiones(tareaId)));
    }

    private buildTareasByTrimestre(detalle: AlumnoMateriaDetalle): Record<number, Array<{ tareaId: number; nombre: string; descripcion?: string | null; valor: number | null }>> {
        return detalle.notas.reduce((acc, task) => {
            const current = acc[task.trimestre] ?? [];
            current.push({ tareaId: task.tareaId, nombre: task.nombre, descripcion: task.descripcion, valor: task.valor });
            acc[task.trimestre] = current;
            return acc;
        }, {} as Record<number, Array<{ tareaId: number; nombre: string; descripcion?: string | null; valor: number | null }>>);
    }
}

