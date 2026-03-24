import { Component, ViewChild, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminSectionsNavComponent } from './components/admin-sections-nav/admin-sections-nav.component';
import { AdminStatsViewComponent } from './components/admin-stats-view/admin-stats-view.component';
import { AdminManagementViewComponent } from './components/admin-management-view/admin-management-view.component';

type AdminSection = 'estadisticas' | 'gestion';

@Component({
    selector: 'app-admin-home',
    standalone: true,
    imports: [
        CommonModule,
        AdminSectionsNavComponent,
        AdminStatsViewComponent,
        AdminManagementViewComponent
    ],
    templateUrl: './admin-home.html',
    styleUrl: './admin-home.scss'
})
export class AdminHomeComponent {
    @ViewChild(AdminStatsViewComponent) private statsView?: AdminStatsViewComponent;

    seccionActiva = signal<AdminSection>('estadisticas');

    cambiarSeccion(seccion: AdminSection): void {
        if (this.seccionActiva() === seccion) {
            return;
        }
        this.seccionActiva.set(seccion);
    }

    onManagementDataChanged(): void {
        void this.statsView?.refrescarStats();
    }
}
