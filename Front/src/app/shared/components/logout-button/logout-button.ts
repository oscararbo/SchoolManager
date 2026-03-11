import { Component, output } from '@angular/core';

@Component({
    selector: 'app-logout-button',
    standalone: true,
    imports: [],
    templateUrl: './logout-button.html',
    styleUrl: './logout-button.scss',
})
export class LogoutButton {
    logout = output<void>();
    mostrandoConfirmacion = false;

    solicitarCierreSesion(): void {
        this.mostrandoConfirmacion = true;
    }

    cancelarCierreSesion(): void {
        this.mostrandoConfirmacion = false;
    }

    confirmarCierreSesion(): void {
        this.mostrandoConfirmacion = false;

        this.logout.emit();
    }
}
