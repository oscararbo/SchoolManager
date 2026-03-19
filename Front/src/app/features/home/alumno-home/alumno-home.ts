import { CommonModule } from '@angular/common';
import { Component, Input, OnInit, inject, signal } from '@angular/core';
import { SchoolApiService, AlumnoMateria, AlumnoPanel } from '../../../shared/services/school-api.service';

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
    expandidas = new Set<number>();

    private api = inject(SchoolApiService);

    /** Carga el panel del alumno al inicializar el componente. */
    async ngOnInit(): Promise<void> {
        await this.cargarPanel();
    }

    /** Obtiene el panel del alumno desde la API y actualiza el Signal de datos. */
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

    /**
     * Alterna el estado expandido/contraido de una asignatura.
     *
     * @param asignaturaId - Identificador de la asignatura.
     */
    toggleExpandir(asignaturaId: number): void {
        if (this.expandidas.has(asignaturaId)) {
            this.expandidas.delete(asignaturaId);
        } else {
            this.expandidas.add(asignaturaId);
        }
    }

    /**
     * Indica si la tarjeta de una asignatura esta actualmente expandida.
     *
     * @param asignaturaId - Identificador de la asignatura.
     */
    estaExpandida(asignaturaId: number): boolean {
        return this.expandidas.has(asignaturaId);
    }

    /**
     * Filtra las tareas de una materia por trimestre.
     *
     * @param materia - Materia de la que obtener las tareas.
     * @param trimestre - Numero de trimestre (1, 2 o 3).
     * @returns Tareas del trimestre con su calificacion.
     */
    tareasPorTrimestre(materia: AlumnoMateria, trimestre: number): Array<{ tareaId: number; nombre: string; valor: number | null }> {
        return materia.notas.filter(tarea => tarea.trimestre === trimestre);
    }

    /**
     * Indica si una materia tiene al menos una tarea asignada al trimestre dado.
     *
     * @param materia - Materia a consultar.
     * @param trimestre - Numero de trimestre (1, 2 o 3).
     */
    tieneTareasEnTrimestre(materia: AlumnoMateria, trimestre: number): boolean {
        return materia.notas.some(tarea => tarea.trimestre === trimestre);
    }

    /**
     * Formatea un valor de nota para la vista.
     * Devuelve `'-'` si la nota aun no esta calificada.
     *
     * @param valor - Valor numerico o `null`.
     */
    formatNota(valor: number | null): string {
        return valor === null ? '-' : valor.toFixed(2);
    }
}
