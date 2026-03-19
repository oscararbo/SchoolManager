import { Component, inject } from '@angular/core';
import { AuthStateService } from '../../../core/services/auth-state.service';

@Component({
    selector: 'app-session-expired-dialog',
    standalone: true,
    imports: [],
    templateUrl: './session-expired-dialog.component.html',
    styleUrl: './session-expired-dialog.component.scss'
})
export class SessionExpiredDialogComponent {
    protected authState = inject(AuthStateService);

    /** Cierra el dialogo y delega en {@link AuthStateService} el reseteo de estado y la redireccion. */
    aceptar(): void {
        this.authState.acceptExpired();
    }
}
