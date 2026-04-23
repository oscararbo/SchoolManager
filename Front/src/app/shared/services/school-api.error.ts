import { HttpErrorResponse } from '@angular/common/http';

export function extractSchoolApiError(error: unknown): Error {
    if (error instanceof HttpErrorResponse) {
        if (error.status === 0) {
            return new Error('No se pudo conectar con el servidor. Comprueba que el backend este en ejecucion.');
        }

        if (typeof error.error === 'string' && error.error.trim()) {
            return new Error(error.error);
        }

        const validationErrors = error.error?.errors;
        if (validationErrors && typeof validationErrors === 'object') {
            const firstMessage = Object.values(validationErrors)
                .flatMap(value => Array.isArray(value) ? value : [String(value)])
                .find(Boolean);

            if (firstMessage) {
                return new Error(String(firstMessage));
            }
        }

        const csvErrors = error.error?.errores;
        if (Array.isArray(csvErrors) && csvErrors.length > 0) {
            const firstCsvError = String(csvErrors[0]);
            const base = error.error?.detail ?? error.error?.mensaje ?? 'La operacion ha fallado.';
            return new Error(`${base} ${firstCsvError}`);
        }

        const msg = error.error?.detail ?? error.error?.mensaje ?? error.error?.title ?? error.message ?? 'Error de servidor.';
        return new Error(msg);
    }

    return error instanceof Error ? error : new Error('Error desconocido.');
}
