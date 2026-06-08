import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SuperusuarioSectionsNav } from './superusuario-sections-nav';

describe('SuperusuarioSectionsNav', () => {
    let component: SuperusuarioSectionsNav;
    let fixture: ComponentFixture<SuperusuarioSectionsNav>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [SuperusuarioSectionsNav],
        }).compileComponents();

        fixture = TestBed.createComponent(SuperusuarioSectionsNav);
        component = fixture.componentInstance;
        await fixture.whenStable();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });
});
