import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class TenantService {
    private readonly fallbackSlug = (environment.defaultSchoolSlug ?? 'default').trim().toLowerCase();
    private router = inject(Router);

    getSchoolSlug(): string {
        // 1. Intentar obtener desde el path: /school/:schoolSlug/...
        const pathSchool = this.extractSchoolFromPath();
        if (pathSchool) {
            return pathSchool;
        }

        // 2. Intentar obtener desde query param: ?school=default
        const querySchool = new URLSearchParams(window.location.search).get('school')?.trim().toLowerCase();
        if (querySchool) {
            return querySchool;
        }

        // 3. Intentar obtener desde hostname: school-slug.app.com
        const host = window.location.hostname.toLowerCase();
        if (host !== 'localhost' && host !== '127.0.0.1') {
            const labels = host.split('.').filter(Boolean);
            if (labels.length >= 3) {
                return labels[0];
            }
        }

        // 4. Usar fallback por defecto
        return this.fallbackSlug;
    }

    private extractSchoolFromPath(): string | null {
        const urlTree = this.router.parseUrl(this.router.url);
        const primarySegment = urlTree.root.children['primary'];
        if (primarySegment?.segments[0]?.path === 'school' && primarySegment?.segments[1]) {
            return primarySegment.segments[1].path?.trim().toLowerCase() || null;
        }
        return null;
    }
}
