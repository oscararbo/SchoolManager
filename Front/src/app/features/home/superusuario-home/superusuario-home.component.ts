import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { SuperusuarioSectionsNavComponent, SuperUsuarioSection } from './components/superusuario-sections-nav/superusuario-sections-nav';
import { SuperusuarioColegiosViewComponent } from './components/superusuario-colegios-view/superusuario-colegios-view';
import { SuperusuarioLogsViewComponent } from './components/superusuario-logs-view/superusuario-logs-view';

@Component({
    selector: 'app-superusuario-home',
    standalone: true,
    imports: [
        SuperusuarioSectionsNavComponent,
        SuperusuarioColegiosViewComponent,
        SuperusuarioLogsViewComponent
    ],
    templateUrl: './superusuario-home.component.html',
    styleUrls: ['./superusuario-home.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class SuperusuarioHomeComponent {

    seccionActiva = signal<SuperUsuarioSection>('colegios');

    cambiarSeccion(seccion: SuperUsuarioSection): void {
        if (this.seccionActiva() === seccion) return;
        this.seccionActiva.set(seccion);
    }
}
