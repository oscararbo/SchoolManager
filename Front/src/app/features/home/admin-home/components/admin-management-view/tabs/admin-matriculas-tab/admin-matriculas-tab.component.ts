import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { AdminMatriculaListItem, AsignaturaItem, CursoItem, EstudianteItem } from '../../../../../../../shared/services/school-api.service';
import { FormsModule } from '@angular/forms';

@Component({
    selector: 'app-admin-matriculas-tab',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './admin-matriculas-tab.component.html'
})
export class AdminMatriculasTabComponent {
    @Input() cargandoFormularioMatriculas = false;
    @Input() cargandoListaMatriculas = false;
    @Input() cargandoMatricularEstudiante = false;
    @Input() cargandoEliminarMatricula = false;

    @Input() estudiantes: EstudianteItem[] = [];
    @Input() asignaturasFiltradas: AsignaturaItem[] = [];
    @Input() matriculasVista: AdminMatriculaListItem[] = [];
    @Input() cursos: CursoItem[] = [];

    @Input() matriculaEstudianteId: number | null = null;
    @Input() matriculaAsignaturaId: number | null = null;
    @Input() filtroMatriculasCursoId: number | null = null;

    @Output() matriculaEstudianteIdChange = new EventEmitter<number | null>();
    @Output() matriculaAsignaturaIdChange = new EventEmitter<number | null>();
    @Output() filtroMatriculasCursoIdChange = new EventEmitter<number | null>();

    @Output() matricularEstudiante = new EventEmitter<void>();
    @Output() eliminarMatricula = new EventEmitter<{ estudianteId: number; asignaturaId: number; asignaturaNombre: string }>();
}
