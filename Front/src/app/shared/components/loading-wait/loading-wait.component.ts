import { ChangeDetectionStrategy, Component, Input } from '@angular/core';

@Component({
    selector: 'app-loading-wait',
    standalone: true,
    imports: [],
    templateUrl: './loading-wait.component.html',
    styleUrl: './loading-wait.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoadingWaitComponent {
    @Input() visible = false;
    @Input() message = 'Por favor espere...';
}
