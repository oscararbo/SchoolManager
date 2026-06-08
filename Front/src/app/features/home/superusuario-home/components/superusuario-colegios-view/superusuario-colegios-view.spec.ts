import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SuperusuarioColegiosView } from './superusuario-colegios-view';

describe('SuperusuarioColegiosView', () => {
    let component: SuperusuarioColegiosView;
    let fixture: ComponentFixture<SuperusuarioColegiosView>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [SuperusuarioColegiosView],
        }).compileComponents();

        fixture = TestBed.createComponent(SuperusuarioColegiosView);
        component = fixture.componentInstance;
        await fixture.whenStable();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
