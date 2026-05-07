import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class TenantService {
    private readonly fallbackSlug = (environment.defaultSchoolSlug ?? 'default').trim().toLowerCase();

    getSchoolSlug(): string {
        const querySchool = new URLSearchParams(window.location.search).get('school')?.trim().toLowerCase();
        if (querySchool) {
            return querySchool;
        }

        const host = window.location.hostname.toLowerCase();
        if (host === 'localhost' || host === '127.0.0.1') {
            return this.fallbackSlug;
        }

        const labels = host.split('.').filter(Boolean);
        if (labels.length >= 3) {
            return labels[0];
        }

        return this.fallbackSlug;
    }
}
