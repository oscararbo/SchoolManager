import { Component, signal, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SchoolApiService } from '../../../../../shared/services/school-api.service';
import { CommonModule } from '@angular/common';
import { Chart, registerables } from 'chart.js';

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

    logs = signal<any[]>([]);
    total = signal(0);

    page = signal(0);
    pageSize = 50;

    query = signal('');
    level = signal('');
    entity = signal('');

    chart: any;

    async ngOnInit() {
        await this.cargar();
        await this.loadTimeline();
    }

    async cargar() {
        const res = await this.api.getLogs({
            page: this.page(),
            pageSize: this.pageSize,
            query: this.query(),
            level: this.level(),
            entity: this.entity()
        });

        this.logs.set(res?.items ?? res?.items ?? []);
        this.total.set(res?.total ?? res?.total ?? 0);
    }

    async next() {
        this.page.update(p => p + 1);
        await this.cargar();
    }

    async prev() {
        if (this.page() > 0) {
            this.page.update(p => p - 1);
            await this.cargar();
        }
    }

    async loadTimeline() {
        const data = await this.api.getLogsTimeline({});
        this.buildChart(data);
    }

    buildChart(data: any[]) {

        if (this.chart) this.chart.destroy();

        this.chart = new Chart('chartLogs', {
            type: 'line',
            data: {
                labels: data.map(x => new Date(x.bucket).toLocaleString()),
                datasets: [
                    {
                        label: 'Info',
                        data: data.map(x => x.info),
                        borderColor: '#198754'
                    },
                    {
                        label: 'Warning',
                        data: data.map(x => x.warning),
                        borderColor: '#ffc107'
                    },
                    {
                        label: 'Error',
                        data: data.map(x => x.error),
                        borderColor: '#dc3545'
                    }
                ]
            }
        });
    }
}
