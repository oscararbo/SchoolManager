import { Component, EventEmitter, Input, Output, ChangeDetectionStrategy } from '@angular/core';

export type SuperUsuarioSection = 'colegios' | 'logs';

@Component({
    selector: 'app-superusuario-sections-nav',
    standalone: true,
    templateUrl: './superusuario-sections-nav.html',
    styleUrls: ['./superusuario-sections-nav.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class SuperusuarioSectionsNavComponent {
    @Input({ required: true }) seccionActiva!: SuperUsuarioSection;
    @Output() readonly seccionChange = new EventEmitter<SuperUsuarioSection>();

    seleccionar(seccion: SuperUsuarioSection): void {
        if (this.seccionActiva !== seccion) {
            this.seccionChange.emit(seccion);
        }
    }
}
