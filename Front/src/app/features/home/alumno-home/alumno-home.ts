import { CommonModule } from '@angular/common';
import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { SchoolApiService, AlumnoPanel } from '../../../shared/services/school-api.service';

@Component({
    selector: 'app-alumno-home',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './alumno-home.html',
    styleUrl: './alumno-home.scss'
})
export class AlumnoHomeComponent implements OnInit {
    @Input({ required: true }) estudianteId!: number;
    @Input({ required: true }) nombre!: string;

    cargando = signal(true);
    error = signal<string | null>(null);
    panel = signal<AlumnoPanel | null>(null);

    private api = inject(SchoolApiService);

    async ngOnInit(): Promise<void> {
        await this.cargarPanel();
    }

    async cargarPanel(): Promise<void> {
        this.cargando.set(true);
        this.error.set(null);

        try {
            const data = await this.api.getPanelAlumno(this.estudianteId);
            this.panel.set(data);
        } catch (e) {
            this.error.set((e as Error).message);
        } finally {
            this.cargando.set(false);
        }
    }
}
