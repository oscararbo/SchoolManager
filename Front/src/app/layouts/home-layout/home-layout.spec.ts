import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { HomeLayout } from './home-layout';
import { SessionService, UserSession } from '../../core/services/session.service';

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

  const mockSessionService: Partial<SessionService> = {
    getSession: () => getSessionResult,
    clearSession: () => { clearSessionCalled = true; },
  };

  beforeEach(async () => {
    getSessionResult = mockSession;
    clearSessionCalled = false;
    navigateCalls = [];

    await TestBed.configureTestingModule({
      imports: [HomeLayout],
      providers: [
        provideRouter([]),
        { provide: SessionService, useValue: mockSessionService },
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

  it('should clear session when cerrarSesion is called', () => {
    component.cerrarSesion();
    expect(clearSessionCalled).toBe(true);
  });
});
