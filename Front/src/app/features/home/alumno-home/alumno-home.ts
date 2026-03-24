import { CommonModule } from '@angular/common';
import { Component, Input, OnInit, inject, signal } from '@angular/core';
import {
    SchoolApiService,
    AlumnoPanelResumen,
    AlumnoMateriaResumen,
    AlumnoMateriaDetalle
} from '../../../shared/services/school-api.service';

@Component({
    selector: 'app-alumno-home',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './alumno-home.html',
    styleUrl: './alumno-home.scss'
})
export class AlumnoHomeComponent implements OnInit {
    @Input({ required: true }) estudianteId!: number;
    @Input({ required: true }) nombre!: string;

    cargando = signal(true);
    error = signal<string | null>(null);
    panel = signal<AlumnoPanelResumen | null>(null);

    private materiaDetalles = signal<Record<number, AlumnoMateriaDetalle>>({});
    private materiaDetallesLoading = signal<Record<number, boolean>>({});
    expandidas = new Set<number>();

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
        if (this.expandidas.has(asignaturaId)) {
            this.expandidas.delete(asignaturaId);
            return;
        }
        this.expandidas.add(asignaturaId);
        await this.cargarMateriaDetalle(asignaturaId);
    }

    estaExpandida(asignaturaId: number): boolean {
        return this.expandidas.has(asignaturaId);
    }

    materiaDetalle(asignaturaId: number): AlumnoMateriaDetalle | null {
        return this.materiaDetalles()[asignaturaId] ?? null;
    }

    materiaDetalleCargando(asignaturaId: number): boolean {
        return this.materiaDetallesLoading()[asignaturaId] ?? false;
    }

    tareasPorTrimestre(asignaturaId: number, trimestre: number): Array<{ tareaId: number; nombre: string; valor: number | null }> {
        const detalle = this.materiaDetalle(asignaturaId);
        if (!detalle) return [];
        return detalle.notas.filter(t => t.trimestre === trimestre);
    }

    tieneTareasEnTrimestre(asignaturaId: number, trimestre: number): boolean {
        const detalle = this.materiaDetalle(asignaturaId);
        return detalle?.notas.some(t => t.trimestre === trimestre) ?? false;
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
        } catch (e) {
            this.error.set((e as Error).message);
        } finally {
            this.materiaDetallesLoading.update(m => ({ ...m, [asignaturaId]: false }));
        }
    }
}

