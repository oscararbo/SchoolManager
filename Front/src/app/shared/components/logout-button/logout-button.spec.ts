import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LogoutButtonComponent } from './logout-button.component';

describe('LogoutButtonComponent', () => {
    let component: LogoutButtonComponent;
    let fixture: ComponentFixture<LogoutButtonComponent>;
    let logoutCalls = 0;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [LogoutButtonComponent],
        }).compileComponents();

        fixture = TestBed.createComponent(LogoutButtonComponent);
        component = fixture.componentInstance;
        logoutCalls = 0;
        component.logout.subscribe(() => {
            logoutCalls += 1;
        });
        await fixture.whenStable();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('should emit logout when user confirms', () => {
        component.solicitarCierreSesion();
        component.confirmarCierreSesion();

        expect(logoutCalls).toBe(1);
    });

    it('should not emit logout when user cancels', () => {
        component.solicitarCierreSesion();
        component.cancelarCierreSesion();

        expect(logoutCalls).toBe(0);
    });
});
