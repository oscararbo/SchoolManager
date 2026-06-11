import { Component, signal, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SchoolApiService } from '../../../../../shared/services/school-api.service';
import { CommonModule } from '@angular/common';
import { Chart, registerables } from 'chart.js';
import { ToastService } from '../../../../../core/services/toast.service';

Chart.register(...registerables);

@Component({
    selector: 'app-superusuario-logs-view',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './superusuario-logs-view.html',
    styleUrls: ['./superusuario-logs-view.scss']
})
export class SuperusuarioLogsViewComponent implements OnInit {

    private api = inject(SchoolApiService);
    private toast = inject(ToastService);

    logs = signal<any[]>([]);
    total = signal(0);
    totalPagesNum = signal(0);

    page = signal(0);
    pageSize = 50;

    level = signal('');
    entity = signal('');
    userEmail = signal('');
    from = signal<string | undefined>(undefined);
    to = signal<string | undefined>(undefined);

    today = new Date();
    maxDate = this.today.toISOString().split('T')[0];

    minDate = new Date(new Date().setFullYear(this.today.getFullYear() - 1))
        .toISOString()
        .split('T')[0];

    chartNoData = signal(false);
    chart: any;
    kpis = signal({ info: 0, warning: 0, error: 0 });

    async ngOnInit() {
        await this.cargar();
        await this.loadTimeline();
    }

    async cargar() {
        if (this.from() && this.to()) {
            const fromDate = new Date(this.from()!);
            const toDate = new Date(this.to()!);

            if (fromDate > toDate) {
                this.toast.show("La fecha 'Desde' no puede ser mayor que 'Hasta'", "error");
                return;
            }
        }

        const params = {
            page: this.page(),
            pageSize: this.pageSize,
            ...this.getFilters()
        };

        const res = await this.api.getLogs(params);

        this.logs.set(res?.items ?? []);
        this.total.set(res?.total ?? 0);
        this.totalPagesNum.set(await this.totalPages());
        this.calculateKpis(res?.items ?? []);
        await this.loadTimeline();
    }

    async next() {
        if(this.page() + 1 >= this.totalPagesNum()) return;
        this.page.update(p => p + 1);
        await this.cargar();
    }

    async prev() {
        if (this.page() === 0) return;
        this.page.update(p => p - 1);
        await this.cargar();
    }

    async totalPages(): Promise<number> {
        const total = this.total();
        return Math.ceil(total / this.pageSize);
    }

    async loadTimeline() {
        const data = await this.api.getLogsTimeline(this.getFilters());
        this.buildChart(data);
    }

    getFilters() {
        const params: any = {
            level: this.level(),
            entity: this.entity(),
            userEmail: this.userEmail()
        };

        if (this.from()) {
            const start = new Date(this.from()!);
            start.setHours(0, 0, 0, 0);
            params.from = start.toISOString();
        }

        if (this.to()) {
            const end = new Date(this.to()!);
            end.setHours(23, 59, 59, 999);
            params.to = end.toISOString();
        }

        return params;
    }

    calculateKpis(logs: any[]) {
        let info = 0, warn = 0, error = 0;

        for (const l of logs) {
            if (l.level === 'Information') info++;
            if (l.level === 'Warning') warn++;
            if (l.level === 'Error') error++;
        }

        this.kpis.set({ info, warning: warn, error });
    }

    buildChart(data: any[]) {
        if (this.chart) this.chart.destroy();

        if (!data || data.length === 0) {
            this.chartNoData.set(true);
            return;
        }

        this.chartNoData.set(false);

        this.chart = new Chart('chartLogs', {
            type: 'line',
            data: {
                labels: data.map(x =>
                    new Date(x.bucket).toLocaleString('es-ES', {
                        day: '2-digit',
                        month: '2-digit',
                        year: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit'
                    }),
                ),
                datasets: [
                    {
                        label: 'Info',
                        data: data.map(x => x.info),
                        borderColor: '#198754',
                        backgroundColor: '#198754'
                    },
                    {
                        label: 'Warning',
                        data: data.map(x => x.warning),
                        borderColor: '#ffc107',
                        backgroundColor: '#ffc107'
                    },
                    {
                        label: 'Error',
                        data: data.map(x => x.error),
                        borderColor: '#dc3545',
                        backgroundColor: '#dc3545'
                    }
                ]
            },
            options: {
                onClick: (event, elements) => {
                    if (!elements.length) return;

                    const index = elements[0].index;
                    const bucket = data[index].bucket;

                    const date = new Date(bucket);
                    const day = date.toISOString().split('T')[0];

                    this.from.set(day);
                    this.to.set(day);
                    this.page.set(0);

                    this.cargar();
                },

                responsive: true,

                interaction: {
                    mode: 'index',
                    intersect: false
                },

                scales: {
                    x: {
                        ticks: {
                            maxRotation: 45,
                            minRotation: 45,
                            autoSkip: false,
                            maxTicksLimit: 10
                        }
                    }
                },

                elements: {
                    point: {
                        radius: 3,
                        hoverRadius: 6
                    },
                    line: {
                        tension: 0.2
                    }
                },

                plugins: {
                    tooltip: {
                        mode: 'index',
                        intersect: false,
                        callbacks: {
                            label: function (context) {
                                return `${context.dataset.label}: ${context.parsed.y}`;
                            }
                        }
                    },
                    legend: {
                        display: true,
                        labels: {
                            usePointStyle: true,
                            pointStyle: 'circle'
                        }
                    }
                }
            }
        });
    }

    async originalFilters(){
        this.level.set('');
        this.entity.set('');
        this.userEmail.set('');
        this.from.set(undefined);
        this.to.set(undefined);
        this.page.set(0);
        await this.cargar();
    }
}
