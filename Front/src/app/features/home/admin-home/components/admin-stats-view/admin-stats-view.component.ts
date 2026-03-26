import { Component, OnInit, computed, inject, signal, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
    SchoolApiService,
    AdminStats,
    AdminCursoNotasStats,
    AdminCursoStatsSelector,
    AdminComparacionCursos
} from '../../../../../shared/services/school-api.service';
import { ToastService } from '../../../../../core/services/toast.service';
import { AdminStatsCardsComponent } from '../admin-stats-cards/admin-stats-cards.component';
import { Chart, registerables } from 'chart.js';

@Component({
    selector: 'app-admin-stats-view',
    standalone: true,
    imports: [CommonModule, AdminStatsCardsComponent],
    templateUrl: './admin-stats-view.component.html',
    styleUrl: './admin-stats-view.component.scss'
})
export class AdminStatsViewComponent implements OnInit, AfterViewInit {
    private static readonly CURSOS_PAGINA = 24;

    private cursosChartRef?: ElementRef<HTMLCanvasElement>;
    private totalesChartRef?: ElementRef<HTMLCanvasElement>;
    private rendimientoChartRef?: ElementRef<HTMLCanvasElement>;
    private comparacionChartRef?: ElementRef<HTMLCanvasElement>;

    @ViewChild('cursosChart')
    set cursosChartCanvas(ref: ElementRef<HTMLCanvasElement> | undefined) {
        this.cursosChartRef = ref;
        this.programarRenderCharts();
    }

    @ViewChild('totalesChart')
    set totalesChartCanvas(ref: ElementRef<HTMLCanvasElement> | undefined) {
        this.totalesChartRef = ref;
        this.programarRenderCharts();
    }

    @ViewChild('rendimientoChart')
    set rendimientoChartCanvas(ref: ElementRef<HTMLCanvasElement> | undefined) {
        this.rendimientoChartRef = ref;
        this.programarRenderCharts();
    }

    @ViewChild('comparacionChart')
    set comparacionChartCanvas(ref: ElementRef<HTMLCanvasElement> | undefined) {
        this.comparacionChartRef = ref;
        this.programarRenderCharts();
    }

    private api = inject(SchoolApiService);
    private toast = inject(ToastService);

    stats = signal<AdminStats | null>(null);
    cursosSelector = signal<AdminCursoStatsSelector[]>([]);
    cursoDetalle = signal<AdminCursoNotasStats | null>(null);
    comparacion = signal<AdminComparacionCursos | null>(null);

    statsEstado = signal<'idle' | 'loading' | 'ready' | 'error'>('idle');
    cursoPendienteId = signal<number | null>(null);
    cursoDetalleCargando = signal(false);
    comparacionCargando = signal(false);
    tabStatsActiva = signal<'curso' | 'comparacion'>('curso');
    filtroCursos = signal('');
    limiteCursosVisibles = signal(AdminStatsViewComponent.CURSOS_PAGINA);

    cursosCompararIds = signal<number[]>([]);

    private cursosFallback = signal<string[]>([]);
    private vistaChartsVisible = signal(false);
    chartsRenderizados = signal(false);

    statsDisponibles = computed(() => this.stats() !== null);
    cursoNotasSeleccionado = computed<AdminCursoNotasStats | null>(() => this.cursoDetalle());
    cursoActivoNombre = computed(() => this.cursoDetalle()?.curso ?? 'Sin curso seleccionado');
    cursosFiltrados = computed(() => {
        const term = this.filtroCursos().trim().toLocaleLowerCase();
        const base = [...this.cursosSelector()].sort((a, b) => a.curso.localeCompare(b.curso));
        if (!term) {
            return base;
        }

        return base.filter(curso =>
            curso.curso.toLocaleLowerCase().includes(term)
            || `${curso.totalEstudiantes}`.includes(term)
            || `${curso.totalAsignaturas}`.includes(term));
    });
    cursosVisibles = computed(() => this.cursosFiltrados().slice(0, this.limiteCursosVisibles()));
    hayMasCursos = computed(() => this.cursosFiltrados().length > this.cursosVisibles().length);

    statsChartData = computed(() => {
        const current = this.stats();
        const labelsFromStats = current?.porCurso.map(x => x.curso) ?? [];
        const labels = labelsFromStats.length > 0 ? labelsFromStats : this.cursosFallback();
        const estudiantes = labelsFromStats.length > 0
            ? (current?.porCurso.map(x => x.estudiantes) ?? [])
            : labels.map(() => 0);
        const asignaturas = labelsFromStats.length > 0
            ? (current?.porCurso.map(x => x.asignaturas) ?? [])
            : labels.map(() => 0);

        return {
            labels,
            estudiantes,
            asignaturas,
            totales: current
                ? [
                    current.totalCursos,
                    current.totalAsignaturas,
                    current.totalProfesores,
                    current.totalEstudiantes,
                    current.totalMatriculas,
                    current.totalTareas
                ]
                : []
        };
    });

    rendimientoChartData = computed(() => {
        const curso = this.cursoNotasSeleccionado();
        return {
            labels: curso?.asignaturas.map(asignatura => asignatura.asignatura) ?? [],
            medias: curso?.asignaturas.map(asignatura => asignatura.media ?? 0) ?? [],
            aprobados: curso?.asignaturas.map(asignatura => asignatura.aprobados) ?? [],
            totalAlumnos: curso?.asignaturas.map(asignatura => asignatura.totalAlumnos) ?? []
        };
    });

    comparacionChartData = computed(() => {
        const data = this.comparacion()?.cursos ?? [];
        return {
            labels: data.map(c => c.curso),
            medias: data.map(c => c.mediaGlobalCurso ?? 0),
            aprobadosPct: data.map(c => {
                const evaluados = c.aprobados + c.suspensos;
                return evaluados > 0 ? Math.round((c.aprobados / evaluados) * 100) : 0;
            }),
            alumnos: data.map(c => c.totalAlumnos)
        };
    });

    notasResumenCards = computed(() => {
        const curso = this.cursoNotasSeleccionado();
        if (!curso) {
            return [] as Array<{ label: string; value: string; helper: string }>;
        }

        const evaluados = curso.aprobados + curso.suspensos;
        const tasaAprobado = evaluados > 0 ? Math.round((curso.aprobados / evaluados) * 100) : 0;
        const asignaturaRiesgo = [...curso.asignaturas]
            .sort((a, b) => b.suspensos - a.suspensos || (a.media ?? 99) - (b.media ?? 99))[0];
        const asignaturaMejor = [...curso.asignaturas]
            .filter(a => a.media !== null)
            .sort((a, b) => (b.media ?? 0) - (a.media ?? 0))[0];

        return [
            {
                label: 'Media del curso',
                value: this.formatNota(curso.mediaGlobalCurso),
                helper: 'Promedio final del curso seleccionado.'
            },
            {
                label: 'Tasa de aprobado',
                value: `${tasaAprobado}%`,
                helper: `${curso.aprobados} aprobados y ${curso.suspensos} suspensos con nota cerrada.`
            },
            {
                label: 'Asignatura destacada',
                value: asignaturaMejor?.asignatura ?? '-',
                helper: asignaturaMejor ? `Media ${this.formatNota(asignaturaMejor.media)}` : 'Todavia no hay notas cerradas.'
            },
            {
                label: 'Asignatura a vigilar',
                value: asignaturaRiesgo?.asignatura ?? '-',
                helper: asignaturaRiesgo ? `${asignaturaRiesgo.suspensos} suspensos y media ${this.formatNota(asignaturaRiesgo.media)}` : 'Sin datos suficientes.'
            }
        ];
    });

    statsTieneDatosGraficables = computed(() => this.statsChartData().labels.length > 0);
    rendimientoTieneDatos = computed(() => this.rendimientoChartData().labels.length > 0);
    comparacionTieneDatos = computed(() => this.comparacionChartData().labels.length > 0);

    private cursosChart?: Chart;
    private totalesChart?: Chart;
    private rendimientoChart?: Chart;
    private comparacionChart?: Chart;

    async ngOnInit(): Promise<void> {
        await this.cargarStats();
    }

    ngAfterViewInit(): void {
        Chart.register(...registerables);
        this.vistaChartsVisible.set(true);
        this.renderStatsCharts();
    }

    async refrescarStats(): Promise<void> {
        await this.cargarStats(true);
    }

    actualizarFiltroCursos(valor: string): void {
        this.filtroCursos.set(valor);
        this.limiteCursosVisibles.set(AdminStatsViewComponent.CURSOS_PAGINA);
    }

    mostrarMasCursos(): void {
        this.limiteCursosVisibles.update(actual => actual + AdminStatsViewComponent.CURSOS_PAGINA);
    }

    async verStatsCurso(cursoId: number): Promise<void> {
        this.cursoPendienteId.set(cursoId);
        await this.verStatsCursoSeleccionado();
    }

    async verStatsCursoSeleccionado(): Promise<void> {
        const cursoId = this.cursoPendienteId();
        if (!cursoId) {
            this.toast.show('Selecciona un curso para ver sus estadisticas.', 'warning');
            return;
        }

        this.cursoDetalleCargando.set(true);
        try {
            this.cursoDetalle.set(await this.api.getAdminStatsByCurso(cursoId));
            this.tabStatsActiva.set('curso');
            this.programarRenderCharts();
        } catch (e) {
            this.mostrarError(e, 'No se pudieron cargar las estadisticas del curso.');
        } finally {
            this.cursoDetalleCargando.set(false);
        }
    }

    toggleCursoComparar(cursoId: number): void {
        this.cursosCompararIds.update(list => {
            if (list.includes(cursoId)) {
                return list.filter(id => id !== cursoId);
            }
            if (list.length >= 6) {
                return list;
            }
            return [...list, cursoId];
        });
    }

    limpiarSeleccionComparacion(): void {
        this.cursosCompararIds.set([]);
    }

    cursoMarcadoComparar(cursoId: number): boolean {
        return this.cursosCompararIds().includes(cursoId);
    }

    async compararCursosSeleccionados(): Promise<void> {
        const ids = this.cursosCompararIds();
        if (ids.length < 2) {
            this.toast.show('Selecciona al menos 2 cursos para comparar.', 'warning');
            return;
        }

        this.comparacionCargando.set(true);
        try {
            this.comparacion.set(await this.api.compararCursos(ids));
            this.tabStatsActiva.set('comparacion');
            this.programarRenderCharts();
        } catch (e) {
            this.mostrarError(e, 'No se pudo obtener la comparacion de cursos.');
        } finally {
            this.comparacionCargando.set(false);
        }
    }

    trackByCurso(_: number, curso: AdminCursoStatsSelector): number {
        return curso.cursoId;
    }

    porcentajeAprobado(aprobados: number, total: number): number {
        return total > 0 ? Math.round((aprobados / total) * 100) : 0;
    }

    formatNota(valor: number | null): string {
        return valor === null ? '-' : valor.toFixed(2);
    }

    private async cargarStats(mostrarToast = false): Promise<void> {
        this.statsEstado.set('loading');
        try {
            const [stats, selector] = await Promise.all([
                this.api.getAdminStats(),
                this.api.getAdminCursosStatsSelector()
            ]);

            await this.prepararFallbackCursos(stats);
            this.stats.set(stats);
            this.cursosSelector.set(selector);

            if (!this.cursoPendienteId() && selector.length > 0) {
                this.cursoPendienteId.set(selector[0].cursoId);
            }

            if (!this.cursoDetalle() && selector.length > 0) {
                this.cursoDetalle.set(await this.api.getAdminStatsByCurso(selector[0].cursoId));
            }

            this.statsEstado.set('ready');
            this.programarRenderCharts();
            if (mostrarToast) {
                this.toast.show('Estadisticas actualizadas.', 'success');
            }
        } catch (e) {
            this.statsEstado.set('error');
            this.mostrarError(e, 'No se pudieron cargar las estadisticas.');
        }
    }

    private programarRenderCharts(): void {
        queueMicrotask(() => this.renderStatsCharts());
    }

    private async prepararFallbackCursos(stats: AdminStats): Promise<void> {
        if (stats.porCurso.length > 0 || stats.totalCursos === 0) {
            this.cursosFallback.set([]);
            return;
        }

        try {
            const cursos = await this.api.getCursos();
            this.cursosFallback.set(cursos.map(curso => curso.nombre));
        } catch {
            this.cursosFallback.set([]);
        }
    }

    private renderStatsCharts(): void {
        if (!this.vistaChartsVisible() || !this.cursosChartRef || !this.totalesChartRef) {
            return;
        }

        const chartData = this.statsChartData();
        const rendimientoData = this.rendimientoChartData();
        if (chartData.labels.length === 0) {
            this.cursosChart?.destroy();
            this.totalesChart?.destroy();
            this.rendimientoChart?.destroy();
            this.comparacionChart?.destroy();
            this.cursosChart = undefined;
            this.totalesChart = undefined;
            this.rendimientoChart = undefined;
            this.comparacionChart = undefined;
            this.chartsRenderizados.set(false);
            return;
        }

        this.cursosChart?.destroy();
        this.totalesChart?.destroy();
        this.rendimientoChart?.destroy();
        this.comparacionChart?.destroy();

        this.cursosChart = new Chart(this.cursosChartRef.nativeElement, {
            type: 'bar',
            data: {
                labels: chartData.labels,
                datasets: [
                    { label: 'Estudiantes', data: chartData.estudiantes, backgroundColor: '#0d6efd' },
                    { label: 'Asignaturas', data: chartData.asignaturas, backgroundColor: '#20c997' }
                ]
            },
            options: { responsive: true, maintainAspectRatio: false }
        });

        this.totalesChart = new Chart(this.totalesChartRef.nativeElement, {
            type: 'doughnut',
            data: {
                labels: ['Cursos', 'Asignaturas', 'Profesores', 'Estudiantes', 'Matriculas', 'Tareas'],
                datasets: [{
                    data: chartData.totales,
                    backgroundColor: ['#0d6efd', '#198754', '#fd7e14', '#dc3545', '#6f42c1', '#20c997']
                }]
            },
            options: { responsive: true, maintainAspectRatio: false }
        });

        if (this.rendimientoChartRef && rendimientoData.labels.length > 0) {
            this.rendimientoChart = new Chart(this.rendimientoChartRef.nativeElement, {
                type: 'bar',
                data: {
                    labels: rendimientoData.labels,
                    datasets: [
                        {
                            label: 'Media final',
                            data: rendimientoData.medias,
                            type: 'line',
                            borderColor: '#f59e0b',
                            backgroundColor: '#f59e0b',
                            tension: 0.25,
                            yAxisID: 'y'
                        },
                        {
                            label: 'Aprobados',
                            data: rendimientoData.aprobados,
                            backgroundColor: '#10b981',
                            yAxisID: 'y1'
                        },
                        {
                            label: 'Total alumnos',
                            data: rendimientoData.totalAlumnos,
                            backgroundColor: '#93c5fd',
                            yAxisID: 'y1'
                        }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    scales: {
                        y: {
                            beginAtZero: true,
                            suggestedMax: 10,
                            title: { display: true, text: 'Media' }
                        },
                        y1: {
                            beginAtZero: true,
                            position: 'right',
                            grid: { drawOnChartArea: false },
                            title: { display: true, text: 'Alumnos' }
                        }
                    }
                }
            });
        }

        const comparacionData = this.comparacionChartData();
        if (this.comparacionChartRef && comparacionData.labels.length > 0) {
            this.comparacionChart = new Chart(this.comparacionChartRef.nativeElement, {
                type: 'bar',
                data: {
                    labels: comparacionData.labels,
                    datasets: [
                        {
                            label: 'Media global',
                            data: comparacionData.medias,
                            backgroundColor: '#2563eb',
                            yAxisID: 'y'
                        },
                        {
                            label: 'Tasa aprobado %',
                            data: comparacionData.aprobadosPct,
                            type: 'line',
                            borderColor: '#16a34a',
                            backgroundColor: '#16a34a',
                            tension: 0.2,
                            yAxisID: 'y1'
                        }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    scales: {
                        y: {
                            beginAtZero: true,
                            suggestedMax: 10,
                            title: { display: true, text: 'Media' }
                        },
                        y1: {
                            beginAtZero: true,
                            suggestedMax: 100,
                            position: 'right',
                            grid: { drawOnChartArea: false },
                            title: { display: true, text: 'Aprobado %' }
                        }
                    }
                }
            });
        }

        this.chartsRenderizados.set(true);
    }

    private mostrarError(error: unknown, fallback = 'No se pudo completar la operacion.'): void {
        const message = error instanceof Error ? error.message : fallback;
        this.toast.show(message || fallback, 'error');
    }
}
