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

/**
 * Validador de seguridad de contrasena.
 * Requiere al menos una mayuscula, una minuscula y un digito.
 *
 * @returns Funcion validadora que retorna `null` si es valida, o `{ invalidPassword: true }` si no.
 */
export function passwordValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
        if (!control.value) {
            return null;
        }

        const value = control.value;
        const hasUpperCase = /[A-Z]/.test(value);
        const hasLowerCase = /[a-z]/.test(value);
        const hasNumber = /[0-9]/.test(value);

        const isValid = hasUpperCase && hasLowerCase && hasNumber;

        if (!isValid) {
            return { invalidPassword: true };
        }

        return null;
    };
}
