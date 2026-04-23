import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { AdminImparticionListItem, AsignaturaItem, CursoItem, ProfesorListItem } from '../../../../../../../shared/services/school-api.service';
import { FormsModule } from '@angular/forms';

@Component({
    selector: 'app-admin-imparticiones-tab',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './admin-imparticiones-tab.component.html'
})
export class AdminImparticionesTabComponent {
    @Input() cargandoFormularioImparticiones = false;
    @Input() cargandoListaImparticiones = false;
    @Input() cargandoAsignarImparticion = false;
    @Input() cargandoEliminarImparticion = false;

    @Input() profesores: ProfesorListItem[] = [];
    @Input() cursos: CursoItem[] = [];
    @Input() asignaturasDeImparticion: AsignaturaItem[] = [];
    @Input() imparticionesVista: AdminImparticionListItem[] = [];

    @Input() imparticionProfesorId: number | null = null;
    @Input() imparticionCursoId: number | null = null;
    @Input() imparticionAsignaturaId: number | null = null;
    @Input() filtroImparticionesCursoId: number | null = null;

    @Output() imparticionProfesorIdChange = new EventEmitter<number | null>();
    @Output() imparticionCursoIdChange = new EventEmitter<number | null>();
    @Output() imparticionAsignaturaIdChange = new EventEmitter<number | null>();
    @Output() filtroImparticionesCursoIdChange = new EventEmitter<number | null>();

    @Output() asignarImparticion = new EventEmitter<void>();
    @Output() eliminarImparticion = new EventEmitter<{ profesorId: number; asignaturaId: number; cursoId: number; asignaturaNombre: string }>();
}
