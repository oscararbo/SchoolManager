import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { AlumnoMateriaDetalle, AlumnoPanelResumen, TareaSubmision } from '../../../../../shared/services/school-api.service';

@Component({
    selector: 'app-alumno-tareas-tab',
    standalone: true,
    imports: [],
    templateUrl: './alumno-tareas-tab.component.html',
    styleUrl: './alumno-tareas-tab.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AlumnoTareasTabComponent {
    @Input({ required: true }) panel!: AlumnoPanelResumen | null;
    @Input({ required: true }) hayTareasPendientes!: () => boolean;
    @Input({ required: true }) materiaDetalleCargando!: (asignaturaId: number) => boolean;
    @Input({ required: true }) materiaDetalle!: (asignaturaId: number) => AlumnoMateriaDetalle | null;
    @Input({ required: true }) tieneTareasPendientesEnTrimestre!: (asignaturaId: number, trimestre: number) => boolean;
    @Input({ required: true }) tareasPendientesPorTrimestre!: (asignaturaId: number, trimestre: number) => Array<{ tareaId: number; nombre: string; descripcion?: string | null; valor: number | null }>;
    @Input({ required: true }) tareaExpandida!: number | null;
    @Input({ required: true }) formatNota!: (valor: number | null | undefined) => string;
    @Input({ required: true }) submisionError!: string | null;
    @Input({ required: true }) submisionCargando!: (tareaId: number) => boolean;
    @Input({ required: true }) tareasMarcadasHechas!: Record<number, boolean>;
    @Input({ required: true }) getSubmisiones!: (tareaId: number) => TareaSubmision[];
    @Input({ required: true }) formatBytes!: (bytes: number) => string;
    @Input({ required: true }) subiendoArchivoTarea!: (tareaId: number) => boolean;

    @Output() readonly cambiarTarea = new EventEmitter<number | null>();
    @Output() readonly eliminarSubmision = new EventEmitter<{ tareaId: number; submisionId: number }>();
    @Output() readonly marcarTareaComoHecha = new EventEmitter<number>();
    @Output() readonly archivoSeleccionado = new EventEmitter<{ tareaId: number; event: Event }>();

    onCambiarTarea(tareaId: number): void {
        this.cambiarTarea.emit(tareaId);
    }

    onEliminarSubmision(tareaId: number, submisionId: number): void {
        this.eliminarSubmision.emit({ tareaId, submisionId });
    }

    onMarcarTareaComoHecha(tareaId: number): void {
        this.marcarTareaComoHecha.emit(tareaId);
    }

    onArchivoSeleccionado(tareaId: number, event: Event): void {
        this.archivoSeleccionado.emit({ tareaId, event });
    }
}
