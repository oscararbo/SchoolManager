import { ComponentFixture, TestBed } from '@angular/core/testing';
import { vi } from 'vitest';

import { AdminManagementViewComponent } from './admin-management-view.component';
import { SchoolApiService } from '../../../../../shared/services/school-api.service';
import { ToastService } from '../../../../../core/services/toast.service';
import type { CsvImportResult, CursoItem } from '../../../../../shared/services/school-api.types';

describe('AdminManagementViewComponent', () => {
    let component: AdminManagementViewComponent;
    let fixture: ComponentFixture<AdminManagementViewComponent>;
    let apiServiceMock: SchoolApiService & Record<string, ReturnType<typeof vi.fn>>;
    let toastServiceMock: ToastService & Record<string, ReturnType<typeof vi.fn>>;

    const cursosMock: CursoItem[] = [
        { id: 1, nombre: '1A' },
        { id: 2, nombre: '2A' }
    ];

    beforeEach(async () => {
        apiServiceMock = {
            getCursos: vi.fn().mockResolvedValue(cursosMock),
            getAsignaturas: vi.fn().mockResolvedValue([]),
            getProfesores: vi.fn().mockResolvedValue([]),
            getEstudiantes: vi.fn().mockResolvedValue([]),
            getAdminMatriculas: vi.fn().mockResolvedValue([]),
            getAdminImparticiones: vi.fn().mockResolvedValue([]),
            getAdminHorarios: vi.fn().mockResolvedValue([]),
            importarCsv: vi.fn().mockResolvedValue({ creados: 1, omitidos: 0, errores: [], detalles: [] })
        } as unknown as SchoolApiService & Record<string, ReturnType<typeof vi.fn>>;
        toastServiceMock = {
            show: vi.fn()
        } as unknown as ToastService & Record<string, ReturnType<typeof vi.fn>>;

        await TestBed.configureTestingModule({
            imports: [AdminManagementViewComponent],
            providers: [
                { provide: SchoolApiService, useValue: apiServiceMock },
                { provide: ToastService, useValue: toastServiceMock }
            ]
        }).compileComponents();

        fixture = TestBed.createComponent(AdminManagementViewComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('muestra botones de recarga y exportacion fuera del tab importar', async () => {
        component.tabActiva.set('cursos');
        fixture.detectChanges();
        await fixture.whenStable();

        const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
        const labels = buttons.map(button => button.textContent?.trim());

        expect(labels).toContain('Recargar');
        expect(labels).toContain('Exportar Excel');
    });

    it('oculta botones de recarga y exportacion en el tab importar', async () => {
        component.tabActiva.set('importar');
        fixture.detectChanges();
        await fixture.whenStable();

        const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
        const labels = buttons.map(button => button.textContent?.trim());

        expect(labels).not.toContain('Recargar');
        expect(labels).not.toContain('Exportar Excel');
    });

    it('exportarTabActualExcel genera el archivo xlsx y muestra toast', () => {
        component.cursos.set(cursosMock);
        component.tabActiva.set('cursos');

        component.exportarTabActualExcel();

        expect(toastServiceMock.show).toHaveBeenCalledWith(expect.stringMatching('Exportacion Excel completada'), 'success');
    });

    it('recargarTabActual vuelve a pedir datos del tab actual', async () => {
        const getCursosMock = apiServiceMock.getCursos as ReturnType<typeof vi.fn>;
        getCursosMock.mockClear();
        getCursosMock.mockResolvedValue(cursosMock);
        component.tabActiva.set('cursos');

        await component.recargarTabActual();

        expect(apiServiceMock.getCursos).toHaveBeenCalledTimes(1);
    });

    it('importarCsv usa el archivo seleccionado, refresca y limpia la seleccion', async () => {
        const csvResult: CsvImportResult = { creados: 1, omitidos: 0, errores: [], detalles: [] };
        const importarCsvMock = apiServiceMock.importarCsv as ReturnType<typeof vi.fn>;
        importarCsvMock.mockResolvedValue(csvResult);
        component.tabActiva.set('cursos');
        component.csvHorariosFile = new File(['a,b\n1,2'], 'horarios.csv', { type: 'text/csv' });

        await component.importarCsv('horarios');

        expect(apiServiceMock.importarCsv).toHaveBeenCalledWith('horarios', expect.any(File));
        expect(apiServiceMock.getCursos).toHaveBeenCalled();
        expect(component.csvHorariosFile).toBeNull();
        expect(toastServiceMock.show).toHaveBeenCalledWith(expect.stringMatching('Importacion de horarios'), 'success');
    });

    it('onCsvFileChange rechaza archivos demasiado grandes', () => {
        const bigFile = new File([new Uint8Array(10 * 1024 * 1024 + 1)], 'big.csv', { type: 'text/csv' });
        const input = document.createElement('input');
        Object.defineProperty(input, 'files', {
            value: [bigFile],
            configurable: true
        });

        component.onCsvFileChange({ target: input } as unknown as Event, 'horarios');

        expect(component.csvHorariosFile).toBeNull();
        expect(toastServiceMock.show).toHaveBeenCalledWith('El archivo CSV no puede superar 10 MB.', 'warning');
    });
});
