import { TestBed } from '@angular/core/testing';
import { AlumnoHomeComponent } from './alumno-home';
import { SchoolApiService } from '../../../shared/services/school-api.service';

describe('AlumnoHomeComponent', () => {
  let calledWithId: number | null = null;

  const schoolApiMock = {
    getPanelAlumnoResumen: async (id: number) => {
      calledWithId = id;
      return {
        id: 1,
        nombre: 'Alumno Test',
        curso: { cursoId: 1, curso: '1A' },
        materias: []
      };
    }
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AlumnoHomeComponent],
      providers: [
        { provide: SchoolApiService, useValue: schoolApiMock }
      ]
    }).compileComponents();
  });

  it('should create and load student panel', async () => {
    const fixture = TestBed.createComponent(AlumnoHomeComponent);
    fixture.componentInstance.estudianteId = 1;
    fixture.componentInstance.nombre = 'Alumno Test';

    fixture.detectChanges();
    await fixture.whenStable();

    expect(fixture.componentInstance).toBeTruthy();
    expect(calledWithId).toBe(1);
  });
});
