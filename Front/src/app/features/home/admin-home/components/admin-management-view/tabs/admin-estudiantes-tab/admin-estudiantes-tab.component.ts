import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { AbstractControl, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CursoItem, EstudianteItem } from '../../../../../../../shared/services/school-api.service';
import { DateInputComponent } from '../../../../../../../shared/components/date-input/date-input.component';
import { SelectInputComponent, SelectOption } from '../../../../../../../shared/components/select-input/select-input.component';
import { TextInputComponent } from '../../../../../../../shared/components/text-input/text-input.component';

@Component({
    selector: 'app-admin-estudiantes-tab',
    standalone: true,
    imports: [FormsModule, ReactiveFormsModule, DateInputComponent, SelectInputComponent, TextInputComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
    templateUrl: './admin-estudiantes-tab.component.html'
})
export class AdminEstudiantesTabComponent {
    @Input() editandoEstudianteId: number | null = null;
    @Input() editEstudianteForm!: FormGroup;
    @Input() estudianteForm!: FormGroup;
    @Input() puedeCrearEstudiante = true;
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
    @Input() page = 0;
    @Input() total = 0;
    @Input() pageSize = 50;

    @Output() busquedaEstudiantesChange = new EventEmitter<string>();
    @Output() filtroEstudiantesCursoIdChange = new EventEmitter<number | null>();
    @Output() iniciarEditarEstudiante = new EventEmitter<EstudianteItem>();
    @Output() eliminarEstudiante = new EventEmitter<{ id: number; nombre: string }>();
    @Output() guardarEstudiante = new EventEmitter<void>();
    @Output() cancelarEditarEstudiante = new EventEmitter<void>();
    @Output() crearEstudiante = new EventEmitter<void>();
    @Output() paginaAnterior = new EventEmitter<void>();
    @Output() siguientePagina = new EventEmitter<void>();

    totalPagesNum(): number {
        return Math.max(1, Math.ceil(this.total / this.pageSize));
    }
}
