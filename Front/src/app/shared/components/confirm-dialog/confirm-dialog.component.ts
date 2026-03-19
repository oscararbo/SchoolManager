import { Component, signal } from '@angular/core';

/**
 * Componente modal de confirmacion.
 * Se muestra como una card centrada con botones de aceptar y cancelar.
 */
@Component({
    selector: 'app-confirm-dialog',
    standalone: true,
    imports: [],
    templateUrl: './confirm-dialog.component.html',
    styleUrl: './confirm-dialog.component.scss'
})
export class ConfirmDialogComponent {
    /**
     * Titulo del dialogo de confirmacion.
     * @example "Eliminar estudiante"
     */
    titulo = signal<string>('');

    /**
     * Mensaje detallado que muestra la accion que se va a realizar.
     * @example "¿Eliminar al estudiante \"Juan Perez\"? Esta accion no se puede deshacer."
     */
    mensaje = signal<string>('');

    /**
     * Indica si el dialogo esta visible.
     */
    visible = signal(false);

    /** Callback interno que resuelve la promesa pendiente de confirmacion. */
    private resolver: ((value: boolean) => void) | null = null;

    /**
     * Muestra el dialogo de confirmacion con un titulo y mensaje, y espera la respuesta del usuario.
     *
     * @param titulo - Titulo del dialogo.
     * @param mensaje - Mensaje descriptivo.
     * @returns Promesa que resuelve a `true` si confirma, `false` si cancela.
     */
    async show(titulo: string, mensaje: string): Promise<boolean> {
        this.titulo.set(titulo);
        this.mensaje.set(mensaje);
        this.visible.set(true);

        return new Promise(resolve => {
            this.resolver = resolve;
        });
    }

    /**
     * Confirma la accion y cierra el dialogo.
     */
    aceptar(): void {
        this.visible.set(false);
        if (this.resolver) {
            this.resolver(true);
            this.resolver = null;
        }
    }

    /**
     * Cancela la accion y cierra el dialogo.
     */
    cancelar(): void {
        this.visible.set(false);
        if (this.resolver) {
            this.resolver(false);
            this.resolver = null;
        }
    }
}
