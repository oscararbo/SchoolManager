import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CsvImportCardComponent } from '../../../csv-import-card/csv-import-card.component';
import { CsvImportEntity, CsvImportResult } from '../../../../../../../shared/services/school-api.service';
import { CsvErrorGroup } from '../../admin-management-view.csv';

type CsvImportItem = {
    entidad: CsvImportEntity;
    orden: string;
    titulo: string;
    descripcion: string;
};

type CsvResultFilter = 'todos' | 'errores' | 'omitidos' | 'creados';

@Component({
    selector: 'app-admin-importar-csv-tab',
    standalone: true,
    imports: [CsvImportCardComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
    templateUrl: './admin-importar-csv-tab.component.html'
})
export class AdminImportarCsvTabComponent {
    @Input() csvImportItems: CsvImportItem[] = [];
    @Input() csvCargando = false;
    @Input() csvEntidadActual: CsvImportEntity | null = null;
    @Input() csvResultado: CsvImportResult | null = null;
    @Input() csvErroresAgrupados: CsvErrorGroup[] = [];
    @Input() csvOmitidosDetalle: string[] = [];
    @Input() csvOmitidosNoDetallados = 0;
    @Input() csvDetallesCreados: string[] = [];

    @Input() csvArchivo: (entidad: CsvImportEntity) => File | null = () => null;
    @Input() erroresVisiblesGrupo: (grupo: CsvErrorGroup) => string[] = () => [];
    @Input() csvPuedeExpandirGrupo: (grupo: CsvErrorGroup) => boolean = () => false;
    @Input() grupoErroresExpandido: (key: string) => boolean = () => false;
    @Input() erroresOcultosGrupo: (grupo: CsvErrorGroup) => number = () => 0;

    @Output() fileSelected = new EventEmitter<{ event: Event; entidad: CsvImportEntity }>();
    @Output() importar = new EventEmitter<CsvImportEntity>();
    @Output() descargarPlantilla = new EventEmitter<CsvImportEntity>();
    @Output() toggleGrupoErrores = new EventEmitter<string>();

    csvResultFilter: CsvResultFilter = 'todos';

    setCsvResultFilter(filter: CsvResultFilter): void {
        this.csvResultFilter = filter;
    }

    get hasErrores(): boolean {
        return this.csvErroresAgrupados.length > 0;
    }

    get hasOmitidos(): boolean {
        return (this.csvResultado?.omitidos ?? 0) > 0;
    }

    get hasCreadosDetalle(): boolean {
        return this.csvDetallesCreados.length > 0;
    }

    get shouldShowErrores(): boolean {
        return this.hasErrores && (this.csvResultFilter === 'todos' || this.csvResultFilter === 'errores');
    }

    get shouldShowOmitidos(): boolean {
        return this.hasOmitidos && (this.csvResultFilter === 'todos' || this.csvResultFilter === 'omitidos');
    }

    get shouldShowCreados(): boolean {
        return this.hasCreadosDetalle && (this.csvResultFilter === 'todos' || this.csvResultFilter === 'creados');
    }
}
