import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { HomeLayout } from './home-layout';
import { SessionService, UserSession } from '../../core/services/session.service';
import { SchoolApiService } from '../../shared/services/school-api.service';

describe('HomeLayout', () => {
  let component: HomeLayout;
  let fixture: ComponentFixture<HomeLayout>;

  const mockSession: UserSession = {
    id: 1,
    nombre: 'Profesor Prueba',
    correo: 'profesorprueba@prueba.com',
    rol: 'profesor',
  };

  let getSessionResult: UserSession | null = mockSession;
  let clearSessionCalled = false;
  let navigateCalls: unknown[][] = [];
  let logoutCalls = 0;

  const mockSessionService: Partial<SessionService> = {
    getSession: () => getSessionResult,
    clearSession: () => { clearSessionCalled = true; },
  };

  const mockSchoolApiService: Partial<SchoolApiService> = {
    logout: async () => {
      logoutCalls += 1;
    }
  };

  beforeEach(async () => {
    getSessionResult = mockSession;
    clearSessionCalled = false;
    navigateCalls = [];
    logoutCalls = 0;

    await TestBed.configureTestingModule({
      imports: [HomeLayout],
      providers: [
        provideRouter([]),
        { provide: SessionService, useValue: mockSessionService },
        { provide: SchoolApiService, useValue: mockSchoolApiService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HomeLayout);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load the session on init', () => {
    expect(component.session()).toEqual(mockSession);
  });

  it('should call logout api when cerrarSesion is called', async () => {
    await component.cerrarSesion();
    expect(logoutCalls).toBe(1);
  });
});
