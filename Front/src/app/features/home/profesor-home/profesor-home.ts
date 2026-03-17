import { CommonModule } from '@angular/common';
import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
    SchoolApiService,
    ProfesorPanel,
    AsignaturaAlumnos,
    AsignaturaAlumno,
    TareaResumen,
} from '../../../shared/services/school-api.service';

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

    // Task creation form
    nuevaTareaNombre = signal('');
    nuevaTareaTrimestre = signal<number>(1);
    creandoTarea = signal(false);

    // Active task for grading
    tareaActiva = signal<TareaResumen | null>(null);
    // Map estudianteId -> nota value being edited
    notaInputs = signal<Record<number, number | null>>({});
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
        this.tareaActiva.set(null);
        this.notaInputs.set({});
        try {
            const data = await this.api.getAlumnosDeAsignatura(this.profesorId, asignaturaId);
            this.detalleAsignatura.set(data);
        } catch (e) {
            this.error.set((e as Error).message);
        }
    }

    seleccionarTarea(tarea: TareaResumen): void {
        this.tareaActiva.set(tarea);
        const detalle = this.detalleAsignatura();
        if (!detalle) return;
        const inputs: Record<number, number | null> = {};
        for (const alumno of detalle.alumnos) {
            const nota = alumno.notas.find(n => n.tareaId === tarea.tareaId);
            inputs[alumno.estudianteId] = nota?.valor ?? null;
        }
        this.notaInputs.set(inputs);
    }

    getNotaInput(estudianteId: number): number | null {
        return this.notaInputs()[estudianteId] ?? null;
    }

    setNotaInput(estudianteId: number, valor: number | null): void {
        this.notaInputs.update(m => ({ ...m, [estudianteId]: valor }));
    }

    async guardarNota(estudianteId: number): Promise<void> {
        const tarea = this.tareaActiva();
        const asignaturaId = this.asignaturaActivaId();
        const valor = this.notaInputs()[estudianteId];

        if (!tarea || valor === null || valor === undefined || Number.isNaN(valor)) {
            this.error.set('Introduce una nota valida.');
            return;
        }
        if (valor < 0 || valor > 10) {
            this.error.set('La nota debe estar entre 0 y 10.');
            return;
        }

        this.guardandoNota.set(true);
        this.error.set(null);
        try {
            await this.api.ponerNota(this.profesorId, estudianteId, tarea.tareaId, valor);
            if (asignaturaId) await this.cargarAlumnos(asignaturaId);
            if (tarea) this.seleccionarTarea(tarea);
        } catch (e) {
            this.error.set((e as Error).message);
        } finally {
            this.guardandoNota.set(false);
        }
    }

    async crearTarea(): Promise<void> {
        const nombre = this.nuevaTareaNombre().trim();
        const trimestre = this.nuevaTareaTrimestre();
        const asignaturaId = this.asignaturaActivaId();

        if (!nombre || !asignaturaId) {
            this.error.set('Rellena el nombre de la tarea.');
            return;
        }

        this.creandoTarea.set(true);
        this.error.set(null);
        try {
            await this.api.crearTarea(this.profesorId, nombre, trimestre, asignaturaId);
            this.nuevaTareaNombre.set('');
            this.nuevaTareaTrimestre.set(1);
            await this.cargarAlumnos(asignaturaId);
        } catch (e) {
            this.error.set((e as Error).message);
        } finally {
            this.creandoTarea.set(false);
        }
    }

    notaDeAlumno(alumno: AsignaturaAlumno, tareaId: number): number | null {
        return alumno.notas.find(n => n.tareaId === tareaId)?.valor ?? null;
    }

    formatNota(n: number | null): string {
        return n !== null ? n.toFixed(2) : '-';
    }
}
