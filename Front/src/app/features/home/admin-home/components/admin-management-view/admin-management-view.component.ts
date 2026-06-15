import { ChangeDetectionStrategy, Component, DestroyRef, EventEmitter, OnInit, Output, ViewChild, computed, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, FormsModule, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import {
    SchoolApiService,
    CursoItem, AsignaturaItem, ProfesorListItem, EstudianteItem,
    UpdateProfesorData, UpdateEstudianteData, CsvImportResult, CsvImportEntity, CsvImportError,
    AdminMatriculaListItem, AdminImparticionListItem, AdminHorarioAsignaturaItem, TareaConNotas
} from '../../../../../shared/services/school-api.service';
import { ToastService } from '../../../../../core/services/toast.service';
import { ConfirmDialogComponent } from '../../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { AdminTabsNavComponent } from '../admin-tabs-nav/admin-tabs-nav.component';
import { SelectInputComponent, SelectOption } from '../../../../../shared/components/select-input/select-input.component';
import { AdminProfesoresTabComponent } from './tabs/admin-profesores-tab/admin-profesores-tab.component';
import { AdminEstudiantesTabComponent } from './tabs/admin-estudiantes-tab/admin-estudiantes-tab.component';
import { AdminMatriculasTabComponent } from './tabs/admin-matriculas-tab/admin-matriculas-tab.component';
import { AdminImparticionesTabComponent } from './tabs/admin-imparticiones-tab/admin-imparticiones-tab.component';
import { AdminImportarCsvTabComponent } from './tabs/admin-importar-csv-tab/admin-importar-csv-tab.component';
import { Subject, debounceTime } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import * as XLSX from 'xlsx';
import { AdminManagementForms, createAdminManagementForms, getAdminControlErrorMessage } from './admin-management-view.forms';
import {
    normalizeDniInput,
    normalizePhoneInput
} from '../../../../../core/validators/profile.validators';
import {
    agruparErroresCsv,
    CSV_ERROR_PREVIEW_COUNT,
    CSV_IMPORT_ITEMS,
    CSV_PLANTILLAS,
    MAX_CSV_FILE_SIZE_BYTES,
    validarHeadersCsv,
    type CsvErrorGroup
} from './admin-management-view.csv';

type AdminTab = 'cursos' | 'asignaturas' | 'profesores' | 'estudiantes' | 'matriculas' | 'imparticiones' | 'horarios' | 'importar';
type PaginacionKeys = 'profesores' | 'estudiantes' | 'matriculas' | 'imparticiones';

@Component({
    selector: 'app-admin-management-view',
    standalone: true,
    imports: [
        FormsModule,
        ReactiveFormsModule,
        AdminTabsNavComponent,
        ConfirmDialogComponent,
        SelectInputComponent,
        AdminProfesoresTabComponent,
        AdminEstudiantesTabComponent,
        AdminMatriculasTabComponent,
        AdminImparticionesTabComponent,
        AdminImportarCsvTabComponent
    ],
    templateUrl: './admin-management-view.html',
    styleUrls: ['./admin-management-view.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AdminManagementViewComponent implements OnInit {
    @ViewChild(ConfirmDialogComponent) confirmDialog!: ConfirmDialogComponent;
    @Output() dataChanged = new EventEmitter<void>();

    private api = inject(SchoolApiService);
    private toast = inject(ToastService);
    private fb = inject(FormBuilder);
    private destroyRef = inject(DestroyRef);

    tabActiva = signal<AdminTab>('cursos');
    private readonly tabBootstrapping = signal(false);
    private readonly resourcesLoaded = signal<{
        cursos: boolean;
        asignaturas: boolean;
        profesores: boolean;
        estudiantes: boolean;
        matriculas: boolean;
        imparticiones: boolean;
        horarios: boolean;
    }>({
        cursos: false,
        asignaturas: false,
        profesores: false,
        estudiantes: false,
        matriculas: false,
        imparticiones: false,
        horarios: false
    });

    paginacion = signal({
        profesores: { page: 0, pageSize: 50, total: 0 },
        estudiantes: { page: 0, pageSize: 50, total: 0 },
        matriculas: { page: 0, pageSize: 50, total: 0 },
        imparticiones: { page: 0, pageSize: 50, total: 0 },
    });

    cursos = signal<CursoItem[]>([]);
    asignaturas = signal<AsignaturaItem[]>([]);
    profesores = signal<ProfesorListItem[]>([]);
    estudiantes = signal<EstudianteItem[]>([]);
    matriculas = signal<AdminMatriculaListItem[]>([]);
    imparticiones = signal<AdminImparticionListItem[]>([]);
    horarios = signal<AdminHorarioAsignaturaItem[]>([]);

    private readonly busquedaCursosInput$ = new Subject<string>();
    private readonly busquedaAsignaturasInput$ = new Subject<string>();
    private readonly busquedaProfesoresInput$ = new Subject<string>();
    private readonly busquedaEstudiantesInput$ = new Subject<string>();

    readonly opCargando = signal<Record<string, boolean>>({});

    // #region Curso Management
    editandoCursoId: number | null = null;
    busquedaCursos = signal('');

    // #endregion
    // #region Asignatura Management
    filtroAsignaturasCursoId = signal<number | null>(null);
    editandoAsignaturaId: number | null = null;
    busquedaAsignaturas = signal('');

    // #endregion
    // #region Profesor Management
    editandoProfesorId: number | null = null;
    busquedaProfesores = signal('');

    // #endregion
    // #region Estudiante Management
    filtroEstudiantesCursoId = signal<number | null>(null);
    editandoEstudianteId: number | null = null;
    busquedaEstudiantes = signal('');
    bloquearCrearEstudianteHastaCambio = signal(false);

    // #endregion
    // #region Matricula Management
    matriculaEstudianteId = signal<number | null>(null);
    matriculaAsignaturaId = signal<number | null>(null);
    filtroMatriculasCursoId = signal<number | null>(null);

    // #endregion
    // #region Imparticion Management
    imparticionProfesorId = signal<number | null>(null);
    imparticionAsignaturaId = signal<number | null>(null);
    imparticionCursoId = signal<number | null>(null);
    filtroImparticionesCursoId = signal<number | null>(null);

    // #endregion
    // #region Horarios Management
    editandoHorarioId: number | null = null;
    horarioAsignaturaId = signal<number | null>(null);
    horarioDiaSemana = signal<number>(1);
    horarioHoraInicio = signal('08:30');
    horarioHoraFin = signal('09:25');
    horarioAula = signal('');
    filtroHorariosCursoId = signal<number | null>(null);

    // #endregion
    // #region Task Management
    mostrarModalTareas = signal(false);
    tareasConNotas = signal<TareaConNotas[]>([]);
    tareaEnDetalle = signal<TareaConNotas | null>(null);
    cargandoTareas = signal(false);

    // #endregion
    // #region CSV Import
    csvCursosFile: File | null = null;
    csvAsignaturasFile: File | null = null;
    csvProfesoresFile: File | null = null;
    csvEstudiantesFile: File | null = null;
    csvTareasFile: File | null = null;
    csvHorariosFile: File | null = null;
    csvMatriculasFile: File | null = null;
    csvImparticionesFile: File | null = null;
    csvNotasFile: File | null = null;
    csvResultado = signal<CsvImportResult | null>(null);
    csvEntidadActual = signal<CsvImportEntity | null>(null);
    csvCargando = signal(false);
    csvErroresExpandidos = signal<Record<string, boolean>>({});
    nombreEntidadCsv = signal<string>('');
    readonly csvImportItems = CSV_IMPORT_ITEMS;

    csvErroresAgrupados = computed<CsvErrorGroup[]>(() => {
        const errores = this.csvResultado()?.errores ?? [];
        if (errores.length === 0) {
            return [];
        }

        return agruparErroresCsv(errores);
    });

    csvOmitidosDetalle = computed<string[]>(() => {
        const result = this.csvResultado();
        if (!result || (result.omitidos ?? 0) <= 0) {
            return [];
        }

        return this.extraerDetalleOmitidos(result);
    });

    csvOmitidosNoDetallados = computed<number>(() => {
        const result = this.csvResultado();
        if (!result || (result.omitidos ?? 0) <= 0) {
            return 0;
        }

        return Math.max((result.omitidos ?? 0) - this.csvOmitidosDetalle().length, 0);
    });

    csvDetallesCreados = computed<string[]>(() => {
        const result = this.csvResultado();
        if (!result || !result.detalles?.length) {
            return [];
        }

        const omitidos = this.extraerDetalleOmitidos(result);
        if (omitidos.length === 0) {
            return result.detalles;
        }

        const omitidosSet = new Set(omitidos);
        return result.detalles.filter(det => !omitidosSet.has(det));
    });

    // #endregion

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

    csvArchivo(entidad: CsvImportEntity): File | null {
        return this.getCsvFile(entidad);
    }

    csvPuedeExpandirGrupo(grupo: CsvErrorGroup): boolean {
        return grupo.errors.length > CSV_ERROR_PREVIEW_COUNT;
    }

    getControlErrorMessage(control: AbstractControl | null): string | null {
        return getAdminControlErrorMessage(control);
    }

    cursoOptions = computed<SelectOption[]>(() => this.cursos().map(c => ({ value: c.id, label: c.nombre })));

    cargandoFormularioAsignaturas(): boolean {
        return this.cargandoListaCursos();
    }

    cargandoFormularioEstudiantes(): boolean {
        return this.cargandoListaCursos();
    }

    cargandoFormularioMatriculas(): boolean {
        const loaded = this.resourcesLoaded();
        return this.estaCargando('cargarEstudiantes')
            || this.estaCargando('cargarAsignaturas')
            || (this.tabBootstrapping() && (!loaded.estudiantes || !loaded.asignaturas));
    }

    cargandoFormularioImparticiones(): boolean {
        const loaded = this.resourcesLoaded();
        return this.estaCargando('cargarProfesores')
            || this.estaCargando('cargarCursos')
            || this.estaCargando('cargarAsignaturas')
            || (this.tabBootstrapping() && (!loaded.profesores || !loaded.cursos || !loaded.asignaturas));
    }

    cargandoListaCursos(): boolean {
        return this.estaCargando('cargarCursos') || (this.tabBootstrapping() && !this.resourcesLoaded().cursos);
    }

    cargandoListaAsignaturas(): boolean {
        return this.estaCargando('cargarAsignaturas') || (this.tabBootstrapping() && !this.resourcesLoaded().asignaturas);
    }

    cargandoListaProfesores(): boolean {
        return this.estaCargando('cargarProfesores') || (this.tabBootstrapping() && !this.resourcesLoaded().profesores);
    }

    cargandoListaEstudiantes(): boolean {
        return this.estaCargando('cargarEstudiantes') || (this.tabBootstrapping() && !this.resourcesLoaded().estudiantes);
    }

    cargandoListaMatriculas(): boolean {
        return this.estaCargando('cargarMatriculas') || (this.tabBootstrapping() && !this.resourcesLoaded().matriculas);
    }

    cargandoListaImparticiones(): boolean {
        return this.estaCargando('cargarImparticiones') || (this.tabBootstrapping() && !this.resourcesLoaded().imparticiones);
    }

    cargandoListaHorarios(): boolean {
        return this.estaCargando('cargarHorarios') || (this.tabBootstrapping() && !this.resourcesLoaded().horarios);
    }

    siguientePagina(recurso: PaginacionKeys) {
        const state = this.paginacion()[recurso];

        if ((state.page + 1) * state.pageSize >= state.total) return;

        this.paginacion.update(p => ({
            ...p,
            [recurso]: { ...state, page: state.page + 1 }
        }));

        this.recargarPorRecurso(recurso);
    }

    paginaAnterior(recurso: PaginacionKeys) {
        const state = this.paginacion()[recurso];

        if (state.page === 0) return;

        this.paginacion.update(p => ({
            ...p,
            [recurso]: { ...state, page: state.page - 1 }
        }));

        this.recargarPorRecurso(recurso);
    }

    private recargarPorRecurso(recurso: PaginacionKeys) {
        switch (recurso) {
            case 'profesores': void this.cargarProfesores(true); break;
            case 'estudiantes': void this.cargarEstudiantes(true); break;
            case 'matriculas': void this.cargarMatriculas(true); break;
            case 'imparticiones': void this.cargarImparticiones(true); break;
        }
    }

    private resetPagina(recurso: PaginacionKeys): void {
        this.paginacion.update(p => ({
            ...p,
            [recurso]: {
                ...p[recurso],
                page: 0
            }
        }));
    }

    // #region Forms
    private readonly forms: AdminManagementForms = createAdminManagementForms(this.fb);
    readonly cursoForm = this.forms.cursoForm;
    readonly editCursoForm = this.forms.editCursoForm;
    readonly asignaturaForm = this.forms.asignaturaForm;
    readonly editAsignaturaForm = this.forms.editAsignaturaForm;
    readonly profesorForm = this.forms.profesorForm;
    readonly editProfesorForm = this.forms.editProfesorForm;
    readonly estudianteForm = this.forms.estudianteForm;
    readonly editEstudianteForm = this.forms.editEstudianteForm;

    // #endregion
    // #region Computed
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
        let result = this.profesores() ?? [];

        const q = this.busquedaProfesores().trim().toLowerCase();
        if (q) {
            result = result.filter(p =>
                p.nombre.toLowerCase().includes(q) ||
                p.correo.toLowerCase().includes(q)
            );
        }

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

    horariosVista = computed<AdminHorarioAsignaturaItem[]>(() => {
        const cursoFiltroRaw = this.filtroHorariosCursoId();
        const cursoFiltro = cursoFiltroRaw ? Number(cursoFiltroRaw) : null;
        return this.horarios()
            .filter(horario => !cursoFiltro || horario.cursoId === cursoFiltro)
            .sort((a, b) => a.curso.localeCompare(b.curso)
                || a.asignatura.localeCompare(b.asignatura)
                || a.diaSemana - b.diaSemana
                || a.horaInicio.localeCompare(b.horaInicio));
    });

    ngOnInit(): void {
        this.configurarValidadoresDocumento();
        this.configurarBloqueoReenvioEstudiante();
        this.configurarDebounceBusquedas();
        void this.cargarTab(this.tabActiva());
    }

    private configurarValidadoresDocumento(): void {
        this.profesorForm.controls.dni.addValidators(this.documentoDisponibleValidator(() => this.editandoProfesorId, () => this.editandoEstudianteId));
        this.editProfesorForm.controls.dni.addValidators(this.documentoDisponibleValidator(() => this.editandoProfesorId, () => this.editandoEstudianteId));
        this.editEstudianteForm.controls.dni.addValidators(this.documentoDisponibleValidator(() => this.editandoProfesorId, () => this.editandoEstudianteId));
        this.actualizarValidacionDocumentos();
    }

    private configurarBloqueoReenvioEstudiante(): void {
        this.estudianteForm.valueChanges
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => {
                if (this.bloquearCrearEstudianteHastaCambio()) {
                    this.bloquearCrearEstudianteHastaCambio.set(false);
                }
            });
    }

    private documentoDisponibleValidator(getProfesorExcluidoId: () => number | null, getEstudianteExcluidoId: () => number | null): ValidatorFn {
        return (control: AbstractControl): ValidationErrors | null => {
            const documento = normalizeDniInput(control.value);
            if (!documento) {
                return null;
            }

            const profesorExcluidoId = getProfesorExcluidoId();
            const estudianteExcluidoId = getEstudianteExcluidoId();

            const existeProfesor = this.profesores().some(profesor => profesor.id !== profesorExcluidoId && normalizeDniInput(profesor.dni) === documento);
            const existeEstudiante = this.estudiantes().some(estudiante => estudiante.id !== estudianteExcluidoId && normalizeDniInput(estudiante.dni) === documento);

            return existeProfesor || existeEstudiante ? { duplicateDni: true } : null;
        };
    }

    private actualizarValidacionDocumentos(): void {
        this.profesorForm.controls.dni.updateValueAndValidity({ emitEvent: false });
        this.editProfesorForm.controls.dni.updateValueAndValidity({ emitEvent: false });
        this.editEstudianteForm.controls.dni.updateValueAndValidity({ emitEvent: false });
    }

    puedeCrearEstudiante(): boolean {
        return this.estudianteForm.valid && !this.bloquearCrearEstudianteHastaCambio();
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
        this.tabBootstrapping.set(true);
        try {
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
                    // #region Import tab bootstrap
                    break;
                    // #endregion
                case 'horarios':
                    await Promise.all([
                        this.cargarCursos(force),
                        this.cargarAsignaturas(force),
                        this.cargarHorarios(force)
                    ]);
                    break;
            }
        } finally {
            this.tabBootstrapping.set(false);
        }
    }

    private setResourceLoaded(resource: 'cursos' | 'asignaturas' | 'profesores' | 'estudiantes' | 'matriculas' | 'imparticiones' | 'horarios', loaded: boolean): void {
        this.resourcesLoaded.update(current => ({ ...current, [resource]: loaded }));
    }

    private async cargarHorarios(force = false): Promise<void> {
        if (!force && this.resourcesLoaded().horarios) {
            return;
        }

        await this.runWithLoading('cargarHorarios', async () => {
            try {
                this.horarios.set(await this.api.getAdminHorarios());
                this.setResourceLoaded('horarios', true);
            } catch (e) {
                this.mostrarError(e, 'No se pudieron cargar los horarios.');
            }
        });
    }

    private async cargarMatriculas(force = false): Promise<void> {
        if (!force && this.resourcesLoaded().matriculas) return;

        const { page, pageSize } = this.paginacion().matriculas;

        await this.runWithLoading('cargarMatriculas', async () => {
            try {
                const result = await this.api.getAdminMatriculas(page, pageSize);

                this.matriculas.set(result.items ?? []);

                this.paginacion.update(p => ({
                    ...p,
                    matriculas: {
                        ...p.matriculas,
                        total: result.total
                    }
                }));

                this.setResourceLoaded('matriculas', true);

            } catch (e) {
                this.mostrarError(e, 'No se pudieron cargar las matriculas.');
            }
        });
    }

    private async cargarImparticiones(force = false): Promise<void> {
        if (!force && this.resourcesLoaded().imparticiones) return;

        const { page, pageSize } = this.paginacion().imparticiones;

        await this.runWithLoading('cargarImparticiones', async () => {
            try {
                const result = await this.api.getAdminImparticiones(page, pageSize);

                this.imparticiones.set(result.items ?? []);

                this.paginacion.update(p => ({
                    ...p,
                    imparticiones: {
                        ...p.imparticiones,
                        total: result.total
                    }
                }));

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
        if (!force && this.resourcesLoaded().profesores) return;

        const { page, pageSize } = this.paginacion().profesores;

        await this.runWithLoading('cargarProfesores', async () => {
            try {
                const result = await this.api.getProfesores(page, pageSize);

                this.profesores.set(result.items ?? []);

                this.paginacion.update(p => ({
                    ...p,
                    profesores: {
                        ...p.profesores,
                        total: result.total
                    }
                }));

                this.setResourceLoaded('profesores', true);
                this.actualizarValidacionDocumentos();
            } catch (e) {
                this.mostrarError(e, 'No se pudieron cargar los profesores.');
            }
        });
    }

    private async cargarEstudiantes(force = false): Promise<void> {
        if (!force && this.resourcesLoaded().estudiantes) return;

        const { page, pageSize } = this.paginacion().estudiantes;

        await this.runWithLoading('cargarEstudiantes', async () => {
            try {
                const result = await this.api.getEstudiantes(page, pageSize);

                this.estudiantes.set(result.items ?? []);

                this.paginacion.update(p => ({
                    ...p,
                    estudiantes: {
                        ...p.estudiantes,
                        total: result.total
                    }
                }));

                this.setResourceLoaded('estudiantes', true);
                this.actualizarValidacionDocumentos();

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
            this.setResourceLoaded('horarios', false);
            return;
        }

        if (entidad === 'asignaturas') {
            this.setResourceLoaded('asignaturas', false);
            this.setResourceLoaded('matriculas', false);
            this.setResourceLoaded('imparticiones', false);
            this.setResourceLoaded('horarios', false);
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

        if (entidad === 'horarios') {
            this.setResourceLoaded('asignaturas', false);
            this.setResourceLoaded('horarios', false);
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
        this.editandoHorarioId = null;
        this.editCursoForm.reset({ nombre: '' });
        this.editAsignaturaForm.reset({ nombre: '', cursoId: null });
        this.editProfesorForm.reset({ nombre: '', apellidos: '', dni: '', telefono: '', especialidad: '' });
        this.editEstudianteForm.reset({ nombre: '', apellidos: '', dni: '', telefono: '', fechaNacimiento: '', cursoId: null });
        this.bloquearCrearEstudianteHastaCambio.set(false);
        this.actualizarValidacionDocumentos();
        this.resetHorarioForm();
    }

    // #endregion
    // #region CRUD: Cursos
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
                this.setResourceLoaded('horarios', false);
                await this.cargarTab(this.tabActiva(), true);
                this.dataChanged.emit();
            } catch (e) {
                this.mostrarError(e);
            }
        });
    }

    // #endregion
    // #region CRUD: Asignaturas
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
                this.setResourceLoaded('horarios', false);
                await this.cargarTab(this.tabActiva(), true);
                this.dataChanged.emit();
            } catch (e) {
                this.mostrarError(e);
            }
        });
    }

    // #endregion
    // #region CRUD: Profesores
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
                    dni: normalizeDniInput(this.profesorForm.value.dni),
                    telefono: normalizePhoneInput(this.profesorForm.value.telefono),
                    especialidad: (this.profesorForm.value.especialidad ?? '').trim()
                });
                this.profesores.set([...this.profesores(), p]);
                this.actualizarValidacionDocumentos();
                this.profesorForm.reset({ nombre: '', apellidos: '', dni: '', telefono: '', especialidad: '' });
                this.toast.show(`Profesor "${p.nombre}" creado. Correo: ${p.correo}. Clave temporal: ${p.contrasenaTemporal ?? 'no disponible'}`, 'success');
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
            especialidad: p.especialidad
        });
        this.actualizarValidacionDocumentos();
    }

    async guardarProfesor(): Promise<void> {
        if (!this.editandoProfesorId || this.editProfesorForm.invalid) return;

        const data: UpdateProfesorData = {
            nombre: (this.editProfesorForm.value.nombre ?? '').trim(),
            apellidos: (this.editProfesorForm.value.apellidos ?? '').trim(),
            dni: normalizeDniInput(this.editProfesorForm.value.dni),
            telefono: normalizePhoneInput(this.editProfesorForm.value.telefono),
            especialidad: (this.editProfesorForm.value.especialidad ?? '').trim()
        };

        await this.runWithLoading('guardarProfesor', async () => {
            try {
                const updated = await this.api.updateProfesor(this.editandoProfesorId!, data);
                this.profesores.update(list => list.map(p => p.id === updated.id ? updated : p));
                this.editandoProfesorId = null;
                this.actualizarValidacionDocumentos();
                this.toast.show('Profesor actualizado.', 'success');
            } catch (e) {
                this.mostrarError(e);
            }
        });
    }

    async eliminarProfesor(id: number, nombre: string): Promise<void> {
        const imparticiones = this.profesores().find(p => p.id === id)?.imparticionesCount ?? 0;
        const confirmado = await this.confirmDialog.show(
            'Eliminar profesor',
            `¿Eliminar al profesor "${nombre}"? Tiene ${imparticiones} imparticiones asignadas y se eliminaran sus tareas.`
        );
        if (!confirmado) return;

        await this.runWithLoading('eliminarProfesor', async () => {
            try {
                await this.api.deleteProfesor(id);
                this.profesores.update(list => list.filter(p => p.id !== id));
                this.actualizarValidacionDocumentos();
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

    // #endregion
    // #region CRUD: Estudiantes
    async crearEstudiante(): Promise<void> {
        if (this.estudianteForm.invalid) {
            this.toast.show('Completa todos los campos del estudiante y revisa DNI/telefono/fecha.', 'warning');
            return;
        }

        const cursoIdSeleccionado = Number(this.estudianteForm.value.cursoId);

        await this.runWithLoading('crearEstudiante', async () => {
            try {
                const e = await this.api.createEstudiante({
                    nombre: (this.estudianteForm.value.nombre ?? '').trim(),
                    apellidos: (this.estudianteForm.value.apellidos ?? '').trim(),
                    dni: normalizeDniInput(this.estudianteForm.value.dni),
                    telefono: normalizePhoneInput(this.estudianteForm.value.telefono),
                    fechaNacimiento: (this.estudianteForm.value.fechaNacimiento ?? '').trim(),
                    cursoId: cursoIdSeleccionado
                });
                this.estudiantes.set([...this.estudiantes(), e]);
                this.actualizarValidacionDocumentos();
                this.estudianteForm.markAsPristine();
                this.estudianteForm.markAsUntouched();
                this.bloquearCrearEstudianteHastaCambio.set(true);
                this.toast.show(`Estudiante "${e.nombre}" creado. Correo: ${e.correo}. Clave temporal: ${e.contrasenaTemporal ?? 'no disponible'}`, 'success');
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
            cursoId: e.cursoId
        });
        this.actualizarValidacionDocumentos();
    }

    async guardarEstudiante(): Promise<void> {
        if (!this.editandoEstudianteId || this.editEstudianteForm.invalid) return;

        const data: UpdateEstudianteData = {
            nombre: (this.editEstudianteForm.value.nombre ?? '').trim(),
            apellidos: (this.editEstudianteForm.value.apellidos ?? '').trim(),
            dni: normalizeDniInput(this.editEstudianteForm.value.dni),
            telefono: normalizePhoneInput(this.editEstudianteForm.value.telefono),
            fechaNacimiento: (this.editEstudianteForm.value.fechaNacimiento ?? '').trim(),
            cursoId: Number(this.editEstudianteForm.value.cursoId)
        };

        await this.runWithLoading('guardarEstudiante', async () => {
            try {
                const updated = await this.api.updateEstudiante(this.editandoEstudianteId!, data);
                this.estudiantes.update(list => list.map(e => e.id === updated.id ? updated : e));
                this.editandoEstudianteId = null;
                this.actualizarValidacionDocumentos();
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
                this.actualizarValidacionDocumentos();
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

    puedeExportarTabActual(): boolean {
        const tab = this.getExportableTabActiva();
        return this.getExcelRowsForTab(tab).length > 0;
    }

    cargandoTabActual(): boolean {
        const tab = this.getExportableTabActiva();
        switch (tab) {
            case 'cursos': return this.cargandoListaCursos();
            case 'asignaturas': return this.cargandoListaAsignaturas();
            case 'profesores': return this.cargandoListaProfesores();
            case 'estudiantes': return this.cargandoListaEstudiantes();
            case 'matriculas': return this.cargandoListaMatriculas();
            case 'imparticiones': return this.cargandoListaImparticiones();
            case 'horarios': return this.cargandoListaHorarios();
        }
    }

    async recargarTabActual(): Promise<void> {
    const tab = this.tabActiva();

    switch (tab) {
        case 'profesores':
            this.resetPagina('profesores');
            break;
        case 'estudiantes':
            this.resetPagina('estudiantes');
            break;
        case 'matriculas':
            this.resetPagina('matriculas');
            break;
        case 'imparticiones':
            this.resetPagina('imparticiones');
            break;
    }

        await this.cargarTab(tab, true);
    }

    exportarTabActualExcel(): void {
        const tab = this.getExportableTabActiva();
        const rows = this.getExcelRowsForTab(tab);
        if (rows.length === 0) {
            this.toast.show('No hay datos para exportar en la pestaña actual.', 'warning');
            return;
        }

        const worksheet = XLSX.utils.json_to_sheet(rows);
        const headers = Object.keys(rows[0]);
        const range = XLSX.utils.encode_range({
            s: { r: 0, c: 0 },
            e: { r: rows.length, c: Math.max(headers.length - 1, 0) }
        });
        worksheet['!autofilter'] = { ref: range };

        const workbook = XLSX.utils.book_new();
        const sheetName = this.getExcelSheetName(tab);
        XLSX.utils.book_append_sheet(workbook, worksheet, sheetName);
        const fileName = `${this.getExcelFileBaseName(tab)}_${new Date().toISOString().slice(0, 10)}.xlsx`;
        XLSX.writeFile(workbook, fileName);

        void this.api.registrarExportacionExcel(tab, rows.length, fileName)
            .catch(() => {
                this.toast.show('El Excel se exporto, pero no se pudo registrar el log en el servidor.', 'warning');
            });

        this.toast.show(`Exportacion Excel completada para ${sheetName}.`, 'success');
    }

    // #endregion
    // #region Matriculas
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

    // #endregion
    // #region Imparticiones
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

                        const yaExiste = false;
                        if (yaExiste) {
                            return profesor;
                        }

                        return {
                            ...profesor,
                            imparticionesCount: profesor.imparticionesCount + 1
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
                        imparticionesCount: p.imparticionesCount - 1
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

    // #endregion
    // #region Horarios
    async crearHorario(): Promise<void> {
        const asignaturaId = Number(this.horarioAsignaturaId());
        const diaSemana = Number(this.horarioDiaSemana());
        const horaInicio = this.horarioHoraInicio().trim();
        const horaFin = this.horarioHoraFin().trim();
        const aula = this.horarioAula().trim();

        if (!asignaturaId || !horaInicio || !horaFin) {
            this.toast.show('Selecciona asignatura y horas de inicio/fin.', 'warning');
            return;
        }

        await this.runWithLoading('crearHorario', async () => {
            try {
                const created = await this.api.createAdminHorario(asignaturaId, diaSemana, horaInicio, horaFin, aula || null);
                this.horarios.update(list => [...list, created]);
                this.resetHorarioForm();
                this.toast.show('Horario creado.', 'success');
            } catch (e) {
                this.mostrarError(e);
            }
        });
    }

    iniciarEditarHorario(horario: AdminHorarioAsignaturaItem): void {
        this.editandoHorarioId = horario.horarioId;
        this.horarioAsignaturaId.set(horario.asignaturaId);
        this.horarioDiaSemana.set(horario.diaSemana);
        this.horarioHoraInicio.set(horario.horaInicio);
        this.horarioHoraFin.set(horario.horaFin);
        this.horarioAula.set(horario.aula ?? '');
    }

    async guardarHorario(): Promise<void> {
        if (!this.editandoHorarioId) {
            return;
        }

        const asignaturaId = Number(this.horarioAsignaturaId());
        const diaSemana = Number(this.horarioDiaSemana());
        const horaInicio = this.horarioHoraInicio().trim();
        const horaFin = this.horarioHoraFin().trim();
        const aula = this.horarioAula().trim();

        if (!asignaturaId || !horaInicio || !horaFin) {
            this.toast.show('Selecciona asignatura y horas de inicio/fin.', 'warning');
            return;
        }

        await this.runWithLoading('guardarHorario', async () => {
            try {
                const updated = await this.api.updateAdminHorario(this.editandoHorarioId!, asignaturaId, diaSemana, horaInicio, horaFin, aula || null);
                this.horarios.update(list => list.map(item => item.horarioId === updated.horarioId ? updated : item));
                this.editandoHorarioId = null;
                this.resetHorarioForm();
                this.toast.show('Horario actualizado.', 'success');
            } catch (e) {
                this.mostrarError(e);
            }
        });
    }

    async eliminarHorario(horarioId: number, asignatura: string, diaSemana: number, horaInicio: string): Promise<void> {
        const confirmado = await this.confirmDialog.show(
            'Eliminar horario',
            `¿Eliminar el horario de "${asignatura}" del dia ${this.diaSemanaLabel(diaSemana)} a las ${horaInicio}?`
        );
        if (!confirmado) return;

        await this.runWithLoading('eliminarHorario', async () => {
            try {
                await this.api.deleteAdminHorario(horarioId);
                this.horarios.update(list => list.filter(item => item.horarioId !== horarioId));
                this.toast.show('Horario eliminado.', 'success');
            } catch (e) {
                this.mostrarError(e);
            }
        });
    }

    resetHorarioForm(): void {
        this.horarioAsignaturaId.set(null);
        this.horarioDiaSemana.set(1);
        this.horarioHoraInicio.set('08:30');
        this.horarioHoraFin.set('09:25');
        this.horarioAula.set('');
    }

    diaSemanaLabel(diaSemana: number): string {
        const labels: Record<number, string> = {
            1: 'Lunes',
            2: 'Martes',
            3: 'Miercoles',
            4: 'Jueves',
            5: 'Viernes'
        };

        return labels[diaSemana] ?? 'N/A';
    }

    // #endregion
    // #region CSV Import
    async onCsvFileChange(event: Event, entidad: CsvImportEntity): Promise<void> {
        const input = event.target as HTMLInputElement;
        const file = input.files?.[0] ?? null;

        if (!file) return;

        if (file.size > MAX_CSV_FILE_SIZE_BYTES) {
            this.setCsvFile(entidad, null);
            input.value = '';
            this.toast.show('El archivo CSV no puede superar 10 MB.', 'warning');
            return;
        }

        const contenido = await file.text();
        const primeraLinea = contenido.split('\n')[0].trim();
        const headers = primeraLinea.split(',').map(h => h.trim());

        const { valido } = validarHeadersCsv(entidad, headers);

        if (!valido) {
            this.setCsvFile(entidad, null);
            input.value = '';
            this.toast.show('El formato es incorrecto, mira la plantilla.', 'error');
            this.nombreEntidadCsv.set(entidad.toString());
            await this.api.registrarErrorCsv(entidad, 'Formato incorrecto');
            return;
        }

        this.setCsvFile(entidad, file);
        this.toast.show(`Archivo ${file.name} preparado para importar ${entidad}.`, 'info');
    }

    async importarCsv(entidad: CsvImportEntity): Promise<void> {
        const file = this.getCsvFile(entidad);

        if (!file) {
            this.toast.show('Selecciona un archivo CSV.', 'error');
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

                const severity = resultado.errores.length > 0 ? 'warning' : 'success';
                this.toast.show(this.construirResumenImportacion(entidad, resultado), severity);

                this.invalidateForImport(entidad);
                await this.cargarTab(this.tabActiva(), true);
                this.dataChanged.emit();
            } catch (e) {
                if (e instanceof CsvImportError) {
                    if (e.result) {
                        this.csvResultado.set(e.result);
                        this.toast.show(this.construirResumenImportacion(entidad, e.result), 'error');
                    } else {
                        this.toast.show(e.message, 'error');
                    }
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
            : entidad === 'horarios' ? this.csvHorariosFile
            : entidad === 'matriculas' ? this.csvMatriculasFile
            : entidad === 'imparticiones' ? this.csvImparticionesFile
            : this.csvNotasFile;
    }

    private getExportableTabActiva(): Exclude<AdminTab, 'importar'> {
        return this.tabActiva() as Exclude<AdminTab, 'importar'>;
    }

    private setCsvFile(entidad: CsvImportEntity, file: File | null): void {
        if (entidad === 'cursos') this.csvCursosFile = file;
        else if (entidad === 'asignaturas') this.csvAsignaturasFile = file;
        else if (entidad === 'profesores') this.csvProfesoresFile = file;
        else if (entidad === 'estudiantes') this.csvEstudiantesFile = file;
        else if (entidad === 'tareas') this.csvTareasFile = file;
        else if (entidad === 'horarios') this.csvHorariosFile = file;
        else if (entidad === 'matriculas') this.csvMatriculasFile = file;
        else if (entidad === 'imparticiones') this.csvImparticionesFile = file;
        else this.csvNotasFile = file;
    }

    private clearCsvSelection(entidad: CsvImportEntity): void {
        this.setCsvFile(entidad, null);
    }

    private construirResumenImportacion(entidad: CsvImportEntity, result: CsvImportResult): string {
        const partes = [
            `Importacion de ${entidad}: ${result.creados} creados`,
            `${result.omitidos ?? 0} omitidos`,
            `${result.errores.length} errores`
        ];

        if (result.errores.length > 0) {
            partes.push(`Primer error: ${result.errores[0]}`);
        }

        return partes.join('. ') + '.';
    }

    private extraerDetalleOmitidos(result: CsvImportResult): string[] {
        if (!result.detalles?.length || (result.omitidos ?? 0) <= 0) {
            return [];
        }

        const detalladosComoOmitidos = result.detalles.filter(det => this.esDetalleDeOmitido(det));
        if (detalladosComoOmitidos.length > 0) {
            return detalladosComoOmitidos;
        }

        if (result.detalles.length <= (result.omitidos ?? 0)) {
            return result.detalles.map(det => `${det} (omitido por duplicado o existente)`);
        }

        return result.detalles.slice(0, result.omitidos ?? 0).map(det => `${det} (omitido por duplicado o existente)`);
    }

    private esDetalleDeOmitido(detalle: string): boolean {
        const value = detalle.toLowerCase();
        return value.includes('omitid')
            || value.includes('ya existe')
            || value.includes('duplicad')
            || value.includes('ya esta')
            || value.includes('ya tiene un profesor asignado');
    }

    descargarPlantilla(entidad: CsvImportEntity): void {
        const csv = CSV_PLANTILLAS[entidad];
        const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `plantilla_${entidad}.csv`;
        link.click();
        URL.revokeObjectURL(url);
    }
    // #endregion
    // #region Excel exports
    private getExcelRowsForTab(tab: Exclude<AdminTab, 'importar'>): Record<string, string | number>[] {
        const excludeKeys = new Set(['contrasenaTemporal']);

        if (tab === 'horarios') {
            return this.horariosVista()
                .map(item => ({ ...item, diaSemana: this.diaSemanaLabel(item.diaSemana) }))
                .map(item => this.flattenForExcel(item as Record<string, unknown>, excludeKeys));
        }

        return this.getTabVista(tab)
            .map(item => this.flattenForExcel(item as Record<string, unknown>, excludeKeys));
    }

    private getTabVista(tab: Exclude<AdminTab, 'importar'>): unknown[] {
        switch (tab) {
            case 'cursos': return this.cursosVista();
            case 'asignaturas': return this.asignaturasVista();
            case 'profesores': return this.profesoresVista();
            case 'estudiantes': return this.estudiantesVista();
            case 'matriculas': return this.matriculasVista();
            case 'imparticiones': return this.imparticionesVista();
            case 'horarios': return this.horariosVista();
            default: return [];
        }
    }

    private flattenForExcel(
        obj: Record<string, unknown>,
        excludeKeys: Set<string> = new Set()
    ): Record<string, string | number> {
        const result: Record<string, string | number> = {};

        for (const [key, value] of Object.entries(obj)) {
            if (excludeKeys.has(key)) continue;

            const label = key.charAt(0).toUpperCase() + key.slice(1);

            if (value === null || value === undefined) {
                result[label] = '';
            } else if (Array.isArray(value)) {
                if (value.length === 0) {
                    result[label] = 0;
                } else if (typeof value[0] === 'object' && value[0] !== null) {
                    const items = value as Record<string, unknown>[];
                    const names = items.map(item => {
                        const display = item['nombre'] ?? item['asignatura'] ?? item['curso'];
                        return display !== undefined
                            ? String(display)
                            : (Object.values(item).find(v => typeof v === 'string') as string | undefined) ?? '';
                    });
                    result[label] = names.join(' | ');
                } else {
                    result[label] = (value as (string | number)[]).join(', ');
                }
            } else if (typeof value === 'object') {
                const nested = value as Record<string, unknown>;
                const display = nested['nombre'] ?? nested['name'];
                if (display !== undefined) {
                    result[label] = String(display);
                } else {
                    for (const [nKey, nVal] of Object.entries(nested)) {
                        if (excludeKeys.has(nKey) || nKey === 'id') continue;
                        const nLabel = `${label}_${nKey.charAt(0).toUpperCase() + nKey.slice(1)}`;
                        result[nLabel] = nVal !== null && nVal !== undefined
                            ? (typeof nVal === 'number' ? nVal : String(nVal)) : '';
                    }
                }
            } else {
                result[label] = value as string | number;
            }
        }

        return result;
    }

    private getExcelSheetName(tab: Exclude<AdminTab, 'importar'>): string {
        const names: Record<Exclude<AdminTab, 'importar'>, string> = {
            cursos: 'Cursos',
            asignaturas: 'Asignaturas',
            profesores: 'Profesores',
            estudiantes: 'Estudiantes',
            matriculas: 'Matriculas',
            imparticiones: 'Imparticiones',
            horarios: 'Horarios'
        };

        return names[tab];
    }

    private getExcelFileBaseName(tab: Exclude<AdminTab, 'importar'>): string {
        const names: Record<Exclude<AdminTab, 'importar'>, string> = {
            cursos: 'admin-cursos',
            asignaturas: 'admin-asignaturas',
            profesores: 'admin-profesores',
            estudiantes: 'admin-estudiantes',
            matriculas: 'admin-matriculas',
            imparticiones: 'admin-imparticiones',
            horarios: 'admin-horarios'
        };

        return names[tab];
    }

    // #endregion
    // #region Task Management
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

    // #endregion
}
