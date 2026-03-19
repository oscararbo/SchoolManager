import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService } from '../../../core/services/toast.service';

@Component({
    selector: 'app-toasts',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './toasts.component.html',
    styleUrl: './toasts.component.scss'
})
export class ToastsComponent {
    toastService = inject(ToastService);
}
