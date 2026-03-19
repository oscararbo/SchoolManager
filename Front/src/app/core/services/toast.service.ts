import { Injectable, signal } from '@angular/core';

export interface Toast {
    id: number;
    message: string;
    type: 'success' | 'error' | 'warning' | 'info';
}

@Injectable({ providedIn: 'root' })
export class ToastService {
    private counter = 0;
    toasts = signal<Toast[]>([]);

    /**
     * Crea y muestra un toast que se elimina automaticamente transcurrida la duracion indicada.
     *
     * @param message - Texto del mensaje a mostrar.
     * @param type - Nivel visual: `'success'`, `'error'`, `'warning'` o `'info'`.
     * @param duration - Milisegundos antes de que el toast desaparezca (defecto: 5000).
     */
    show(message: string, type: Toast['type'] = 'info', duration = 5000): void {
        const id = ++this.counter;
        this.toasts.update(ts => [...ts, { id, message, type }]);
        setTimeout(() => this.dismiss(id), duration);
    }

    /**
     * Elimina el toast identificado por su id.
     *
     * @param id - Identificador unico del toast.
     */
    dismiss(id: number): void {
        this.toasts.update(ts => ts.filter(t => t.id !== id));
    }
}
