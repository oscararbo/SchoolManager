import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { SessionService } from '../services/session.service';
import { AuthStateService } from '../services/auth-state.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
    const sessionService = inject(SessionService);
    const authState = inject(AuthStateService);

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
                        authState.markExpired();
                        return throwError(() => error);
                    }
                    return next(addAuth(req));
                })
            );
        })
    );
};
