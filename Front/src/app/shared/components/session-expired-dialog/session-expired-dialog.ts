import { Component, inject } from '@angular/core';
import { AuthStateService } from '../../../core/services/auth-state.service';

@Component({
    selector: 'app-session-expired-dialog',
    standalone: true,
    imports: [],
    templateUrl: './session-expired-dialog.html',
    styles: [`
        .session-expired-overlay {
            position: fixed;
            inset: 0;
            background: rgba(0, 0, 0, 0.55);
            display: flex;
            align-items: center;
            justify-content: center;
            z-index: 9999;
        }
        .session-expired-card {
            max-width: 400px;
            width: 90%;
        }
    `]
})
export class SessionExpiredDialogComponent {
    private authState = inject(AuthStateService);

    aceptar(): void {
        this.authState.acceptExpired();
    }
}
