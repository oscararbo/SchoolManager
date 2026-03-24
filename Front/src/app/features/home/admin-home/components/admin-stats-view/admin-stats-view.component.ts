import { Component, OnInit, computed, inject, signal, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SchoolApiService, AdminStats, AdminNotasStats, AdminCursoNotasStats } from '../../../../../shared/services/school-api.service';
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
    private cursosChartRef?: ElementRef<HTMLCanvasElement>;
    private totalesChartRef?: ElementRef<HTMLCanvasElement>;
    private rendimientoChartRef?: ElementRef<HTMLCanvasElement>;

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

    private api = inject(SchoolApiService);
    private toast = inject(ToastService);

    stats = signal<AdminStats | null>(null);
    notasStats = signal<AdminNotasStats | null>(null);
    statsEstado = signal<'idle' | 'loading' | 'ready' | 'error'>('idle');
    cursoNotasActivoId = signal<number | null>(null);
    private cursosFallback = signal<string[]>([]);
    private vistaChartsVisible = signal(false);
    chartsRenderizados = signal(false);

    statsDisponibles = computed(() => this.stats() !== null && this.notasStats() !== null);
    cursoNotasSeleccionado = computed<AdminCursoNotasStats | null>(() => {
        const notas = this.notasStats();
        if (!notas?.porCurso.length) {
            return null;
        }

        const activo = this.cursoNotasActivoId();
        return notas.porCurso.find(curso => curso.cursoId === activo) ?? notas.porCurso[0];
    });

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
            aprobados: curso?.asignaturas.map(asignatura => asignatura.aprobados) ?? []
        };
    });

    notasResumenCards = computed(() => {
        const notas = this.notasStats();
        if (!notas) {
            return [] as Array<{ label: string; value: string; helper: string }>;
        }

        const asignaturas = notas.porCurso.flatMap(curso => curso.asignaturas);
        const alumnos = asignaturas.flatMap(asignatura => asignatura.alumnos);
        const alumnosConNota = alumnos.filter(alumno => alumno.notaFinal !== null);
        const aprobados = alumnosConNota.filter(alumno => alumno.aprobado).length;
        const tasaAprobado = alumnosConNota.length > 0 ? Math.round((aprobados / alumnosConNota.length) * 100) : 0;
        const asignaturaRiesgo = [...asignaturas].sort((a, b) => b.suspensos - a.suspensos || (a.media ?? 99) - (b.media ?? 99))[0];
        const mejorCurso = [...notas.porCurso]
            .filter(curso => curso.media !== null)
            .sort((a, b) => (b.media ?? 0) - (a.media ?? 0))[0];

        return [
            {
                label: 'Media global',
                value: this.formatNota(notas.mediaGlobal),
                helper: 'Promedio final agregado de todas las asignaturas evaluadas.'
            },
            {
                label: 'Tasa de aprobado',
                value: `${tasaAprobado}%`,
                helper: `${aprobados} de ${alumnosConNota.length} medias finales aprobadas.`
            },
            {
                label: 'Curso con mejor media',
                value: mejorCurso?.curso ?? '-',
                helper: mejorCurso ? `Media ${this.formatNota(mejorCurso.media)}` : 'Todavia no hay medias finales cerradas.'
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

    private cursosChart?: Chart;
    private totalesChart?: Chart;
    private rendimientoChart?: Chart;

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

    seleccionarCursoNotas(cursoId: string): void {
        this.cursoNotasActivoId.set(Number(cursoId));
        this.programarRenderCharts();
    }

    trackByCurso(_: number, curso: AdminCursoNotasStats): number {
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
            const [stats, notasStats] = await Promise.all([
                this.api.getAdminStats(),
                this.api.getAdminNotasStats()
            ]);
            await this.prepararFallbackCursos(stats);
            this.stats.set(stats);
            this.notasStats.set(notasStats);

            const cursoActivo = this.cursoNotasActivoId();
            if (!cursoActivo || !notasStats.porCurso.some(curso => curso.cursoId === cursoActivo)) {
                this.cursoNotasActivoId.set(notasStats.porCurso[0]?.cursoId ?? null);
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
            this.cursosChart = undefined;
            this.totalesChart = undefined;
            this.rendimientoChart = undefined;
            this.chartsRenderizados.set(false);
            return;
        }

        this.cursosChart?.destroy();
        this.totalesChart?.destroy();
        this.rendimientoChart?.destroy();

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
                        { label: 'Media final', data: rendimientoData.medias, backgroundColor: '#f59e0b' },
                        { label: 'Aprobados', data: rendimientoData.aprobados, backgroundColor: '#10b981' }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    scales: {
                        y: {
                            beginAtZero: true,
                            suggestedMax: 10
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
