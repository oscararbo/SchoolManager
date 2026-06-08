import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SuperusuarioLogsView } from './superusuario-logs-view';

describe('SuperusuarioLogsView', () => {
    let component: SuperusuarioLogsView;
    let fixture: ComponentFixture<SuperusuarioLogsView>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [SuperusuarioLogsView],
        }).compileComponents();

        fixture = TestBed.createComponent(SuperusuarioLogsView);
        component = fixture.componentInstance;
        await fixture.whenStable();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
