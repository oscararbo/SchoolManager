import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { extractSchoolApiError } from './school-api.error';
import type { ApiColegioAdminItem, ApiColegioItem } from './school-api.contracts';
import { mapColegioAdminItem, mapColegioItem } from './school-api.mappers';
import type { ColegioAdminItem, ColegioItem } from './school-api.types';

@Injectable({ providedIn: 'root' })
export class SchoolApiSuperUsuarioService {
    private readonly apiUrl = environment.apiBaseUrl;
    private http = inject(HttpClient);

    async getColegios(): Promise<ColegioItem[]> {
        try {
            const response = await firstValueFrom(this.http.get<ApiColegioItem[]>(`${this.apiUrl}/superusuario/colegios`));
            return response.map(mapColegioItem);
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async getColegioBySlug(slug: string): Promise<ColegioItem> {
        try {
            const response = await firstValueFrom(this.http.get<ApiColegioItem>(`${this.apiUrl}/superusuario/colegios/slug/${encodeURIComponent(slug)}`));
            return mapColegioItem(response);
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async getAdminsByColegio(colegioId: number): Promise<ColegioAdminItem[]> {
        try {
            const response = await firstValueFrom(this.http.get<ApiColegioAdminItem[]>(`${this.apiUrl}/superusuario/colegios/${colegioId}/admins`));
            return response.map(mapColegioAdminItem);
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async createColegio(nombre: string, slug: string, logoUrl?: string, faviconUrl?: string, colorPrimario?: string, mensajeLogin?: string): Promise<ColegioItem> {
        try {
            const response = await firstValueFrom(this.http.post<ApiColegioItem>(`${this.apiUrl}/superusuario/colegios`, { nombre, slug, logoUrl, faviconUrl, colorPrimario, mensajeLogin }));
            return mapColegioItem(response);
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async updateColegio(id: number, nombre: string, slug: string, logoUrl?: string, faviconUrl?: string, colorPrimario?: string, mensajeLogin?: string): Promise<ColegioItem> {
        try {
            const response = await firstValueFrom(this.http.put<ApiColegioItem>(`${this.apiUrl}/superusuario/colegios/${id}`, { nombre, slug, logoUrl, faviconUrl, colorPrimario, mensajeLogin }));
            return mapColegioItem(response);
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async deleteColegio(id: number): Promise<void> {
        try {
            await firstValueFrom(this.http.delete<void>(`${this.apiUrl}/superusuario/colegios/${id}`));
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async createAdminColegio(colegioId: number, nombre: string): Promise<ColegioAdminItem> {
        try {
            const response = await firstValueFrom(this.http.post<ApiColegioAdminItem>(`${this.apiUrl}/superusuario/colegios/${colegioId}/admins`, { nombre }));
            return mapColegioAdminItem(response);
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }
}
