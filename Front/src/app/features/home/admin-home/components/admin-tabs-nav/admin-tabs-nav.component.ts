import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

export type AdminTab = 'cursos' | 'asignaturas' | 'profesores' | 'estudiantes' | 'matriculas' | 'imparticiones' | 'importar';

@Component({
    selector: 'app-admin-tabs-nav',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './admin-tabs-nav.component.html'
})
export class AdminTabsNavComponent {
    @Input({ required: true }) tabActiva!: AdminTab;
    @Output() readonly tabChange = new EventEmitter<AdminTab>();

    readonly tabs: Array<{ key: AdminTab; label: string; iconClass?: string }> = [
        { key: 'cursos', label: 'Cursos' },
        { key: 'asignaturas', label: 'Asignaturas' },
        { key: 'profesores', label: 'Profesores' },
        { key: 'estudiantes', label: 'Estudiantes' },
        { key: 'matriculas', label: 'Matriculas' },
        { key: 'imparticiones', label: 'Imparticiones' },
        { key: 'importar', label: 'Importar CSV', iconClass: 'bi bi-upload me-1' }
    ];

    seleccionar(tab: AdminTab): void {
        if (this.tabActiva !== tab) {
            this.tabChange.emit(tab);
        }
    }
}