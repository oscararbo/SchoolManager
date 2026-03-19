import { Component, output } from '@angular/core';

@Component({
    selector: 'app-logout-button',
    standalone: true,
    imports: [],
    templateUrl: './logout-button.component.html',
    styleUrl: './logout-button.component.scss',
})
export class LogoutButtonComponent {
    logout = output<void>();
    mostrandoConfirmacion = false;

    /** Muestra el panel de confirmacion de cierre de sesion. */
    solicitarCierreSesion(): void {
        this.mostrandoConfirmacion = true;
    }

    /** Oculta el panel de confirmacion sin ejecutar el cierre de sesion. */
    cancelarCierreSesion(): void {
        this.mostrandoConfirmacion = false;
    }

    /** Confirma el cierre de sesion y emite el evento de salida al componente padre. */
    confirmarCierreSesion(): void {
        this.mostrandoConfirmacion = false;
        this.logout.emit();
    }
}
