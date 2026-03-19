import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
    selector: 'app-loading-wait',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './loading-wait.component.html',
    styleUrl: './loading-wait.component.scss'
})
export class LoadingWaitComponent {
    @Input() visible = false;
    @Input() message = 'Por favor espere...';
}
