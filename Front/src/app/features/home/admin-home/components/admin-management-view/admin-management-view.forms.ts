import { AbstractControl, FormBuilder, Validators } from '@angular/forms';
import { dniValidator, isoDateValidator, phoneValidator } from '../../../../../core/validators/profile.validators';

export type AdminManagementForms = ReturnType<typeof createAdminManagementForms>;

export function createAdminManagementForms(fb: FormBuilder) {
    return {
        cursoForm: fb.group({
            nombre: fb.nonNullable.control('', [Validators.required, Validators.maxLength(120)])
        }),
        editCursoForm: fb.group({
            nombre: fb.nonNullable.control('', [Validators.required, Validators.maxLength(120)])
        }),
        asignaturaForm: fb.group({
            nombre: fb.nonNullable.control('', [Validators.required, Validators.maxLength(120)]),
            cursoId: fb.control<number | null>(null, [Validators.required])
        }),
        editAsignaturaForm: fb.group({
            nombre: fb.nonNullable.control('', [Validators.required, Validators.maxLength(120)]),
            cursoId: fb.control<number | null>(null, [Validators.required])
        }),
        profesorForm: fb.group({
            nombre: fb.nonNullable.control('', [Validators.required, Validators.maxLength(120)]),
            apellidos: fb.nonNullable.control('', [Validators.required, Validators.maxLength(120)]),
            dni: fb.nonNullable.control('', [Validators.required, Validators.maxLength(20), dniValidator()]),
            telefono: fb.nonNullable.control('', [Validators.required, Validators.maxLength(20), phoneValidator()]),
            especialidad: fb.nonNullable.control('', [Validators.required, Validators.maxLength(100)]),
            correo: fb.nonNullable.control('', [Validators.required, Validators.email, Validators.maxLength(200)]),
            contrasena: fb.nonNullable.control('', [Validators.required, Validators.minLength(6), Validators.maxLength(200)])
        }),
        editProfesorForm: fb.group({
            nombre: fb.nonNullable.control('', [Validators.required, Validators.maxLength(120)]),
            apellidos: fb.nonNullable.control('', [Validators.required, Validators.maxLength(120)]),
            dni: fb.nonNullable.control('', [Validators.required, Validators.maxLength(20), dniValidator()]),
            telefono: fb.nonNullable.control('', [Validators.required, Validators.maxLength(20), phoneValidator()]),
            especialidad: fb.nonNullable.control('', [Validators.required, Validators.maxLength(100)]),
            correo: fb.nonNullable.control('', [Validators.required, Validators.email, Validators.maxLength(200)]),
            nuevaContrasena: fb.nonNullable.control('', [Validators.minLength(6), Validators.maxLength(200)])
        }),
        estudianteForm: fb.group({
            nombre: fb.nonNullable.control('', [Validators.required, Validators.maxLength(120)]),
            apellidos: fb.nonNullable.control('', [Validators.required, Validators.maxLength(120)]),
            dni: fb.nonNullable.control('', [Validators.required, Validators.maxLength(20), dniValidator()]),
            telefono: fb.nonNullable.control('', [Validators.required, Validators.maxLength(20), phoneValidator()]),
            fechaNacimiento: fb.nonNullable.control('', [Validators.required, isoDateValidator()]),
            correo: fb.nonNullable.control('', [Validators.required, Validators.email, Validators.maxLength(200)]),
            contrasena: fb.nonNullable.control('', [Validators.required, Validators.minLength(6), Validators.maxLength(200)]),
            cursoId: fb.control<number | null>(null, [Validators.required])
        }),
        editEstudianteForm: fb.group({
            nombre: fb.nonNullable.control('', [Validators.required, Validators.maxLength(120)]),
            apellidos: fb.nonNullable.control('', [Validators.required, Validators.maxLength(120)]),
            dni: fb.nonNullable.control('', [Validators.required, Validators.maxLength(20), dniValidator()]),
            telefono: fb.nonNullable.control('', [Validators.required, Validators.maxLength(20), phoneValidator()]),
            fechaNacimiento: fb.nonNullable.control('', [Validators.required, isoDateValidator()]),
            correo: fb.nonNullable.control('', [Validators.required, Validators.email, Validators.maxLength(200)]),
            nuevaContrasena: fb.nonNullable.control('', [Validators.minLength(6), Validators.maxLength(200)]),
            cursoId: fb.control<number | null>(null, [Validators.required])
        })
    };
}

export function getAdminControlErrorMessage(control: AbstractControl | null): string | null {
    if (!control || !control.touched || !control.errors) {
        return null;
    }

    if (control.hasError('required')) {
        return 'Campo obligatorio.';
    }

    if (control.hasError('email')) {
        return 'Correo no valido.';
    }

    if (control.hasError('minlength')) {
        const requiredLength = control.getError('minlength')?.requiredLength;
        return `Minimo ${requiredLength ?? 0} caracteres.`;
    }

    if (control.hasError('maxlength')) {
        const requiredLength = control.getError('maxlength')?.requiredLength;
        return `Maximo ${requiredLength ?? 0} caracteres.`;
    }

    return null;
}
