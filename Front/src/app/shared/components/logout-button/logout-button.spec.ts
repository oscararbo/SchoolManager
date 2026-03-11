import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LogoutButton } from './logout-button';

describe('LogoutButton', () => {
    let component: LogoutButton;
    let fixture: ComponentFixture<LogoutButton>;
    let logoutCalls = 0;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [LogoutButton],
        }).compileComponents();

        fixture = TestBed.createComponent(LogoutButton);
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
        const originalConfirm = window.confirm;
        window.confirm = () => true;

        component.solicitarCierreSesion();

        expect(logoutCalls).toBe(1);
        window.confirm = originalConfirm;
    });

    it('should not emit logout when user cancels', () => {
        const originalConfirm = window.confirm;
        window.confirm = () => false;

        component.solicitarCierreSesion();

        expect(logoutCalls).toBe(0);
        window.confirm = originalConfirm;
    });
});
