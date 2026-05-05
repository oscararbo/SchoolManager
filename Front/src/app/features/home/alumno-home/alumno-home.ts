import { ChangeDetectionStrategy, Component, Input, OnInit, inject, signal } from '@angular/core';
import {
    SchoolApiService,
    AlumnoPanelResumen,
    AlumnoMateriaResumen,
    AlumnoMateriaDetalle
} from '../../../shared/services/school-api.service';

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

    private materiaDetalles = signal<Record<number, AlumnoMateriaDetalle>>({});
    private materiaDetallesLoading = signal<Record<number, boolean>>({});
    private materiaTareasByTrimestre = signal<Record<number, Record<number, Array<{ tareaId: number; nombre: string; valor: number | null }>>>>({});
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

    tareasPorTrimestre(asignaturaId: number, trimestre: number): Array<{ tareaId: number; nombre: string; valor: number | null }> {
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

    private buildTareasByTrimestre(detalle: AlumnoMateriaDetalle): Record<number, Array<{ tareaId: number; nombre: string; valor: number | null }>> {
        return detalle.notas.reduce((acc, task) => {
            const current = acc[task.trimestre] ?? [];
            current.push({ tareaId: task.tareaId, nombre: task.nombre, valor: task.valor });
            acc[task.trimestre] = current;
            return acc;
        }, {} as Record<number, Array<{ tareaId: number; nombre: string; valor: number | null }>>);
    }
}

