import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
    selector: 'app-form-field',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './form-field.component.html',
    styleUrls: ['./form-field.component.scss']
})
export class FormFieldComponent {
    @Input() label = '';
    @Input() forId = '';
    @Input() required = false;
    @Input() errorMessage: string | null = null;
    @Input() hint: string | null = null;
}
