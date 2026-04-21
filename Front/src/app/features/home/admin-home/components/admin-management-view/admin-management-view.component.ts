import { Component, OnInit, computed, inject, signal, ViewChild, DestroyRef, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import {
    SchoolApiService,
    CursoItem, AsignaturaItem, ProfesorListItem, EstudianteItem,
    UpdateProfesorData, UpdateEstudianteData, CsvImportResult, CsvImportEntity, CsvImportError,
    AdminMatriculaListItem, AdminImparticionListItem, TareaConNotas
} from '../../../../../shared/services/school-api.service';
import { ToastService } from '../../../../../core/services/toast.service';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { AdminTabsNavComponent } from '../admin-tabs-nav/admin-tabs-nav.component';
import { CsvImportCardComponent } from '../csv-import-card/csv-import-card.component';
import { Subject, debounceTime } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { dniValidator, isoDateValidator, phoneValidator } from '../../../../../core/validators/profile.validators';

type AdminTab = 'cursos' | 'asignaturas' | 'profesores' | 'estudiantes' | 'matriculas' | 'imparticiones' | 'importar';

type CsvErrorGroupKey =
    | 'curso'
    | 'asignatura'
    | 'profesor'
    | 'estudiante'
    | 'duplicado'
    | 'formato'
    | 'otros';

type CsvErrorGroup = {
    key: CsvErrorGroupKey;
    label: string;
    errors: string[];
};

const CSV_ERROR_PREVIEW_COUNT = 5;
const MAX_CSV_FILE_SIZE_BYTES = 10 * 1024 * 1024;

@Component({
    selector: 'app-admin-management-view',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        AdminTabsNavComponent,
        ConfirmDialogComponent,
        CsvImportCardComponent
    ],
    templateUrl: './admin-management-view.html',
    styleUrls: ['./admin-management-view.scss']
})
export class AdminManagementViewComponent implements OnInit {
    @ViewChild(ConfirmDialogComponent) confirmDialog!: ConfirmDialogComponent;
    @Output() dataChanged = new EventEmitter<void>();

    private api = inject(SchoolApiService);
    private toast = inject(ToastService);
    private fb = inject(FormBuilder);
    private destroyRef = inject(DestroyRef);

    tabActiva = signal<AdminTab>('cursos');
    private readonly resourcesLoaded = signal<{
        cursos: boolean;
        asignaturas: boolean;
        profesores: boolean;
        estudiantes: boolean;
        matriculas: boolean;
        imparticiones: boolean;
    }>({
        cursos: false,
        asignaturas: false,
        profesores: false,
        estudiantes: false,
        matriculas: false,
        imparticiones: false
    });

    cursos = signal<CursoItem[]>([]);
    asignaturas = signal<AsignaturaItem[]>([]);
    profesores = signal<ProfesorListItem[]>([]);
    estudiantes = signal<EstudianteItem[]>([]);
    matriculas = signal<AdminMatriculaListItem[]>([]);
    imparticiones = signal<AdminImparticionListItem[]>([]);

    private readonly busquedaCursosInput$ = new Subject<string>();
    private readonly busquedaAsignaturasInput$ = new Subject<string>();
    private readonly busquedaProfesoresInput$ = new Subject<string>();
    private readonly busquedaEstudiantesInput$ = new Subject<string>();

    readonly opCargando = signal<Record<string, boolean>>({});

    // Curso Management
    nuevoCursoNombre = '';
    editandoCursoId: number | null = null;
    editCursoNombre = '';
    busquedaCursos = signal('');

    // Asignatura Management
    nuevaAsignaturaNombre = '';
    nuevaAsignaturaCursoId: number | null = null;
    filtroAsignaturasCursoId = signal<number | null>(null);
    editandoAsignaturaId: number | null = null;
    editAsignaturaNombre = '';
    editAsignaturaCursoId: number | null = null;
    busquedaAsignaturas = signal('');

    // Profesor Management
    nuevoProfesorNombre = '';
    nuevoProfesorCorreo = '';
    nuevoProfesorContrasena = '';
    filtroProfesoresCursoId = signal<number | null>(null);
    editandoProfesorId: number | null = null;
    editProfesorNombre = '';
    editProfesorCorreo = '';
    editProfesorContrasena = '';
    busquedaProfesores = signal('');

    // Estudiante Management
    nuevoEstudianteNombre = '';
    nuevoEstudianteCorreo = '';
    nuevoEstudianteContrasena = '';
    nuevoEstudianteCursoId: number | null = null;
    filtroEstudiantesCursoId = signal<number | null>(null);
    editandoEstudianteId: number | null = null;
    editEstudianteNombre = '';
    editEstudianteCorreo = '';
    editEstudianteCursoId: number | null = null;
    editEstudianteContrasena = '';
    busquedaEstudiantes = signal('');

    // Matricula Management
    matriculaEstudianteId = signal<number | null>(null);
    matriculaAsignaturaId = signal<number | null>(null);
    filtroMatriculasCursoId = signal<number | null>(null);

    // Imparticion Management
    imparticionProfesorId = signal<number | null>(null);
    imparticionAsignaturaId = signal<number | null>(null);
    imparticionCursoId = signal<number | null>(null);
    filtroImparticionesCursoId = signal<number | null>(null);

    // Task Management
    mostrarModalTareas = signal(false);
    tareasConNotas = signal<TareaConNotas[]>([]);
    tareaEnDetalle = signal<TareaConNotas | null>(null);
    cargandoTareas = signal(false);

    // CSV Import
    csvCursosFile: File | null = null;
    csvAsignaturasFile: File | null = null;
    csvProfesoresFile: File | null = null;
    csvEstudiantesFile: File | null = null;
    csvTareasFile: File | null = null;
    csvMatriculasFile: File | null = null;
    csvImparticionesFile: File | null = null;
    csvNotasFile: File | null = null;
    csvResultado = signal<CsvImportResult | null>(null);
    csvEntidadActual = signal<CsvImportEntity | null>(null);
    csvCargando = signal(false);
    csvErroresExpandidos = signal<Record<string, boolean>>({});
    readonly csvImportItems: Array<{ entidad: CsvImportEntity; titulo: string; descripcion: string; orden: string }> = [
        { entidad: 'cursos', titulo: 'Cursos', descripcion: 'Alta masiva de cursos.', orden: '1' },
        { entidad: 'asignaturas', titulo: 'Asignaturas', descripcion: 'Alta masiva de asignaturas ligadas a curso.', orden: '2' },
        { entidad: 'profesores', titulo: 'Profesores', descripcion: 'Alta masiva de profesores.', orden: '3' },
        { entidad: 'estudiantes', titulo: 'Estudiantes', descripcion: 'Alta masiva de estudiantes con su curso.', orden: '4' },
        { entidad: 'imparticiones', titulo: 'Imparticiones', descripcion: 'Relaciona profesor, asignatura y curso.', orden: '5' },
        { entidad: 'tareas', titulo: 'Tareas', descripcion: 'Crea tareas por profesor, asignatura, curso y trimestre.', orden: '6' },
        { entidad: 'matriculas', titulo: 'Matriculas', descripcion: 'Relaciona estudiante con asignaturas de su curso.', orden: '7' },
        { entidad: 'notas', titulo: 'Notas', descripcion: 'Carga masiva de calificaciones sobre tareas ya existentes.', orden: '8' }
    ];

    csvErroresAgrupados = computed<CsvErrorGroup[]>(() => {
        const errores = this.csvResultado()?.errores ?? [];
        if (errores.length === 0) {
            return [];
        }

        const labels: Record<CsvErrorGroupKey, string> = {
            curso: 'Curso no valido o no encontrado',
            asignatura: 'Asignatura no valida o no encontrada',
            profesor: 'Profesor no valido o no encontrado',
            estudiante: 'Estudiante no valido o no encontrado',
            duplicado: 'Registros duplicados o ya existentes',
            formato: 'Formato o datos incompletos',
            otros: 'Otros errores'
        };

        const grouped = new Map<CsvErrorGroupKey, string[]>();
        for (const err of errores) {
            const key = this.clasificarCsvError(err);
            const current = grouped.get(key) ?? [];
            current.push(err);
            grouped.set(key, current);
        }

        const order: CsvErrorGroupKey[] = ['curso', 'asignatura', 'profesor', 'estudiante', 'duplicado', 'formato', 'otros'];
        return order
            .filter(key => grouped.has(key))
            .map(key => ({
                key,
                label: labels[key],
                errors: grouped.get(key) ?? []
            }));
    });

    grupoErroresExpandido(key: string): boolean {
        return this.csvErroresExpandidos()[key] ?? false;
    }

    toggleGrupoErrores(key: string): void {
        this.csvErroresExpandidos.update(current => ({
            ...current,
            [key]: !(current[key] ?? false)
        }));
    }

    erroresVisiblesGrupo(grupo: CsvErrorGroup): string[] {
        if (this.grupoErroresExpandido(grupo.key)) {
            return grupo.errors;
        }
        return grupo.errors.slice(0, CSV_ERROR_PREVIEW_COUNT);
    }

    erroresOcultosGrupo(grupo: CsvErrorGroup): number {
        return Math.max(grupo.errors.length - CSV_ERROR_PREVIEW_COUNT, 0);
    }

    private clasificarCsvError(error: string): CsvErrorGroupKey {
        const text = error.toLowerCase();

        if (text.includes('curso no encontrado') || text.includes('su curso')) {
            return 'curso';
        }

        if (text.includes('asignatura no encontrada') || text.includes('asignatura')) {
            return 'asignatura';
        }

        if (text.includes('profesor no encontrado') || text.includes('profesor')) {
            return 'profesor';
        }

        if (text.includes('estudiante no encontrado') || text.includes('estudiante')) {
            return 'estudiante';
        }

        if (text.includes('duplicado') || text.includes('ya existe') || text.includes('ya esta matriculado') || text.includes('ya tiene un profesor asignado')) {
            return 'duplicado';
        }

        if (text.includes('se esperaban') || text.includes('obligatori') || text.includes('datos incompletos') || text.includes('columnas')) {
            return 'formato';
        }

        return 'otros';
    }

    // Forms
    readonly cursoForm = this.fb.group({
        nombre: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(120)])
    });

    readonly editCursoForm = this.fb.group({
        nombre: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(120)])
    });

    readonly asignaturaForm = this.fb.group({
        nombre: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(120)]),
        cursoId: this.fb.control<number | null>(null, [Validators.required])
    });

    readonly editAsignaturaForm = this.fb.group({
        nombre: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(120)]),
        cursoId: this.fb.control<number | null>(null, [Validators.required])
    });

    readonly profesorForm = this.fb.group({
        nombre: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(120)]),
        apellidos: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(120)]),
        dni: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(20), dniValidator()]),
        telefono: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(20), phoneValidator()]),
        especialidad: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(100)]),
        correo: this.fb.nonNullable.control('', [Validators.required, Validators.email, Validators.maxLength(200)]),
        contrasena: this.fb.nonNullable.control('', [Validators.required, Validators.minLength(6), Validators.maxLength(200)])
    });

    readonly editProfesorForm = this.fb.group({
        nombre: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(120)]),
        apellidos: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(120)]),
        dni: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(20), dniValidator()]),
        telefono: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(20), phoneValidator()]),
        especialidad: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(100)]),
        correo: this.fb.nonNullable.control('', [Validators.required, Validators.email, Validators.maxLength(200)]),
        nuevaContrasena: this.fb.nonNullable.control('', [Validators.minLength(6), Validators.maxLength(200)])
    });

    readonly estudianteForm = this.fb.group({
        nombre: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(120)]),
        apellidos: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(120)]),
        dni: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(20), dniValidator()]),
        telefono: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(20), phoneValidator()]),
        fechaNacimiento: this.fb.nonNullable.control('', [Validators.required, isoDateValidator()]),
        correo: this.fb.nonNullable.control('', [Validators.required, Validators.email, Validators.maxLength(200)]),
        contrasena: this.fb.nonNullable.control('', [Validators.required, Validators.minLength(6), Validators.maxLength(200)]),
        cursoId: this.fb.control<number | null>(null, [Validators.required])
    });

    readonly editEstudianteForm = this.fb.group({
        nombre: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(120)]),
        apellidos: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(120)]),
        dni: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(20), dniValidator()]),
        telefono: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(20), phoneValidator()]),
        fechaNacimiento: this.fb.nonNullable.control('', [Validators.required, isoDateValidator()]),
        correo: this.fb.nonNullable.control('', [Validators.required, Validators.email, Validators.maxLength(200)]),
        nuevaContrasena: this.fb.nonNullable.control('', [Validators.minLength(6), Validators.maxLength(200)]),
        cursoId: this.fb.control<number | null>(null, [Validators.required])
    });

    // Computed
    cursosVista = computed<CursoItem[]>(() => {
        const q = this.busquedaCursos().trim().toLowerCase();
        const cursos = this.cursos();
        return q ? cursos.filter(c => c.nombre.toLowerCase().includes(q)) : cursos;
    });

    asignaturasVista = computed<AsignaturaItem[]>(() => {
        let result = this.asignaturas();
        const cursoId = this.filtroAsignaturasCursoId();
        if (cursoId)
            result = result.filter(a => a.curso.id === Number(cursoId));
        const q = this.busquedaAsignaturas().trim().toLowerCase();
        if (q) result = result.filter(a => a.nombre.toLowerCase().includes(q));
        return result;
    });

    profesoresVista = computed<ProfesorListItem[]>(() => {
        let result = this.profesores();
        const cursoIdFiltro = this.filtroProfesoresCursoId();
        if (cursoIdFiltro) {
            const cursoId = Number(cursoIdFiltro);
            result = result.filter(p => p.imparticiones.some(i => i.cursoId === cursoId));
        }
        const q = this.busquedaProfesores().trim().toLowerCase();
        if (q) result = result.filter(p => p.nombre.toLowerCase().includes(q) || p.correo.toLowerCase().includes(q));
        return result;
    });

    estudiantesVista = computed<EstudianteItem[]>(() => {
        let result = this.estudiantes();
        const cursoId = this.filtroEstudiantesCursoId();
        if (cursoId)
            result = result.filter(e => e.cursoId === Number(cursoId));
        const q = this.busquedaEstudiantes().trim().toLowerCase();
        if (q) result = result.filter(e => e.nombre.toLowerCase().includes(q) || e.correo.toLowerCase().includes(q));
        return result;
    });

    asignaturasFiltradas = computed<AsignaturaItem[]>(() => {
        const estudianteId = this.matriculaEstudianteId();
        if (!estudianteId) return this.asignaturas();
        const est = this.estudiantes().find(e => e.id === Number(estudianteId));
        if (!est) return this.asignaturas();
        return this.asignaturas().filter(a => a.curso.id === est.cursoId);
    });

    asignaturasDeImparticion = computed<AsignaturaItem[]>(() => {
        const cursoId = this.imparticionCursoId();
        if (!cursoId) return this.asignaturas();
        return this.asignaturas().filter(a => a.curso.id === Number(cursoId));
    });

    matriculasVista = computed<AdminMatriculaListItem[]>(() => {
        const cursoFiltroRaw = this.filtroMatriculasCursoId();
        const cursoFiltro = cursoFiltroRaw ? Number(cursoFiltroRaw) : null;
        return this.matriculas()
            .filter(m => !cursoFiltro || m.cursoId === cursoFiltro)
            .sort((a, b) => a.estudiante.localeCompare(b.estudiante));
    });

    imparticionesVista = computed<AdminImparticionListItem[]>(() => {
        const cursoFiltroRaw = this.filtroImparticionesCursoId();
        const cursoFiltro = cursoFiltroRaw ? Number(cursoFiltroRaw) : null;
        return this.imparticiones()
            .filter(x => !cursoFiltro || x.cursoId === cursoFiltro)
            .sort((a, b) => a.curso.localeCompare(b.curso) || a.asignatura.localeCompare(b.asignatura));
    });

    ngOnInit(): void {
        this.configurarDebounceBusquedas();
        void this.cargarTab(this.tabActiva());
    }

    onBusquedaCursosChange(value: string): void {
        this.busquedaCursosInput$.next(value ?? '');
    }

    onBusquedaAsignaturasChange(value: string): void {
        this.busquedaAsignaturasInput$.next(value ?? '');
    }

    onBusquedaProfesoresChange(value: string): void {
        this.busquedaProfesoresInput$.next(value ?? '');
    }

    onBusquedaEstudiantesChange(value: string): void {
        this.busquedaEstudiantesInput$.next(value ?? '');
    }

    estaCargando(op: string): boolean {
        return !!this.opCargando()[op];
    }

    private setOpCargando(op: string, value: boolean): void {
        this.opCargando.update(current => ({ ...current, [op]: value }));
    }

    private async runWithLoading<T>(op: string, work: () => Promise<T>): Promise<T> {
        this.setOpCargando(op, true);
        try {
            return await work();
        } finally {
            this.setOpCargando(op, false);
        }
    }

    private configurarDebounceBusquedas(): void {
        this.busquedaCursosInput$
            .pipe(debounceTime(250), takeUntilDestroyed(this.destroyRef))
            .subscribe(value => this.busquedaCursos.set(value));

        this.busquedaAsignaturasInput$
            .pipe(debounceTime(250), takeUntilDestroyed(this.destroyRef))
            .subscribe(value => this.busquedaAsignaturas.set(value));

        this.busquedaProfesoresInput$
            .pipe(debounceTime(250), takeUntilDestroyed(this.destroyRef))
            .subscribe(value => this.busquedaProfesores.set(value));

        this.busquedaEstudiantesInput$
            .pipe(debounceTime(250), takeUntilDestroyed(this.destroyRef))
            .subscribe(value => this.busquedaEstudiantes.set(value));
    }

    cambiarTab(tab: AdminTab): void {
        if (this.tabActiva() === tab) {
            return;
        }

        this.tabActiva.set(tab);
        this.cancelarEdicion();
        void this.cargarTab(tab);
    }

    private async cargarTab(tab: AdminTab, force = false): Promise<void> {
        switch (tab) {
            case 'cursos':
                await this.cargarCursos(force);
                break;
            case 'asignaturas':
                await Promise.all([this.cargarCursos(force), this.cargarAsignaturas(force)]);
                break;
            case 'profesores':
                await Promise.all([this.cargarCursos(force), this.cargarProfesores(force)]);
                break;
            case 'estudiantes':
                await Promise.all([this.cargarCursos(force), this.cargarEstudiantes(force)]);
                break;
            case 'matriculas':
                await Promise.all([
                    this.cargarCursos(force),
                    this.cargarAsignaturas(force),
                    this.cargarEstudiantes(force),
                    this.cargarMatriculas(force)
                ]);
                break;
            case 'imparticiones':
                await Promise.all([
                    this.cargarCursos(force),
                    this.cargarAsignaturas(force),
                    this.cargarProfesores(force),
                    this.cargarImparticiones(force)
                ]);
                break;
            case 'importar':
                // No carga listas hasta que se necesiten en otras pestañas.
                break;
        }
    }

    private setResourceLoaded(resource: 'cursos' | 'asignaturas' | 'profesores' | 'estudiantes' | 'matriculas' | 'imparticiones', loaded: boolean): void {
        this.resourcesLoaded.update(current => ({ ...current, [resource]: loaded }));
    }

    private async cargarMatriculas(force = false): Promise<void> {
        if (!force && this.resourcesLoaded().matriculas) {
            return;
        }

        await this.runWithLoading('cargarMatriculas', async () => {
            try {
                this.matriculas.set(await this.api.getAdminMatriculas());
                this.setResourceLoaded('matriculas', true);
            } catch (e) {
                this.mostrarError(e, 'No se pudieron cargar las matriculas.');
            }
        });
    }

    private async cargarImparticiones(force = false): Promise<void> {
        if (!force && this.resourcesLoaded().imparticiones) {
            return;
        }

        await this.runWithLoading('cargarImparticiones', async () => {
            try {
                this.imparticiones.set(await this.api.getAdminImparticiones());
                this.setResourceLoaded('imparticiones', true);
            } catch (e) {
                this.mostrarError(e, 'No se pudieron cargar las imparticiones.');
            }
        });
    }

    private async cargarCursos(force = false): Promise<void> {
        if (!force && this.resourcesLoaded().cursos) {
            return;
        }

        await this.runWithLoading('cargarCursos', async () => {
            try {
                this.cursos.set(await this.api.getCursos());
                this.setResourceLoaded('cursos', true);
            } catch (e) {
                this.mostrarError(e, 'No se pudieron cargar los cursos.');
            }
        });
    }

    private async cargarAsignaturas(force = false): Promise<void> {
        if (!force && this.resourcesLoaded().asignaturas) {
            return;
        }

        await this.runWithLoading('cargarAsignaturas', async () => {
            try {
                this.asignaturas.set(await this.api.getAsignaturas());
                this.setResourceLoaded('asignaturas', true);
            } catch (e) {
                this.mostrarError(e, 'No se pudieron cargar las asignaturas.');
            }
        });
    }

    private async cargarProfesores(force = false): Promise<void> {
        if (!force && this.resourcesLoaded().profesores) {
            return;
        }

        await this.runWithLoading('cargarProfesores', async () => {
            try {
                this.profesores.set(await this.api.getProfesores());
                this.setResourceLoaded('profesores', true);
            } catch (e) {
                this.mostrarError(e, 'No se pudieron cargar los profesores.');
            }
        });
    }

    private async cargarEstudiantes(force = false): Promise<void> {
        if (!force && this.resourcesLoaded().estudiantes) {
            return;
        }

        await this.runWithLoading('cargarEstudiantes', async () => {
            try {
                this.estudiantes.set(await this.api.getEstudiantes());
                this.setResourceLoaded('estudiantes', true);
            } catch (e) {
                this.mostrarError(e, 'No se pudieron cargar los estudiantes.');
            }
        });
    }

    private invalidateForImport(entidad: CsvImportEntity): void {
        if (entidad === 'cursos') {
            this.setResourceLoaded('cursos', false);
            this.setResourceLoaded('asignaturas', false);
            this.setResourceLoaded('estudiantes', false);
            this.setResourceLoaded('profesores', false);
            this.setResourceLoaded('matriculas', false);
            this.setResourceLoaded('imparticiones', false);
            return;
        }

        if (entidad === 'asignaturas') {
            this.setResourceLoaded('asignaturas', false);
            this.setResourceLoaded('matriculas', false);
            this.setResourceLoaded('imparticiones', false);
            return;
        }

        if (entidad === 'profesores') {
            this.setResourceLoaded('profesores', false);
            this.setResourceLoaded('imparticiones', false);
            return;
        }

        if (entidad === 'estudiantes') {
            this.setResourceLoaded('estudiantes', false);
            this.setResourceLoaded('matriculas', false);
            return;
        }

        if (entidad === 'tareas') {
            this.setResourceLoaded('asignaturas', false);
            return;
        }

        if (entidad === 'matriculas') {
            this.setResourceLoaded('asignaturas', false);
            this.setResourceLoaded('matriculas', false);
            return;
        }

        if (entidad === 'imparticiones') {
            this.setResourceLoaded('profesores', false);
            this.setResourceLoaded('imparticiones', false);
            return;
        }

        if (entidad === 'notas') {
            this.setResourceLoaded('asignaturas', false);
        }
    }

    private mostrarError(error: unknown, fallback = 'No se pudo completar la operacion.'): void {
        const message = error instanceof Error ? error.message : fallback;
        this.toast.show(message || fallback, 'error');
    }

    cancelarEdicion(): void {
        this.editandoCursoId = null;
        this.editandoAsignaturaId = null;
        this.editandoProfesorId = null;
        this.editandoEstudianteId = null;
        this.editCursoForm.reset({ nombre: '' });
        this.editAsignaturaForm.reset({ nombre: '', cursoId: null });
        this.editProfesorForm.reset({ nombre: '', apellidos: '', dni: '', telefono: '', especialidad: '', correo: '', nuevaContrasena: '' });
        this.editEstudianteForm.reset({ nombre: '', apellidos: '', dni: '', telefono: '', fechaNacimiento: '', correo: '', nuevaContrasena: '', cursoId: null });
    }

    // CRUD: Cursos
    async crearCurso(): Promise<void> {
        if (this.cursoForm.invalid) {
            this.toast.show('El nombre del curso es obligatorio.', 'warning');
            return;
        }

        await this.runWithLoading('crearCurso', async () => {
            try {
                const nombre = (this.cursoForm.value.nombre ?? '').trim();
                const c = await this.api.createCurso(nombre);
                this.cursos.set([...this.cursos(), c]);
                this.cursoForm.reset({ nombre: '' });
                this.toast.show(`Curso "${c.nombre}" creado.`, 'success');
                this.dataChanged.emit();
            } catch (e) {
                this.mostrarError(e);
            }
        });
    }

    iniciarEditarCurso(c: CursoItem): void {
        this.editandoCursoId = c.id;
        this.editCursoForm.setValue({ nombre: c.nombre });
    }

    async guardarCurso(): Promise<void> {
        if (!this.editandoCursoId || this.editCursoForm.invalid) {
            return;
        }

        await this.runWithLoading('guardarCurso', async () => {
            try {
                const nombre = (this.editCursoForm.value.nombre ?? '').trim();
                const updated = await this.api.updateCurso(this.editandoCursoId!, nombre);
                this.cursos.update(list => list.map(c => c.id === updated.id ? updated : c));
                this.editandoCursoId = null;
                this.toast.show('Curso actualizado.', 'success');
                this.dataChanged.emit();
            } catch (e) {
                this.mostrarError(e);
            }
        });
    }

    async eliminarCurso(id: number, nombre: string): Promise<void> {
        await Promise.all([this.cargarAsignaturas(), this.cargarEstudiantes()]);
        const asignaturas = this.asignaturas().filter(a => a.curso.id === id).length;
        const estudiantes = this.estudiantes().filter(e => e.cursoId === id).length;
        const confirmado = await this.confirmDialog.show(
            'Eliminar curso',
            `¿Eliminar el curso "${nombre}"? Se veran afectados ${asignaturas} asignaturas y ${estudiantes} estudiantes.`
        );
        if (!confirmado) return;
        await this.runWithLoading('eliminarCurso', async () => {
            try {
                await this.api.deleteCurso(id);
                this.cursos.update(list => list.filter(c => c.id !== id));
                this.toast.show(`Curso "${nombre}" eliminado.`, 'success');
                this.setResourceLoaded('cursos', false);
                this.setResourceLoaded('asignaturas', false);
                this.setResourceLoaded('estudiantes', false);
                this.setResourceLoaded('matriculas', false);
                this.setResourceLoaded('imparticiones', false);
                await this.cargarTab(this.tabActiva(), true);
                this.dataChanged.emit();
            } catch (e) {
                this.mostrarError(e);
            }
        });
    }

    // CRUD: Asignaturas
    async crearAsignatura(): Promise<void> {
        if (this.asignaturaForm.invalid) {
            this.toast.show('Nombre y curso son obligatorios.', 'warning');
            return;
        }

        await this.runWithLoading('crearAsignatura', async () => {
            try {
                const nombre = (this.asignaturaForm.value.nombre ?? '').trim();
                const cursoId = Number(this.asignaturaForm.value.cursoId);
                const a = await this.api.createAsignatura(nombre, cursoId);
                this.asignaturas.set([...this.asignaturas(), a]);
                this.asignaturaForm.reset({ nombre: '', cursoId: null });
                this.toast.show(`Asignatura "${a.nombre}" creada.`, 'success');
                this.dataChanged.emit();
            } catch (e) {
                this.mostrarError(e);
            }
        });
    }

    iniciarEditarAsignatura(a: AsignaturaItem): void {
        this.editandoAsignaturaId = a.id;
        this.editAsignaturaForm.setValue({ nombre: a.nombre, cursoId: a.curso.id });
    }

    async guardarAsignatura(): Promise<void> {
        if (!this.editandoAsignaturaId || this.editAsignaturaForm.invalid) return;

        await this.runWithLoading('guardarAsignatura', async () => {
            try {
                const nombre = (this.editAsignaturaForm.value.nombre ?? '').trim();
                const cursoId = Number(this.editAsignaturaForm.value.cursoId);
                const updated = await this.api.updateAsignatura(this.editandoAsignaturaId!, nombre, cursoId);
                this.asignaturas.update(list => list.map(a => a.id === updated.id ? updated : a));
                this.editandoAsignaturaId = null;
                this.toast.show('Asignatura actualizada.', 'success');
                this.dataChanged.emit();
            } catch (e) {
                this.mostrarError(e);
            }
        });
    }

    async eliminarAsignatura(id: number, nombre: string): Promise<void> {
        const asignatura = this.asignaturas().find(a => a.id === id);
        const alumnos = asignatura?.alumnos.length ?? 0;
        const confirmado = await this.confirmDialog.show(
            'Eliminar asignatura',
            `¿Eliminar la asignatura "${nombre}"? Se eliminaran sus tareas, notas y ${alumnos} matriculas relacionadas.`
        );
        if (!confirmado) return;
        await this.runWithLoading('eliminarAsignatura', async () => {
            try {
                await this.api.deleteAsignatura(id);
                this.asignaturas.update(list => list.filter(a => a.id !== id));
                this.toast.show(`Asignatura "${nombre}" eliminada.`, 'success');
                this.setResourceLoaded('asignaturas', false);
                this.setResourceLoaded('matriculas', false);
                this.setResourceLoaded('imparticiones', false);
                await this.cargarTab(this.tabActiva(), true);
                this.dataChanged.emit();
            } catch (e) {
                this.mostrarError(e);
            }
        });
    }

    // CRUD: Profesores
    async crearProfesor(): Promise<void> {
        if (this.profesorForm.invalid) {
            this.toast.show('Completa todos los campos del profesor y revisa DNI/telefono.', 'warning');
            return;
        }

        await this.runWithLoading('crearProfesor', async () => {
            try {
                const p = await this.api.createProfesor({
                    nombre: (this.profesorForm.value.nombre ?? '').trim(),
                    apellidos: (this.profesorForm.value.apellidos ?? '').trim(),
                    dni: (this.profesorForm.value.dni ?? '').trim().toUpperCase(),
                    telefono: (this.profesorForm.value.telefono ?? '').trim(),
                    especialidad: (this.profesorForm.value.especialidad ?? '').trim(),
                    correo: (this.profesorForm.value.correo ?? '').trim(),
                    contrasena: this.profesorForm.value.contrasena ?? ''
                });
                this.profesores.set([...this.profesores(), p]);
                this.profesorForm.reset({ nombre: '', apellidos: '', dni: '', telefono: '', especialidad: '', correo: '', contrasena: '' });
                this.toast.show(`Profesor "${p.nombre}" creado.`, 'success');
                this.dataChanged.emit();
            } catch (e) {
                this.mostrarError(e);
            }
        });
    }

    iniciarEditarProfesor(p: ProfesorListItem): void {
        this.editandoProfesorId = p.id;
        this.editProfesorForm.setValue({
            nombre: p.nombre,
            apellidos: p.apellidos,
            dni: p.dni,
            telefono: p.telefono,
            especialidad: p.especialidad,
            correo: p.correo,
            nuevaContrasena: ''
        });
    }

    async guardarProfesor(): Promise<void> {
        if (!this.editandoProfesorId || this.editProfesorForm.invalid) return;

        const data: UpdateProfesorData = {
            nombre: (this.editProfesorForm.value.nombre ?? '').trim(),
            apellidos: (this.editProfesorForm.value.apellidos ?? '').trim(),
            dni: (this.editProfesorForm.value.dni ?? '').trim().toUpperCase(),
            telefono: (this.editProfesorForm.value.telefono ?? '').trim(),
            especialidad: (this.editProfesorForm.value.especialidad ?? '').trim(),
            correo: (this.editProfesorForm.value.correo ?? '').trim(),
            nuevaContrasena: this.editProfesorForm.value.nuevaContrasena || undefined
        };

        await this.runWithLoading('guardarProfesor', async () => {
            try {
                const updated = await this.api.updateProfesor(this.editandoProfesorId!, data);
                this.profesores.update(list => list.map(p => p.id === updated.id ? updated : p));
                this.editandoProfesorId = null;
                this.toast.show('Profesor actualizado.', 'success');
            } catch (e) {
                this.mostrarError(e);
            }
        });
    }

    async eliminarProfesor(id: number, nombre: string): Promise<void> {
        const imparticiones = this.profesores().find(p => p.id === id)?.imparticiones.length ?? 0;
        const confirmado = await this.confirmDialog.show(
            'Eliminar profesor',
            `¿Eliminar al profesor "${nombre}"? Tiene ${imparticiones} imparticiones asignadas y se eliminaran sus tareas.`
        );
        if (!confirmado) return;

        await this.runWithLoading('eliminarProfesor', async () => {
            try {
                await this.api.deleteProfesor(id);
                this.profesores.update(list => list.filter(p => p.id !== id));
                this.toast.show(`Profesor "${nombre}" eliminado.`, 'success');
                this.setResourceLoaded('profesores', false);
                this.setResourceLoaded('imparticiones', false);
                await this.cargarTab(this.tabActiva(), true);
                this.dataChanged.emit();
            } catch (e) {
                this.mostrarError(e);
            }
        });
    }

    // CRUD: Estudiantes
    async crearEstudiante(): Promise<void> {
        if (this.estudianteForm.invalid) {
            this.toast.show('Completa todos los campos del estudiante y revisa DNI/telefono/fecha.', 'warning');
            return;
        }

        await this.runWithLoading('crearEstudiante', async () => {
            try {
                const e = await this.api.createEstudiante({
                    nombre: (this.estudianteForm.value.nombre ?? '').trim(),
                    apellidos: (this.estudianteForm.value.apellidos ?? '').trim(),
                    dni: (this.estudianteForm.value.dni ?? '').trim().toUpperCase(),
                    telefono: (this.estudianteForm.value.telefono ?? '').trim(),
                    fechaNacimiento: (this.estudianteForm.value.fechaNacimiento ?? '').trim(),
                    correo: (this.estudianteForm.value.correo ?? '').trim(),
                    contrasena: this.estudianteForm.value.contrasena ?? '',
                    cursoId: Number(this.estudianteForm.value.cursoId)
                });
                this.estudiantes.set([...this.estudiantes(), e]);
                this.estudianteForm.reset({ nombre: '', apellidos: '', dni: '', telefono: '', fechaNacimiento: '', correo: '', contrasena: '', cursoId: null });
                this.toast.show(`Estudiante "${e.nombre}" creado.`, 'success');
                this.dataChanged.emit();
            } catch (e) {
                this.mostrarError(e);
            }
        });
    }

    iniciarEditarEstudiante(e: EstudianteItem): void {
        this.editandoEstudianteId = e.id;
        this.editEstudianteForm.setValue({
            nombre: e.nombre,
            apellidos: e.apellidos,
            dni: e.dni,
            telefono: e.telefono,
            fechaNacimiento: e.fechaNacimiento,
            correo: e.correo,
            cursoId: e.cursoId,
            nuevaContrasena: ''
        });
    }

    async guardarEstudiante(): Promise<void> {
        if (!this.editandoEstudianteId || this.editEstudianteForm.invalid) return;

        const data: UpdateEstudianteData = {
            nombre: (this.editEstudianteForm.value.nombre ?? '').trim(),
            apellidos: (this.editEstudianteForm.value.apellidos ?? '').trim(),
            dni: (this.editEstudianteForm.value.dni ?? '').trim().toUpperCase(),
            telefono: (this.editEstudianteForm.value.telefono ?? '').trim(),
            fechaNacimiento: (this.editEstudianteForm.value.fechaNacimiento ?? '').trim(),
            correo: (this.editEstudianteForm.value.correo ?? '').trim(),
            cursoId: Number(this.editEstudianteForm.value.cursoId),
            nuevaContrasena: this.editEstudianteForm.value.nuevaContrasena || undefined
        };

        await this.runWithLoading('guardarEstudiante', async () => {
            try {
                const updated = await this.api.updateEstudiante(this.editandoEstudianteId!, data);
                this.estudiantes.update(list => list.map(e => e.id === updated.id ? updated : e));
                this.editandoEstudianteId = null;
                this.toast.show('Estudiante actualizado.', 'success');
            } catch (e) {
                this.mostrarError(e);
            }
        });
    }

    async eliminarEstudiante(id: number, nombre: string): Promise<void> {
        await this.cargarAsignaturas();
        const asignaturasMatriculadas = this.asignaturas().filter(a => a.alumnos.some(al => al.id === id)).length;
        const confirmado = await this.confirmDialog.show(
            'Eliminar estudiante',
            `¿Eliminar al estudiante "${nombre}"? Tiene ${asignaturasMatriculadas} matriculas activas y se eliminaran sus notas.`
        );
        if (!confirmado) return;

        await this.runWithLoading('eliminarEstudiante', async () => {
            try {
                await this.api.deleteEstudiante(id);
                this.estudiantes.update(list => list.filter(e => e.id !== id));
                this.toast.show(`Estudiante "${nombre}" eliminado.`, 'success');
                this.setResourceLoaded('estudiantes', false);
                this.setResourceLoaded('asignaturas', false);
                this.setResourceLoaded('matriculas', false);
                await this.cargarTab(this.tabActiva(), true);
                this.dataChanged.emit();
            } catch (e) {
                this.mostrarError(e);
            }
        });
    }

    // Matriculas
    async matricularEstudiante(): Promise<void> {
        if (!this.matriculaEstudianteId() || !this.matriculaAsignaturaId()) {
            this.toast.show('Selecciona un estudiante y una asignatura.', 'warning');
            return;
        }

        const estudianteId = Number(this.matriculaEstudianteId());
        const asignaturaId = Number(this.matriculaAsignaturaId());
        await this.runWithLoading('matricularEstudiante', async () => {
            try {
                await this.api.matricularEstudiante(estudianteId, asignaturaId);

                const estudiante = this.estudiantes().find(e => e.id === estudianteId);
                if (estudiante) {
                    this.asignaturas.update(list => list.map(asignatura => {
                        if (asignatura.id !== asignaturaId) {
                            return asignatura;
                        }

                        if (asignatura.alumnos.some(alumno => alumno.id === estudianteId)) {
                            return asignatura;
                        }

                        return {
                            ...asignatura,
                            alumnos: [...asignatura.alumnos, { id: estudiante.id, nombre: estudiante.nombre }]
                        };
                    }));
                }

                this.matriculaAsignaturaId.set(null);
                await this.cargarMatriculas(true);
                this.toast.show('Matricula realizada correctamente.', 'success');
                this.dataChanged.emit();
            } catch (e) {
                this.mostrarError(e);
            }
        });
    }

    // Imparticiones
    async asignarImparticion(): Promise<void> {
        if (!this.imparticionProfesorId() || !this.imparticionAsignaturaId() || !this.imparticionCursoId()) {
            this.toast.show('Selecciona profesor, asignatura y curso.', 'warning');
            return;
        }

        const profesorId = Number(this.imparticionProfesorId());
        const asignaturaId = Number(this.imparticionAsignaturaId());
        const cursoId = Number(this.imparticionCursoId());
        await this.runWithLoading('asignarImparticion', async () => {
            try {
                await this.api.asignarImparticion(profesorId, asignaturaId, cursoId);

                const asignatura = this.asignaturas().find(a => a.id === asignaturaId);
                const curso = this.cursos().find(c => c.id === cursoId);

                if (asignatura && curso) {
                    this.profesores.update(list => list.map(profesor => {
                        if (profesor.id !== profesorId) {
                            return profesor;
                        }

                        const yaExiste = profesor.imparticiones.some(i => i.asignaturaId === asignaturaId && i.cursoId === cursoId);
                        if (yaExiste) {
                            return profesor;
                        }

                        return {
                            ...profesor,
                            imparticiones: [
                                ...profesor.imparticiones,
                                {
                                    asignaturaId,
                                    asignatura: asignatura.nombre,
                                    cursoId,
                                    curso: curso.nombre
                                }
                            ]
                        };
                    }));
                }

                this.imparticionAsignaturaId.set(null);
                await this.cargarImparticiones(true);
                this.toast.show('Imparticion asignada correctamente.', 'success');
            } catch (e) {
                this.mostrarError(e);
            }
        });
    }

    async eliminarMatricula(estudianteId: number, asignaturaId: number, asignaturaNombre: string): Promise<void> {
        const confirmado = await this.confirmDialog.show(
            'Eliminar matricula',
            `¿Desmatricular al estudiante de "${asignaturaNombre}"?`
        );
        if (!confirmado) return;
        await this.runWithLoading('eliminarMatricula', async () => {
            try {
                await this.api.desmatricularEstudiante(estudianteId, asignaturaId);
                this.asignaturas.update(list => list.map(a => {
                    if (a.id !== asignaturaId) return a;
                    return { ...a, alumnos: a.alumnos.filter(al => al.id !== estudianteId) };
                }));
                await this.cargarMatriculas(true);
                this.toast.show('Matricula eliminada.', 'success');
                this.dataChanged.emit();
            } catch (e) {
                this.mostrarError(e);
            }
        });
    }

    async eliminarImparticion(profesorId: number, asignaturaId: number, cursoId: number, asignaturaNombre: string): Promise<void> {
        const confirmado = await this.confirmDialog.show(
            'Eliminar imparticion',
            `¿Quitar la imparticion de "${asignaturaNombre}"?`
        );
        if (!confirmado) return;
        await this.runWithLoading('eliminarImparticion', async () => {
            try {
                await this.api.eliminarImparticion(profesorId, asignaturaId, cursoId);
                this.profesores.update(list => list.map(p => {
                    if (p.id !== profesorId) return p;
                    return {
                        ...p,
                        imparticiones: p.imparticiones.filter(
                            i => !(i.asignaturaId === asignaturaId && i.cursoId === cursoId)
                        )
                    };
                }));
                await this.cargarImparticiones(true);
                this.toast.show('Imparticion eliminada.', 'success');
                this.dataChanged.emit();
            } catch (e) {
                this.mostrarError(e);
            }
        });
    }

    // CSV Import
    onCsvFileChange(event: Event, entidad: CsvImportEntity): void {
        const input = event.target as HTMLInputElement;
        const file = input.files?.[0] ?? null;

        if (file && file.size > MAX_CSV_FILE_SIZE_BYTES) {
            this.setCsvFile(entidad, null);
            input.value = '';
            this.toast.show('El archivo CSV no puede superar 10 MB.', 'warning');
            return;
        }

        this.setCsvFile(entidad, file);

        if (file) {
            this.toast.show(`Archivo ${file.name} preparado para importar ${entidad}.`, 'info');
        }
    }

    async importarCsv(entidad: CsvImportEntity): Promise<void> {
        const file = this.getCsvFile(entidad);

        if (!file) {
            this.toast.show('Selecciona un archivo CSV.', 'warning');
            return;
        }

        await this.runWithLoading(`importar-${entidad}`, async () => {
            this.csvCargando.set(true);
            this.csvResultado.set(null);
            this.csvEntidadActual.set(entidad);
            this.csvErroresExpandidos.set({});
            this.toast.show(`Importando ${entidad}...`, 'info');
            try {
                const resultado = await this.api.importarCsv(entidad, file);
                this.csvResultado.set(resultado);

                if (resultado.errores.length > 0) {
                    const primerError = resultado.errores[0];
                    this.toast.show(
                        `Importacion de ${entidad}: ${resultado.creados} creados, ${resultado.errores.length} errores. ${primerError}`,
                        'warning'
                    );
                } else {
                    this.toast.show(`Importacion de ${entidad}: ${resultado.creados} creados.`, 'success');
                }

                this.invalidateForImport(entidad);
                await this.cargarTab(this.tabActiva(), true);
                this.dataChanged.emit();
            } catch (e) {
                if (e instanceof CsvImportError) {
                    if (e.result) {
                        this.csvResultado.set(e.result);
                    }
                    this.toast.show(e.message, 'error');
                } else {
                    this.mostrarError(e, `No se pudo importar el CSV de ${entidad}.`);
                }
            } finally {
                this.clearCsvSelection(entidad);
                this.csvCargando.set(false);
            }
        });
    }

    private getCsvFile(entidad: CsvImportEntity): File | null {
        return entidad === 'cursos' ? this.csvCursosFile
            : entidad === 'asignaturas' ? this.csvAsignaturasFile
            : entidad === 'profesores' ? this.csvProfesoresFile
            : entidad === 'estudiantes' ? this.csvEstudiantesFile
            : entidad === 'tareas' ? this.csvTareasFile
            : entidad === 'matriculas' ? this.csvMatriculasFile
            : entidad === 'imparticiones' ? this.csvImparticionesFile
            : this.csvNotasFile;
    }

    private setCsvFile(entidad: CsvImportEntity, file: File | null): void {
        if (entidad === 'cursos') this.csvCursosFile = file;
        else if (entidad === 'asignaturas') this.csvAsignaturasFile = file;
        else if (entidad === 'profesores') this.csvProfesoresFile = file;
        else if (entidad === 'estudiantes') this.csvEstudiantesFile = file;
        else if (entidad === 'tareas') this.csvTareasFile = file;
        else if (entidad === 'matriculas') this.csvMatriculasFile = file;
        else if (entidad === 'imparticiones') this.csvImparticionesFile = file;
        else this.csvNotasFile = file;
    }

    private clearCsvSelection(entidad: CsvImportEntity): void {
        this.setCsvFile(entidad, null);
    }

    descargarPlantilla(entidad: CsvImportEntity): void {
        const plantillas: Record<CsvImportEntity, string> = {
            cursos: 'nombre\n1°A\n1°B\n2°A',
            asignaturas: 'nombre,cursoNombre\nMatematicas,1°A\nLengua,1°A\nCiencias,1°B',
            profesores: 'nombre,correo,contrasena\nJuan Garcia,juan@colegio.es,Pass123',
            estudiantes: 'nombre,correo,contrasena,cursoNombre\nLucia Perez,lucia@colegio.es,Pass123,1°A',
            tareas: 'profesorCorreo,asignaturaNombre,cursoNombre,trimestre,tareaNombre\njuan@colegio.es,Matematicas,1°A,1,Examen T1',
            matriculas: 'estudianteCorreo,asignaturaNombre,cursoNombre\nlucia@colegio.es,Matematicas,1°A',
            imparticiones: 'profesorCorreo,asignaturaNombre,cursoNombre\njuan@colegio.es,Matematicas,1°A',
            notas: 'profesorCorreo,estudianteCorreo,asignaturaNombre,cursoNombre,trimestre,tareaNombre,valor\njuan@colegio.es,lucia@colegio.es,Matematicas,1°A,1,Examen T1,7.50'
        };
        const csv = plantillas[entidad];
        const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `plantilla_${entidad}.csv`;
        link.click();
        URL.revokeObjectURL(url);
    }

    // Task Management
    async verTareasAsignatura(asignaturaId: number): Promise<void> {
        this.cargandoTareas.set(true);
        try {
            const tareas = await this.api.getTareasConNotas(asignaturaId);
            this.tareasConNotas.set(tareas);
            this.mostrarModalTareas.set(true);
        } catch (e) {
            this.mostrarError(e, 'No se pudieron cargar las tareas.');
        } finally {
            this.cargandoTareas.set(false);
        }
    }

    seleccionarTarea(tarea: TareaConNotas): void {
        this.tareaEnDetalle.set(tarea);
    }

    cerrarModalTareas(): void {
        this.mostrarModalTareas.set(false);
        this.tareaEnDetalle.set(null);
        this.tareasConNotas.set([]);
    }

}
