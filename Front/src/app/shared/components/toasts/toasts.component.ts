import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastService } from '../../../core/services/toast.service';

@Component({
    selector: 'app-toasts',
    standalone: true,
    imports: [],
    templateUrl: './toasts.component.html',
    styleUrl: './toasts.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ToastsComponent {
    toastService = inject(ToastService);
}
