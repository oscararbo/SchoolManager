import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { SessionService } from '../../core/services/session.service';
import { environment } from '../../../environments/environment';
import type { ApiLoginResponse } from './school-api.contracts';
import { mapLoginResponse } from './school-api.mappers';
import type { LoginResponse } from './school-api.types';
import { extractSchoolApiError } from './school-api.error';

@Injectable({ providedIn: 'root' })
export class SchoolApiAuthService {
    private readonly apiUrl = environment.apiBaseUrl;
    private http = inject(HttpClient);
    private sessionService = inject(SessionService);

    async login(correo: string, contrasena: string): Promise<LoginResponse> {
        try {
            const response = await firstValueFrom(
                this.http.post<ApiLoginResponse>(`${this.apiUrl}/auth/login`, { correo, contrasena })
            );
            return mapLoginResponse(response);
        } catch (e) {
            throw extractSchoolApiError(e);
        }
    }

    async logout(): Promise<void> {
        try {
            await firstValueFrom(this.http.post<void>(`${this.apiUrl}/auth/logout`, {}));
        } finally {
            this.sessionService.clearSession();
        }
    }
}
