import { Component, Input, Output, EventEmitter, ElementRef, ViewChild, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CsvImportEntity } from '../../../../../shared/services/school-api.service';

@Component({
    selector: 'app-csv-import-card',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './csv-import-card.html',
    styleUrls: ['./csv-import-card.scss']
})
export class CsvImportCardComponent implements OnChanges {
    @ViewChild('fileInput') private fileInputRef?: ElementRef<HTMLInputElement>;

    @Input() entidad!: CsvImportEntity;
    @Input() titulo!: string;
    @Input() descripcion!: string;
    @Input() cargando = false;
    @Input() archivo: File | null = null;

    @Output() fileSelected = new EventEmitter<{ event: Event; entidad: CsvImportEntity }>();
    @Output() importar = new EventEmitter<CsvImportEntity>();
    @Output() descargarPlantilla = new EventEmitter<CsvImportEntity>();

    archivoNombre(): string {
        return this.archivo?.name ?? 'Ningun archivo seleccionado';
    }

    botonImportarDeshabilitado(): boolean {
        return this.cargando || !this.archivo;
    }

    textoBotonImportar(): string {
        return this.cargando ? 'Importando...' : 'Importar';
    }

    ngOnChanges(changes: SimpleChanges): void {
        if ('archivo' in changes && this.archivo === null && this.fileInputRef?.nativeElement) {
            this.fileInputRef.nativeElement.value = '';
        }
    }

    onFileChange(event: Event): void {
        this.fileSelected.emit({ event, entidad: this.entidad });
    }

    onImportar(): void {
        this.importar.emit(this.entidad);
    }

    onDescargarPlantilla(): void {
        this.descargarPlantilla.emit(this.entidad);
    }
}
