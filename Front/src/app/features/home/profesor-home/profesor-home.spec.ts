import { TestBed } from '@angular/core/testing';
import { ProfesorHomeComponent } from './profesor-home';
import { SchoolApiService } from '../../../shared/services/school-api.service';

describe('ProfesorHomeComponent', () => {
  let calledWithId: number | null = null;

  const schoolApiMock = {
    getPanelProfesor: async (id: number) => {
      calledWithId = id;
      return {
        id: 1,
        nombre: 'Profesor Test',
        cursos: []
      };
    },
    getAlumnosDeAsignatura: async () => ({
      asignatura: { id: 1, nombre: 'Matematicas', cursoId: 1, curso: '1A' },
      alumnos: []
    }),
    ponerNota: async () => undefined
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProfesorHomeComponent],
      providers: [
        { provide: SchoolApiService, useValue: schoolApiMock }
      ]
    }).compileComponents();
  });

  it('should create and load teacher panel', async () => {
    const fixture = TestBed.createComponent(ProfesorHomeComponent);
    fixture.componentInstance.profesorId = 1;
    fixture.componentInstance.profesorNombre = 'Profesor Test';

    fixture.detectChanges();
    await fixture.whenStable();

    expect(fixture.componentInstance).toBeTruthy();
    expect(calledWithId).toBe(1);
  });
});
