import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CsvImportCardComponent } from '../../../csv-import-card/csv-import-card.component';
import { CsvImportEntity, CsvImportResult } from '../../../../../../../shared/services/school-api.service';
import { CsvErrorGroup } from '../../admin-management-view.csv';

type CsvImportItem = {
    entidad: CsvImportEntity;
    orden: string;
    titulo: string;
    descripcion: string;
};

@Component({
    selector: 'app-admin-importar-csv-tab',
    standalone: true,
    imports: [CommonModule, CsvImportCardComponent],
    templateUrl: './admin-importar-csv-tab.component.html'
})
export class AdminImportarCsvTabComponent {
    @Input() csvImportItems: CsvImportItem[] = [];
    @Input() csvCargando = false;
    @Input() csvEntidadActual: CsvImportEntity | null = null;
    @Input() csvResultado: CsvImportResult | null = null;
    @Input() csvErroresAgrupados: CsvErrorGroup[] = [];

    @Input() csvArchivo: (entidad: CsvImportEntity) => File | null = () => null;
    @Input() erroresVisiblesGrupo: (grupo: CsvErrorGroup) => string[] = () => [];
    @Input() csvPuedeExpandirGrupo: (grupo: CsvErrorGroup) => boolean = () => false;
    @Input() grupoErroresExpandido: (key: string) => boolean = () => false;
    @Input() erroresOcultosGrupo: (grupo: CsvErrorGroup) => number = () => 0;

    @Output() fileSelected = new EventEmitter<{ event: Event; entidad: CsvImportEntity }>();
    @Output() importar = new EventEmitter<CsvImportEntity>();
    @Output() descargarPlantilla = new EventEmitter<CsvImportEntity>();
    @Output() toggleGrupoErrores = new EventEmitter<string>();
}
