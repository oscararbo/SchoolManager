import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { NgClass, CommonModule, NgIf } from '@angular/common';
import { emailValidator } from '../../../core/validators/auth.validators';
import { Router } from '@angular/router';
import { SchoolApiService } from '../../../shared/services/school-api.service';
import { SessionService } from '../../../core/services/session.service';

@Component({
    selector: 'app-login',
    imports: [ReactiveFormsModule, NgClass, CommonModule, NgIf],
    templateUrl: './login.html',
    styleUrls: ['./login.scss'],
    standalone: true,
})
export class Login implements OnInit {
    formLogin: FormGroup;
    cargando: boolean = false;
    mostrarErrores: boolean = false;
    errorMessage = signal<string | null>(null);
    private router = inject(Router);
    private formBuilder = inject(FormBuilder);
    private schoolApiService = inject(SchoolApiService);
    private sessionService = inject(SessionService);

    constructor() {
        this.formLogin = this.formBuilder.group({
            email: ['', [Validators.required, emailValidator()]],
            password: ['', [Validators.required]],
        });
    }

// #region LIFECYCLE

    /**
     * Pre-fill form when coming from register flow
     */
    ngOnInit() {
        const state = (history as any).state;
        if (state?.email && state?.password) {
            this.formLogin.patchValue({
                email: state.email,
                password: state.password
            });
        }
    }

// #endregion
// #region AUTH FLOW

    /**
     * Validate and submit login credentials
     */
    async iniciarSesion() {
        this.mostrarErrores = true;
        this.errorMessage.set(null);

        if (this.formLogin.invalid) {
            return;
        }

        this.cargando = true;

        try {
            const correo = this.formLogin.value.email;
            const contrasena = this.formLogin.value.password;

            const data = await this.schoolApiService.login(correo, contrasena);

            this.sessionService.setSession({
                rol: data.rol,
                id: data.id,
                nombre: data.nombre,
                correo: data.correo,
                cursoId: data.cursoId,
                curso: data.curso
            });

            this.router.navigate(['/home']);
        } catch (error) {
            this.errorMessage.set((error as Error).message || 'No se pudo iniciar sesion.');
        } finally {
            this.cargando = false;
        }
    }
// #endregion
}
