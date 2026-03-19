import { Component, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgClass, CommonModule } from '@angular/common';
import { emailValidator, passwordValidator } from '../../../core/validators/auth.validators';
import { Router } from '@angular/router';

@Component({
    selector: 'app-register',
    imports: [ReactiveFormsModule, NgClass, CommonModule],
    templateUrl: './register.html',
    styleUrls: ['./register.scss'],
    standalone: true,
})
export class Register {
    formRegister: FormGroup;
    isLoading: boolean = false;
    showErrors: boolean = false;
    isEmailAvailable: boolean | null = null;
    isCheckingEmail: boolean = false;
    isUsernameAvailable: boolean | null = null;
    isCheckingUsername: boolean = false;
    errorMessage = signal<string | null>(null);

    private emailCheckTimeout: any;
    private usernameCheckTimeout: any;

    constructor(private formBuilder: FormBuilder, private router: Router) {
        this.formRegister = this.formBuilder.group({
        email: ['', [Validators.required, emailValidator()]],
        username: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(50)]],
        password: ['', [Validators.required, Validators.minLength(6), passwordValidator()]],
        passwordConfirm: ['', [Validators.required, Validators.minLength(6), passwordValidator()]],
        });
    }

// #region HELPERS

    /**
     * Genera el hash SHA-256 de la contrasena en formato hexadecimal.
     *
     * @param password - Contrasena en texto plano.
     * @returns Hash hexadecimal de 64 caracteres.
     */
    private async hashPassword(password: string): Promise<string> {
        const enc = new TextEncoder();
        const data = enc.encode(password);
        const hashBuffer = await crypto.subtle.digest('SHA-256', data);
        const hashArray = Array.from(new Uint8Array(hashBuffer));
        return hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
    }

// #endregion
// #region AVAILABILITY CHECKS

    /**
     * Comprueba con debounce la disponibilidad del correo electronico.
     * Espera 1 segundo desde el ultimo cambio antes de consultar la API.
     */
    checkEmailAvailability() {
        const email = this.formRegister.get('email')?.value?.trim();

        if (!email || this.formRegister.get('email')?.hasError('required') || this.formRegister.get('email')?.hasError('invalidEmail')) {
            this.isEmailAvailable = null;
            return;
        }

        if (this.emailCheckTimeout) {
            clearTimeout(this.emailCheckTimeout);
        }

        this.emailCheckTimeout = setTimeout(async () => {
        this.isCheckingEmail = true;
        try {
            const response = await fetch(`http://127.0.0.1:8000/api/auth/check-email/?email=${encodeURIComponent(email)}`);
            const data = await response.json();
            this.isEmailAvailable = data.available;
        } catch (error) {
            this.isEmailAvailable = null;
        } finally {
            this.isCheckingEmail = false;
            this.formRegister.get('email')?.markAsTouched();
        }
        }, 1000);
    }

    /**
     * Comprueba con debounce la disponibilidad del nombre de usuario.
     * Espera 1 segundo desde el ultimo cambio antes de consultar la API.
     */
    checkUsernameAvailability() {
        const username = this.formRegister.get('username')?.value?.trim();

        if (!username || username.length < 3) {
            this.isUsernameAvailable = null;
            return;
        }

        if (this.usernameCheckTimeout) {
            clearTimeout(this.usernameCheckTimeout);
        }

        this.usernameCheckTimeout = setTimeout(async () => {
        this.isCheckingUsername = true;
        try {
            const response = await fetch(`http://127.0.0.1:8000/api/auth/check-username/?username=${encodeURIComponent(username)}`);
            const data = await response.json();
            this.isUsernameAvailable = data.available;
        } catch (error) {
            this.isUsernameAvailable = null;
        } finally {
            this.isCheckingUsername = false;
            this.formRegister.get('username')?.markAsTouched();
        }
        }, 1000);
    }

// #endregion
// #region AUTH FLOW

    /**
     * Validate and submit registration form
     */
    async register() {
        this.showErrors = true;
        this.errorMessage.set(null);

        if (this.formRegister.invalid) {
            return;
        }

        if (this.isEmailAvailable === null) {
            this.errorMessage.set('Por favor, espera a que se verifique el correo electrónico');
            return;
        }

        if (this.isEmailAvailable === false) {
            return;
        }

        if (this.isUsernameAvailable === null) {
            this.errorMessage.set('Por favor, espera a que se verifique el nombre de usuario');
            return;
        }

        if (this.isUsernameAvailable === false) {
            return;
        }

        if (this.formRegister.value.password !== this.formRegister.value.passwordConfirm) {
            this.errorMessage.set('Las contraseñas no coinciden');
            return;
        }

        this.isLoading = true;

        try {
            const hashed = await this.hashPassword(this.formRegister.value.password);
            const response = await fetch('http://127.0.0.1:8000/api/auth/register/', {
                method: 'POST',
                headers: {
                'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                email: this.formRegister.value.email,
                username: this.formRegister.value.username,
                password: hashed,
                password_confirm: hashed,
                }),
            });

            const data = await response.json();

            if (!response.ok) {
                const errors = data.errors || {};
                const errorMsg =
                errors.email?.[0] ||
                errors.username?.[0] ||
                errors.password?.[0] ||
                errors.password_confirm?.[0] ||
                data.error ||
                'Error desconocido';
                this.errorMessage.set(errorMsg);
                return;
            }

            // Login after successful registration using the original credentials
            await this.loginAfterRegister(this.formRegister.value.email, this.formRegister.value.password);
        } catch (error) {
            this.errorMessage.set('Error de conexión. Verifica que el servidor esta corriendo en http://127.0.0.1:8000');
        } finally {
            this.isLoading = false;
        }
    }

    /**
     * Auto-login after successful registration
     */
    private async loginAfterRegister(email: string, password: string) {
        try {
            const hashed = await this.hashPassword(password);
            const response = await fetch('http://127.0.0.1:8000/api/auth/login/', {
                method: 'POST',
                headers: {
                'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                email: email,
                password: hashed,
                }),
            });

            const data = await response.json();

            if (!response.ok) {
                const errorMsg = data.errors?.email?.[0] || data.errors?.password?.[0] || data.error || 'Error desconocido';
                this.errorMessage.set(errorMsg);
                return;
            }

            // Save tokens and user info in localStorage
            localStorage.setItem('access_token', data.access);
            localStorage.setItem('refresh_token', data.refresh);
            localStorage.setItem('user', JSON.stringify(data.user));

            // Navigate to home page after successful login
            this.router.navigate(['/home']);
        } catch (error) {
            this.errorMessage.set('Error de conexión. Verifica que el servidor esta corriendo en http://127.0.0.1:8000');
        }
    }
// #endregion
}
