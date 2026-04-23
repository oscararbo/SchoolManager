import { CommonModule } from '@angular/common';
import { Component, Input, forwardRef } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { FormFieldComponent } from '../form-field/form-field.component';

export type SelectOption = {
    value: number | string;
    label: string;
};

@Component({
    selector: 'app-select-input',
    standalone: true,
    imports: [CommonModule, FormFieldComponent],
    templateUrl: './select-input.component.html',
    styleUrls: ['./select-input.component.scss'],
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => SelectInputComponent),
            multi: true
        }
    ]
})
export class SelectInputComponent implements ControlValueAccessor {
    @Input() id = '';
    @Input() label = '';
    @Input() placeholder = 'Selecciona una opcion';
    @Input() required = false;
    @Input() errorMessage: string | null = null;
    @Input() valueType: 'string' | 'number' = 'string';
    @Input() options: SelectOption[] = [];

    value = '';
    disabled = false;

    private onChange: (value: number | string | null) => void = () => {};
    private onTouched: () => void = () => {};

    writeValue(value: number | string | null): void {
        this.value = value === null || value === undefined ? '' : String(value);
    }

    registerOnChange(fn: (value: number | string | null) => void): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    setDisabledState(isDisabled: boolean): void {
        this.disabled = isDisabled;
    }

    onSelectChange(event: Event): void {
        const target = event.target as HTMLSelectElement;
        this.value = target.value;

        if (this.value === '') {
            this.onChange(null);
            return;
        }

        this.onChange(this.valueType === 'number' ? Number(this.value) : this.value);
    }

    onBlur(): void {
        this.onTouched();
    }
}
