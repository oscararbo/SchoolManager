import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

const dniRegex = /^\d{8}[TRWAGMYFPDXBNJZSQVHLCKE]$/i;
const phoneRegex = /^[6-9]\d{8}$/;

export function dniValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
        const value = String(control.value ?? '').trim();
        if (!value) {
            return null;
        }

        return dniRegex.test(value) ? null : { invalidDni: true };
    };
}

export function phoneValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
        const value = String(control.value ?? '').trim();
        if (!value) {
            return null;
        }

        return phoneRegex.test(value) ? null : { invalidPhone: true };
    };
}

export function isoDateValidator(): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
        const value = String(control.value ?? '').trim();
        if (!value) {
            return null;
        }

        const date = new Date(`${value}T00:00:00`);
        if (Number.isNaN(date.getTime())) {
            return { invalidDate: true };
        }

        const [year, month, day] = value.split('-').map(Number);
        if (!year || !month || !day) {
            return { invalidDate: true };
        }

        const normalized = `${String(year).padStart(4, '0')}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
        return normalized === value ? null : { invalidDate: true };
    };
}
