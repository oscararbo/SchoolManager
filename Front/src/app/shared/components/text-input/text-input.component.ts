import { CommonModule } from '@angular/common';
import { Component, Input, forwardRef } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { FormFieldComponent } from '../form-field/form-field.component';

@Component({
    selector: 'app-text-input',
    standalone: true,
    imports: [CommonModule, FormFieldComponent],
    templateUrl: './text-input.component.html',
    styleUrls: ['./text-input.component.scss'],
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: forwardRef(() => TextInputComponent),
            multi: true
        }
    ]
})
export class TextInputComponent implements ControlValueAccessor {
    @Input() id = '';
    @Input() label = '';
    @Input() type: 'text' | 'email' | 'password' | 'tel' = 'text';
    @Input() placeholder = '';
    @Input() autocomplete = '';
    @Input() required = false;
    @Input() maxLength: number | null = null;
    @Input() minLength: number | null = null;
    @Input() errorMessage: string | null = null;
    @Input() showValidState = false;

    value = '';
    disabled = false;
    touched = false;

    private onChange: (value: string) => void = () => {};
    private onTouched: () => void = () => {};

    writeValue(value: string | null): void {
        this.value = value ?? '';
    }

    registerOnChange(fn: (value: string) => void): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: () => void): void {
        this.onTouched = fn;
    }

    setDisabledState(isDisabled: boolean): void {
        this.disabled = isDisabled;
    }

    onInput(event: Event): void {
        const target = event.target as HTMLInputElement;
        this.value = target.value;
        this.onChange(this.value);
    }

    onBlur(): void {
        this.touched = true;
        this.onTouched();
    }
}
