import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';

/**
 * Extrae el primer mensaje significativo de una respuesta HTTP de error.
 *
 * Orden de prioridad:
 * 1. Error de red (status 0).
 * 2. Cuerpo de texto plano.
 * 3. Primer error de validacion de modelo.
 * 4. Campo `detail` o `title` de ProblemDetails.
 * 5. Mensaje generico de HTTP.
 *
 * @param error - Respuesta HTTP de error.
 * @returns Mensaje legible para el usuario.
 */
function getHttpErrorMessage(error: HttpErrorResponse): string {
    if (error.status === 0) {
        return 'No se pudo conectar con el servidor. Comprueba que el backend este en ejecucion.';
    }

    if (typeof error.error === 'string' && error.error.trim()) {
        return error.error;
    }

    const validationErrors = error.error?.errors;
    if (validationErrors && typeof validationErrors === 'object') {
        const firstMessage = Object.values(validationErrors)
            .flatMap(value => Array.isArray(value) ? value : [String(value)])
            .find(Boolean);

        if (firstMessage) {
            return String(firstMessage);
        }
    }

    const csvErrors = error.error?.errores;
    if (Array.isArray(csvErrors) && csvErrors.length > 0) {
        const firstCsvError = String(csvErrors[0]);
        const base = error.error?.detail ?? error.error?.mensaje ?? 'La importacion ha fallado.';
        return `${base} ${firstCsvError}`;
    }

    return error.error?.detail ?? error.error?.mensaje ?? error.error?.title ?? error.message ?? 'Error de servidor.';
}

/**
 * Interceptor HTTP que muestra un toast de error para cualquier fallo HTTP que no sea `401`.
 * Los errores `401` los gestiona el flujo de refresco en {@link authInterceptor}.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
    const toastService = inject(ToastService);
    const skipToast = req.headers.has('X-Skip-Error-Toast');
    const request = skipToast
        ? req.clone({ headers: req.headers.delete('X-Skip-Error-Toast') })
        : req;

    const esImportCsv = request.url.includes('/admin/csv/');

    return next(request).pipe(
        catchError((error: HttpErrorResponse) => {
            // 401 is handled by the auth refresh flow and the session-expired dialog.
            if (error.status === 403 && !skipToast) {
                toastService.show('No tienes permisos para realizar esta accion.', 'warning');
            } else if (error.status !== 401 && !esImportCsv && !skipToast) {
                toastService.show(getHttpErrorMessage(error), 'error');
            }
            return throwError(() => error);
        })
    );
};
