import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Validador de formato de correo electronico.
 * Acepta el patron: `texto@texto.texto`.
 *
 * @returns Funcion validadora que retorna `null` si es valido, o `{ invalidEmail: true }` si no.
 */
export function emailValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
        if (!control.value) {
            return null;
        }

        const emailRegex = /^[a-zA-Z0-9._%-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;

        if (!emailRegex.test(control.value)) {
            return { invalidEmail: true };
        }

        return null;
    };
}

