import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { AlumnoMateriaDetalle, AlumnoPanelResumen } from '../../../../../shared/services/school-api.service';

@Component({
    selector: 'app-alumno-notas-tab',
    standalone: true,
    imports: [],
    templateUrl: './alumno-notas-tab.component.html',
    styleUrl: './alumno-notas-tab.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AlumnoNotasTabComponent {
    @Input({ required: true }) panel!: AlumnoPanelResumen | null;
    @Input({ required: true }) estaExpandida!: (asignaturaId: number) => boolean;
    @Input({ required: true }) materiaDetalleCargando!: (asignaturaId: number) => boolean;
    @Input({ required: true }) materiaDetalle!: (asignaturaId: number) => AlumnoMateriaDetalle | null;
    @Input({ required: true }) formatNota!: (valor: number | null | undefined) => string;
    @Input({ required: true }) tieneTareasEnTrimestre!: (asignaturaId: number, trimestre: number) => boolean;
    @Input({ required: true }) tareasPorTrimestre!: (asignaturaId: number, trimestre: number) => Array<{ tareaId: number; nombre: string; descripcion?: string | null; valor: number | null }>;

    @Output() readonly toggleExpandir = new EventEmitter<number>();

    onToggleExpandir(asignaturaId: number): void {
        this.toggleExpandir.emit(asignaturaId);
    }
}
