import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

export type AdminSection = 'estadisticas' | 'gestion';

@Component({
    selector: 'app-admin-sections-nav',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './admin-sections-nav.component.html'
})
export class AdminSectionsNavComponent {
    @Input({ required: true }) seccionActiva!: AdminSection;
    @Output() readonly seccionChange = new EventEmitter<AdminSection>();

    seleccionar(seccion: AdminSection): void {
        if (this.seccionActiva !== seccion) {
            this.seccionChange.emit(seccion);
        }
    }
}
