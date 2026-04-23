import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { AbstractControl, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CursoItem, EstudianteItem } from '../../../../../../../shared/services/school-api.service';
import { DateInputComponent } from '../../../../../../../shared/components/date-input/date-input.component';
import { SelectInputComponent, SelectOption } from '../../../../../../../shared/components/select-input/select-input.component';
import { TextInputComponent } from '../../../../../../../shared/components/text-input/text-input.component';

@Component({
    selector: 'app-admin-estudiantes-tab',
    standalone: true,
    imports: [CommonModule, FormsModule, ReactiveFormsModule, DateInputComponent, SelectInputComponent, TextInputComponent],
    templateUrl: './admin-estudiantes-tab.component.html'
})
export class AdminEstudiantesTabComponent {
    @Input() editandoEstudianteId: number | null = null;
    @Input() editEstudianteForm!: FormGroup;
    @Input() estudianteForm!: FormGroup;
    @Input() estudiantesVista: EstudianteItem[] = [];
    @Input() cursos: CursoItem[] = [];
    @Input() cursoOptions: SelectOption[] = [];
    @Input() filtroEstudiantesCursoId: number | null = null;
    @Input() busquedaEstudiantes = '';
    @Input() cargandoFormularioEstudiantes = false;
    @Input() cargandoListaEstudiantes = false;
    @Input() cargandoCrearEstudiante = false;
    @Input() cargandoGuardarEstudiante = false;
    @Input() cargandoEliminarEstudiante = false;
    @Input() controlErrorMessage: (control: AbstractControl | null) => string | null = () => null;

    @Output() busquedaEstudiantesChange = new EventEmitter<string>();
    @Output() filtroEstudiantesCursoIdChange = new EventEmitter<number | null>();
    @Output() iniciarEditarEstudiante = new EventEmitter<EstudianteItem>();
    @Output() eliminarEstudiante = new EventEmitter<{ id: number; nombre: string }>();
    @Output() guardarEstudiante = new EventEmitter<void>();
    @Output() cancelarEditarEstudiante = new EventEmitter<void>();
    @Output() crearEstudiante = new EventEmitter<void>();
}
