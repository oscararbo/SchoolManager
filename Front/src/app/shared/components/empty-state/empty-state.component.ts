import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
    selector: 'app-empty-state',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './empty-state.component.html',
    styleUrl: './empty-state.component.scss'
})
export class EmptyStateComponent {
    @Input() title = 'Sin datos';
    @Input() description = 'Todavia no hay informacion para mostrar.';
    @Input() actionLabel?: string;
    @Output() action = new EventEmitter<void>();
}
