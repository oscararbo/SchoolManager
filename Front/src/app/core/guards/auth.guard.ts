import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessionService } from '../services/session.service';

/**
 * Guarda de ruta que permite el acceso solo a usuarios autenticados.
 * Redirige a `'/'` si no hay sesion activa.
 */
export const authGuard: CanActivateFn = () => {
    const sessionService = inject(SessionService);
    const router = inject(Router);

    if (sessionService.getSession()) {
        return true;
    }

    return router.createUrlTree(['']);
};

/**
 * Guarda de ruta que permite el acceso solo a usuarios NO autenticados.
 * Redirige a `'/home'` si ya existe una sesion activa.
 */
export const unauthGuard: CanActivateFn = () => {
    const sessionService = inject(SessionService);
    const router = inject(Router);

    if (!sessionService.getSession()) {
        return true;
    }

    return router.createUrlTree(['home']);
};
