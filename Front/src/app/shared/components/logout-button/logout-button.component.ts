import { Component, output, signal } from '@angular/core';

@Component({
    selector: 'app-logout-button',
    standalone: true,
    imports: [],
    templateUrl: './logout-button.component.html',
    styleUrl: './logout-button.component.scss',
})
export class LogoutButtonComponent {
    logout = output<void>();
    mostrandoConfirmacion = signal(false);

    /** Muestra el panel de confirmacion de cierre de sesion. */
    solicitarCierreSesion(): void {
        this.mostrandoConfirmacion.set(true);
    }

    /** Oculta el panel de confirmacion sin ejecutar el cierre de sesion. */
    cancelarCierreSesion(): void {
        this.mostrandoConfirmacion.set(false);
    }

    /** Confirma el cierre de sesion y emite el evento de salida al componente padre. */
    confirmarCierreSesion(): void {
        this.mostrandoConfirmacion.set(false);
        this.logout.emit();
    }
}
