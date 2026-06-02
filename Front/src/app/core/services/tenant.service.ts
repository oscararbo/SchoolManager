import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class TenantService {
    private readonly fallbackSlug = (environment.defaultSchoolSlug ?? 'default').trim().toLowerCase();
    private router = inject(Router);

    getSchoolSlug(): string {
        // #region Path slug resolution
        const pathSchool = this.extractSchoolFromPath();
        if (pathSchool) {
            return pathSchool;
        }
        // #endregion

        // #region Query slug resolution
        const querySchool = new URLSearchParams(window.location.search).get('school')?.trim().toLowerCase();
        if (querySchool) {
            return querySchool;
        }
        // #endregion

        // #region Hostname slug resolution
        const host = window.location.hostname.toLowerCase();
        if (host !== 'localhost' && host !== '127.0.0.1') {
            const labels = host.split('.').filter(Boolean);
            if (labels.length >= 3) {
                return labels[0];
            }
        }
        // #endregion

        // #region Default fallback
        return this.fallbackSlug;
        // #endregion
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
