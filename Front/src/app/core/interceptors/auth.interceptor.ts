import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { SessionService } from '../services/session.service';
import { AuthStateService } from '../services/auth-state.service';

/**
 * Extrae el mensaje legible de un error `401 Unauthorized`.
 * Prioriza el cuerpo de texto plano, luego `detail`, luego `title` del ProblemDetails.
 *
 * @param error - Respuesta HTTP con estado 401.
 * @returns Mensaje legible para el usuario.
 */
function getUnauthorizedMessage(error: HttpErrorResponse): string {
    if (typeof error.error === 'string' && error.error.trim()) {
        return error.error;
    }

    return error.error?.detail ?? error.error?.title ?? 'Necesitas iniciar sesion para acceder a este recurso.';
}

/**
 * Interceptor HTTP que gestiona la autenticacion JWT y el refresco de sesion.
 *
 * - Adjunta el header `Authorization: Bearer <token>` a cada peticion.
 * - Ante un `401`, intenta renovar la sesion via refreshToken.
 * - Si el refresco falla, activa el dialogo de sesion expirada con el mensaje del backend.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const sessionService = inject(SessionService);
    const authState = inject(AuthStateService);

    /**
     * Clona la peticion aniadiendo la cabecera de autorizacion.
     * Si no hay token activo, devuelve la peticion original sin modificar.
     */
    const addAuth = (r: HttpRequest<unknown>): HttpRequest<unknown> => {
        const token = sessionService.getSession()?.token;
        if (!token) return r;
        return r.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
    };

    const isAuthUrl = req.url.includes('/auth/login') || req.url.includes('/auth/refresh');

    return next(addAuth(req)).pipe(
        catchError((error: HttpErrorResponse) => {
            if (error.status !== 401 || isAuthUrl) {
                return throwError(() => error);
            }

            return from(authState.tryRefresh()).pipe(
                switchMap(refreshed => {
                    if (!refreshed) {
                        authState.markExpired(getUnauthorizedMessage(error));
                        return throwError(() => error);
                    }
                    return next(addAuth(req));
                })
            );
        })
    );
};
