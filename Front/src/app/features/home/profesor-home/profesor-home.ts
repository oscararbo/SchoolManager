import { CommonModule } from '@angular/common';
import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SchoolApiService, ProfesorPanel, AsignaturaAlumnos } from '../../../shared/services/school-api.service';

@Component({
    selector: 'app-profesor-home',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: './profesor-home.html',
    styleUrl: './profesor-home.scss'
})
export class ProfesorHomeComponent implements OnInit {
    @Input({ required: true }) profesorId!: number;
    @Input({ required: true }) profesorNombre!: string;

    cargando = signal(true);
    error = signal<string | null>(null);

    panel = signal<ProfesorPanel | null>(null);
    detalleAsignatura = signal<AsignaturaAlumnos | null>(null);
    asignaturaActivaId = signal<number | null>(null);

    estudianteSeleccionadoId = signal<number | null>(null);
    notaTemporal = signal<number | null>(null);
    guardandoNota = signal(false);

    private api = inject(SchoolApiService);

    async ngOnInit(): Promise<void> {
        await this.cargarPanel();
    }

    async cargarPanel(): Promise<void> {
        this.cargando.set(true);
        this.error.set(null);

        try {
            const data = await this.api.getPanelProfesor(this.profesorId);
            this.panel.set(data);
        } catch (e) {
            this.error.set((e as Error).message);
        } finally {
            this.cargando.set(false);
        }
    }

    async cargarAlumnos(asignaturaId: number): Promise<void> {
        this.error.set(null);
        this.asignaturaActivaId.set(asignaturaId);
        this.estudianteSeleccionadoId.set(null);
        this.notaTemporal.set(null);

        try {
            const data = await this.api.getAlumnosDeAsignatura(this.profesorId, asignaturaId);
            this.detalleAsignatura.set(data);
        } catch (e) {
            this.error.set((e as Error).message);
        }
    }

    seleccionarAlumno(estudianteId: number, notaActual: number | null): void {
        this.estudianteSeleccionadoId.set(estudianteId);
        this.notaTemporal.set(notaActual);
    }

    async guardarNota(asignaturaId: number): Promise<void> {
        const estudianteId = this.estudianteSeleccionadoId();
        const valor = this.notaTemporal();

        if (!estudianteId || valor === null || Number.isNaN(valor)) {
            this.error.set('Selecciona un alumno e introduce una nota valida.');
            return;
        }

        if (valor < 0 || valor > 10) {
            this.error.set('La nota debe estar entre 0 y 10.');
            return;
        }

        this.guardandoNota.set(true);
        this.error.set(null);

        try {
            await this.api.ponerNota(this.profesorId, estudianteId, asignaturaId, valor);
            await this.cargarAlumnos(asignaturaId);
        } catch (e) {
            this.error.set((e as Error).message);
        } finally {
            this.guardandoNota.set(false);
        }
    }
}
