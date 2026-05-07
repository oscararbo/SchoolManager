import { DOCUMENT } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import {Login} from '../../features/auth/login/login';
import { SchoolApiService } from '../../shared/services/school-api.service';
import { TenantService } from '../../core/services/tenant.service';

@Component({
    selector: 'app-auth-layout',
    imports: [
        Login
    ],
    templateUrl: './auth-layout.html',
    styleUrl: './auth-layout.scss',
    })
export class AuthLayout implements OnInit {
    schoolName = signal('School Manager');
    schoolLogoUrl = signal<string | null>(null);
    schoolMessage = signal('Consulta tus clases, tus asignaturas y tus notas en un solo lugar.');
    schoolPrimaryColor = signal('#0b3d91');

    private schoolApi = inject(SchoolApiService);
    private tenantService = inject(TenantService);
    private document = inject(DOCUMENT);

    async ngOnInit(): Promise<void> {
        const slug = this.tenantService.getSchoolSlug();
        try {
            const colegio = await this.schoolApi.getColegioBySlug(slug);
            this.schoolName.set(colegio.nombre);
            this.schoolLogoUrl.set(colegio.logoUrl ?? null);
            this.schoolMessage.set(colegio.mensajeLogin?.trim() || 'Consulta tus clases, tus asignaturas y tus notas en un solo lugar.');
            this.schoolPrimaryColor.set(colegio.colorPrimario?.trim() || '#0b3d91');
            this.document.title = `${colegio.nombre} | School Manager`;
            this.updateFavicon(colegio.faviconUrl ?? colegio.logoUrl ?? null);
        } catch {
            this.schoolName.set('School Manager');
            this.schoolLogoUrl.set(null);
            this.schoolMessage.set('Consulta tus clases, tus asignaturas y tus notas en un solo lugar.');
            this.schoolPrimaryColor.set('#0b3d91');
            this.document.title = 'School Manager';
        }
    }

    private updateFavicon(iconUrl: string | null): void {
        if (!iconUrl) {
            return;
        }

        let link = this.document.querySelector<HTMLLinkElement>('link[rel="icon"]');
        if (!link) {
            link = this.document.createElement('link');
            link.rel = 'icon';
            this.document.head.appendChild(link);
        }

        link.href = iconUrl;
    }
}
