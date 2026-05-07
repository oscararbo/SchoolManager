import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { SchoolApiService, ColegioAdminItem, ColegioItem } from '../../../shared/services/school-api.service';

@Component({
    selector: 'app-superusuario-home',
    standalone: true,
    imports: [ReactiveFormsModule],
    templateUrl: './superusuario-home.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class SuperusuarioHomeComponent {
    colegios = signal<ColegioItem[]>([]);
    adminsColegio = signal<ColegioAdminItem[]>([]);
    cargando = signal(false);
    cargandoAdmins = signal(false);
    error = signal<string | null>(null);
    mensaje = signal<string | null>(null);
    colegioEnEdicionId = signal<number | null>(null);
    colegioAdminsActivo = signal<ColegioItem | null>(null);

    colegioForm;
    adminForm;

    private api = inject(SchoolApiService);
    private fb = inject(FormBuilder);

    constructor() {
        this.colegioForm = this.fb.group({
            id: [null as number | null],
            nombre: ['', [Validators.required, Validators.maxLength(160)]],
            slug: ['', [Validators.required, Validators.pattern(/^[a-z0-9-]+$/), Validators.maxLength(80)]],
            logoUrl: ['', [Validators.maxLength(500)]],
            faviconUrl: ['', [Validators.maxLength(500)]],
            colorPrimario: ['#1f2937', [Validators.maxLength(20)]],
            mensajeLogin: ['', [Validators.maxLength(240)]]
        });

        this.adminForm = this.fb.group({
            colegioId: [null as number | null, [Validators.required]],
            nombre: ['', [Validators.required, Validators.maxLength(120)]]
        });

        void this.cargarColegios();
    }

    async cargarColegios(): Promise<void> {
        this.cargando.set(true);
        this.error.set(null);
        try {
            const data = await this.api.getColegios();
            this.colegios.set(data);
        } catch (e) {
            this.error.set((e as Error).message || 'No se pudo cargar colegios.');
        } finally {
            this.cargando.set(false);
        }
    }

    async guardarColegio(): Promise<void> {
        this.mensaje.set(null);
        this.error.set(null);
        if (this.colegioForm.invalid) {
            return;
        }

        const value = this.colegioForm.getRawValue();
        try {
            const payload = [
                (value.nombre ?? '').trim(),
                (value.slug ?? '').trim().toLowerCase(),
                (value.logoUrl ?? '').trim() || undefined,
                (value.faviconUrl ?? '').trim() || undefined,
                (value.colorPrimario ?? '').trim() || undefined,
                (value.mensajeLogin ?? '').trim() || undefined
            ] as const;

            if (value.id) {
                const updated = await this.api.updateColegio(value.id, ...payload);
                this.colegios.update(current => current.map(colegio => colegio.id === updated.id ? updated : colegio).sort((a, b) => a.nombre.localeCompare(b.nombre)));
                if (this.colegioAdminsActivo()?.id === updated.id) {
                    this.colegioAdminsActivo.set(updated);
                }
                this.mensaje.set(`Colegio ${updated.nombre} actualizado.`);
            } else {
                const created = await this.api.createColegio(...payload);
                this.colegios.update(current => [...current, created].sort((a, b) => a.nombre.localeCompare(b.nombre)));
                this.mensaje.set(`Colegio ${created.nombre} creado.`);
            }

            this.limpiarFormularioColegio();
        } catch (e) {
            this.error.set((e as Error).message || 'No se pudo guardar el colegio.');
        }
    }

    async crearAdminColegio(): Promise<void> {
        this.mensaje.set(null);
        this.error.set(null);
        if (this.adminForm.invalid) {
            return;
        }

        const value = this.adminForm.getRawValue();
        try {
            const createdAdmin = await this.api.createAdminColegio(
                value.colegioId!,
                (value.nombre ?? '').trim()
            );
            this.adminForm.reset({ colegioId: value.colegioId, nombre: '' });
            this.mensaje.set(`Administrador creado. Correo: ${createdAdmin.correo}. Clave temporal: ${createdAdmin.contrasenaTemporal ?? 'no disponible'}`);
            if (this.colegioAdminsActivo()?.id === value.colegioId) {
                await this.cargarAdminsColegio(value.colegioId!);
            }
            await this.cargarColegios();
        } catch (e) {
            this.error.set((e as Error).message || 'No se pudo crear el administrador.');
        }
    }

    editarColegio(colegio: ColegioItem): void {
        this.colegioEnEdicionId.set(colegio.id);
        this.colegioForm.reset({
            id: colegio.id,
            nombre: colegio.nombre,
            slug: colegio.slug,
            logoUrl: colegio.logoUrl ?? '',
            faviconUrl: colegio.faviconUrl ?? '',
            colorPrimario: colegio.colorPrimario ?? '#1f2937',
            mensajeLogin: colegio.mensajeLogin ?? ''
        });
    }

    cancelarEdicion(): void {
        this.limpiarFormularioColegio();
    }

    async eliminarColegio(colegio: ColegioItem): Promise<void> {
        this.mensaje.set(null);
        this.error.set(null);
        if (!window.confirm(`Se desactivara el colegio ${colegio.nombre}.`)) {
            return;
        }

        try {
            await this.api.deleteColegio(colegio.id);
            this.colegios.update(current => current.filter(item => item.id !== colegio.id));
            if (this.colegioAdminsActivo()?.id === colegio.id) {
                this.colegioAdminsActivo.set(null);
                this.adminsColegio.set([]);
            }
            if (this.colegioEnEdicionId() === colegio.id) {
                this.limpiarFormularioColegio();
            }
            this.mensaje.set(`Colegio ${colegio.nombre} desactivado.`);
        } catch (e) {
            this.error.set((e as Error).message || 'No se pudo desactivar el colegio.');
        }
    }

    async cargarAdminsColegio(colegioId: number): Promise<void> {
        this.error.set(null);
        this.cargandoAdmins.set(true);
        try {
            const colegio = this.colegios().find(item => item.id === colegioId) ?? null;
            const admins = await this.api.getAdminsByColegio(colegioId);
            this.colegioAdminsActivo.set(colegio);
            this.adminsColegio.set(admins);
        } catch (e) {
            this.error.set((e as Error).message || 'No se pudo cargar administradores.');
        } finally {
            this.cargandoAdmins.set(false);
        }
    }

    private limpiarFormularioColegio(): void {
        this.colegioEnEdicionId.set(null);
        this.colegioForm.reset({ id: null, nombre: '', slug: '', logoUrl: '', faviconUrl: '', colorPrimario: '#1f2937', mensajeLogin: '' });
    }

    getDevUrl(colegio: ColegioItem): string {
        return `${window.location.origin}/?school=${colegio.slug}`;
    }

    getSuggestedSubdomainUrl(colegio: ColegioItem): string {
        return `https://${colegio.slug}.tu-dominio.com`;
    }
}
