import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { AdminStats } from '../../../../../shared/services/school-api.service';

@Component({
    selector: 'app-admin-stats-cards',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './admin-stats-cards.component.html'
})
export class AdminStatsCardsComponent {
    @Input({ required: true }) stats!: AdminStats;

    get cards(): Array<{ label: string; value: number }> {
        return [
            { label: 'Cursos', value: this.stats.totalCursos },
            { label: 'Asignaturas', value: this.stats.totalAsignaturas },
            { label: 'Profesores', value: this.stats.totalProfesores },
            { label: 'Estudiantes', value: this.stats.totalEstudiantes },
            { label: 'Matriculas', value: this.stats.totalMatriculas },
            { label: 'Tareas', value: this.stats.totalTareas }
        ];
    }
}
