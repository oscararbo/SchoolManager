import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { AbstractControl, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CursoItem, ProfesorListItem } from '../../../../../../../shared/services/school-api.service';
import { TextInputComponent } from '../../../../../../../shared/components/text-input/text-input.component';

@Component({
    selector: 'app-admin-profesores-tab',
    standalone: true,
    imports: [CommonModule, FormsModule, ReactiveFormsModule, TextInputComponent],
    templateUrl: './admin-profesores-tab.component.html'
})
export class AdminProfesoresTabComponent {
    @Input() editandoProfesorId: number | null = null;
    @Input() editProfesorForm!: FormGroup;
    @Input() profesorForm!: FormGroup;
    @Input() profesoresVista: ProfesorListItem[] = [];
    @Input() cursos: CursoItem[] = [];
    @Input() filtroProfesoresCursoId: number | null = null;
    @Input() busquedaProfesores = '';
    @Input() cargandoListaProfesores = false;
    @Input() cargandoCrearProfesor = false;
    @Input() cargandoGuardarProfesor = false;
    @Input() cargandoEliminarProfesor = false;
    @Input() controlErrorMessage: (control: AbstractControl | null) => string | null = () => null;

    @Output() busquedaProfesoresChange = new EventEmitter<string>();
    @Output() filtroProfesoresCursoIdChange = new EventEmitter<number | null>();
    @Output() iniciarEditarProfesor = new EventEmitter<ProfesorListItem>();
    @Output() eliminarProfesor = new EventEmitter<{ id: number; nombre: string }>();
    @Output() guardarProfesor = new EventEmitter<void>();
    @Output() cancelarEditarProfesor = new EventEmitter<void>();
    @Output() crearProfesor = new EventEmitter<void>();
}
