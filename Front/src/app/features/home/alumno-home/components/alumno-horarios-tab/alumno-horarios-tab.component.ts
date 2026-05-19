import { ChangeDetectionStrategy, Component, Input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AlumnoPanelResumen } from '../../../../../shared/services/school-api.service';
import { CalendarEvent, WeeklyEventsCalendarComponent } from '../weekly-events-calendar/weekly-events-calendar.component';

type PersonalEventColor = CalendarEvent['colorClass'];

@Component({
    selector: 'app-alumno-horarios-tab',
    standalone: true,
    imports: [WeeklyEventsCalendarComponent, FormsModule],
    templateUrl: './alumno-horarios-tab.component.html',
    styleUrl: './alumno-horarios-tab.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AlumnoHorariosTabComponent {
    @Input({ required: true }) set panel(_value: AlumnoPanelResumen | null) {
        // Reserved for future API integration
    }

    // ── Admin events (read-only, set by admin) ──────────────────────────────
    readonly adminEvents: CalendarEvent[] = this.buildAdminEvents();

    // ── Personal events (editable by student) ───────────────────────────────
    readonly personalEvents = signal<CalendarEvent[]>(this.buildInitialPersonalEvents());

    // ── Form state ──────────────────────────────────────────────────────────
    readonly showForm = signal(false);
    formTitle = '';
    formDescription = '';
    formDate = this.toIsoDate(new Date());
    formStartTime = '09:00';
    formEndTime = '10:00';
    formLocation = '';
    formColor: PersonalEventColor = 'purple';
    formRepeatWeekly = false;
    readonly formError = signal<string | null>(null);

    readonly availableColors: Array<{ value: PersonalEventColor; label: string; bg: string }> = [
        { value: 'purple', label: 'Morado',  bg: '#9333ea' },
        { value: 'blue',   label: 'Azul',    bg: '#2563eb' },
        { value: 'green',  label: 'Verde',   bg: '#16a34a' },
        { value: 'orange', label: 'Naranja', bg: '#ea580c' },
        { value: 'pink',   label: 'Rosa',    bg: '#db2777' },
        { value: 'red',    label: 'Rojo',    bg: '#dc2626' },
        { value: 'slate',  label: 'Gris',    bg: '#475569' },
    ];

    // ── Form actions ────────────────────────────────────────────────────────
    abrirFormulario(dayOfWeek: 1 | 2 | 3 | 4 | 5 = 1): void {
        this.formTitle = '';
        this.formDescription = '';
        this.formDate = this.getNextDateForWeekday(dayOfWeek);
        this.formStartTime = '09:00';
        this.formEndTime = '10:00';
        this.formLocation = '';
        this.formColor = 'purple';
        this.formRepeatWeekly = false;
        this.formError.set(null);
        this.showForm.set(true);
    }

    cerrarFormulario(): void {
        this.showForm.set(false);
        this.formError.set(null);
    }

    guardarEvento(): void {
        this.formError.set(null);

        if (!this.formTitle.trim()) {
            this.formError.set('El titulo es obligatorio.');
            return;
        }

        const dayOfWeek = this.getWeekdayFromIso(this.formDate);
        if (!dayOfWeek) {
            this.formError.set('Selecciona una fecha valida de lunes a viernes.');
            return;
        }

        const [sh, sm] = this.formStartTime.split(':').map(Number);
        const [eh, em] = this.formEndTime.split(':').map(Number);
        if (sh * 60 + sm >= eh * 60 + em) {
            this.formError.set('La hora de fin debe ser posterior a la hora de inicio.');
            return;
        }

        const newEvent: CalendarEvent = {
            id: `personal-${Date.now()}`,
            title: this.formTitle.trim(),
            subtitle: this.formDescription.trim() || undefined,
            dayOfWeek,
            startTime: this.formStartTime,
            endTime: this.formEndTime,
            location: this.formLocation.trim() || undefined,
            colorClass: this.formColor,
            type: 'personal',
            repeatWeekly: this.formRepeatWeekly,
        };

        this.personalEvents.update(events => [...events, newEvent]);
        this.cerrarFormulario();
    }

    eliminarEventoPersonal(id: string): void {
        this.personalEvents.update(events => events.filter(e => e.id !== id));
    }

    getSelectedDateDayLabel(): string {
        const dayOfWeek = this.getWeekdayFromIso(this.formDate);
        if (!dayOfWeek) return 'Fin de semana';
        return this.weekdayLabel(dayOfWeek);
    }

    // ── Dummy data ──────────────────────────────────────────────────────────
    private buildAdminEvents(): CalendarEvent[] {
        const slots = [
            { start: '08:30', end: '09:25' },
            { start: '09:25', end: '10:20' },
            { start: '10:20', end: '11:15' },
            { start: '11:45', end: '12:40' },
            { start: '12:40', end: '13:35' },
            { start: '13:35', end: '14:30' },
        ] as const;

        const schedule: Record<1 | 2 | 3 | 4 | 5, Array<{
            title: string; subtitle: string; location: string; colorClass: CalendarEvent['colorClass']
        }>> = {
            1: [
                { title: 'Matematicas II',     subtitle: 'Profa. Elena Ruiz',       location: 'Aula B-12',    colorClass: 'blue'   },
                { title: 'Fisica Aplicada',    subtitle: 'Dr. Javier Nunez',        location: 'Aula B-10',    colorClass: 'green'  },
                { title: 'Programacion Web',   subtitle: 'Ing. Carlos Mena',        location: 'Lab 3',        colorClass: 'orange' },
                { title: 'Bases de Datos',     subtitle: 'Dra. Alicia Torres',      location: 'Aula C-04',    colorClass: 'pink'   },
                { title: 'Ingles Tecnico',     subtitle: 'Prof. Daniel Soto',       location: 'Aula A-07',    colorClass: 'slate'  },
                { title: 'Taller de Proyecto', subtitle: 'Ing. Laura Vega',         location: 'Lab 1',        colorClass: 'blue'   },
            ],
            2: [
                { title: 'Algoritmos',          subtitle: 'Ing. Marta Solis',       location: 'Aula D-01',    colorClass: 'green'  },
                { title: 'Matematicas II',      subtitle: 'Profa. Elena Ruiz',      location: 'Aula B-12',    colorClass: 'blue'   },
                { title: 'Arquitectura SW',     subtitle: 'Ing. Laura Vega',        location: 'Aula D-02',    colorClass: 'pink'   },
                { title: 'Sistemas Operativos', subtitle: 'Ing. Pablo Rojas',       location: 'Aula C-08',    colorClass: 'slate'  },
                { title: 'Lab. de Redes',       subtitle: 'Ing. Mario Paredes',     location: 'Lab 1',        colorClass: 'orange' },
                { title: 'Etica Profesional',   subtitle: 'Prof. Ana Ledesma',      location: 'Aula A-03',    colorClass: 'green'  },
            ],
            3: [
                { title: 'Bases de Datos',      subtitle: 'Dra. Alicia Torres',     location: 'Aula C-04',    colorClass: 'pink'   },
                { title: 'Programacion Web',    subtitle: 'Ing. Carlos Mena',       location: 'Lab 3',        colorClass: 'orange' },
                { title: 'Diseno UX',           subtitle: 'Lic. Paula Herrera',     location: 'Aula A-11',    colorClass: 'slate'  },
                { title: 'Ingles Tecnico',      subtitle: 'Prof. Daniel Soto',      location: 'Aula A-07',    colorClass: 'blue'   },
                { title: 'Sistemas Operativos', subtitle: 'Ing. Pablo Rojas',       location: 'Aula C-08',    colorClass: 'green'  },
                { title: 'Tutoria Academica',   subtitle: 'Orientacion estudiantil',location: 'Sala tutoria', colorClass: 'orange' },
            ],
            4: [
                { title: 'Arquitectura SW',     subtitle: 'Ing. Laura Vega',        location: 'Aula D-02',    colorClass: 'pink'   },
                { title: 'Algoritmos',          subtitle: 'Ing. Marta Solis',       location: 'Aula D-01',    colorClass: 'slate'  },
                { title: 'Matematicas II',      subtitle: 'Profa. Elena Ruiz',      location: 'Aula B-12',    colorClass: 'blue'   },
                { title: 'Lab. de Redes',       subtitle: 'Ing. Mario Paredes',     location: 'Lab 1',        colorClass: 'green'  },
                { title: 'Fisica Aplicada',     subtitle: 'Dr. Javier Nunez',       location: 'Aula B-10',    colorClass: 'orange' },
                { title: 'Proyecto Integrador', subtitle: 'Comite academico',       location: 'Sala 2',       colorClass: 'pink'   },
            ],
            5: [
                { title: 'Sistemas Operativos', subtitle: 'Ing. Pablo Rojas',       location: 'Aula C-08',    colorClass: 'slate'  },
                { title: 'Bases de Datos',      subtitle: 'Dra. Alicia Torres',     location: 'Aula C-04',    colorClass: 'blue'   },
                { title: 'Programacion Web',    subtitle: 'Ing. Carlos Mena',       location: 'Lab 3',        colorClass: 'green'  },
                { title: 'Arquitectura SW',     subtitle: 'Ing. Laura Vega',        location: 'Aula D-02',    colorClass: 'orange' },
                { title: 'Ingles Tecnico',      subtitle: 'Prof. Daniel Soto',      location: 'Aula A-07',    colorClass: 'pink'   },
                { title: 'Lab. de Proyecto',    subtitle: 'Ing. Mario Paredes',     location: 'Lab 2',        colorClass: 'slate'  },
            ],
        };

        return ([1, 2, 3, 4, 5] as const).flatMap(day =>
            slots.map((slot, i) => ({
                id: `admin-${day}-${i}`,
                title: schedule[day][i].title,
                subtitle: schedule[day][i].subtitle,
                location: schedule[day][i].location,
                colorClass: schedule[day][i].colorClass,
                dayOfWeek: day,
                startTime: slot.start,
                endTime: slot.end,
                type: 'admin' as const,
                repeatWeekly: true,
            }))
        );
    }

    private buildInitialPersonalEvents(): CalendarEvent[] {
        return [
            {
                id: 'personal-1',
                title: 'Estudiar Algoritmos',
                subtitle: 'Examen la semana proxima',
                dayOfWeek: 1,
                startTime: '16:00',
                endTime: '18:00',
                location: 'Biblioteca',
                colorClass: 'purple',
                type: 'personal',
                repeatWeekly: false,
            },
            {
                id: 'personal-2',
                title: 'Reunion de grupo',
                subtitle: 'Proyecto Integrador',
                dayOfWeek: 3,
                startTime: '15:00',
                endTime: '16:30',
                location: 'Sala coworking',
                colorClass: 'red',
                type: 'personal',
                repeatWeekly: true,
            },
            {
                id: 'personal-3',
                title: 'Tutoria Profa. Ruiz',
                dayOfWeek: 4,
                startTime: '16:00',
                endTime: '17:00',
                location: 'Despacho B-05',
                colorClass: 'orange',
                type: 'personal',
                repeatWeekly: false,
            },
        ];
    }

    private toIsoDate(date: Date): string {
        const y = date.getFullYear();
        const m = String(date.getMonth() + 1).padStart(2, '0');
        const d = String(date.getDate()).padStart(2, '0');
        return `${y}-${m}-${d}`;
    }

    private parseIsoDate(value: string): Date | null {
        const parts = value.split('-').map(Number);
        if (parts.length !== 3 || parts.some(Number.isNaN)) return null;
        return new Date(parts[0], parts[1] - 1, parts[2]);
    }

    private getWeekdayFromIso(value: string): 1 | 2 | 3 | 4 | 5 | null {
        const parsed = this.parseIsoDate(value);
        if (!parsed) return null;
        const jsDay = parsed.getDay();
        if (jsDay === 0 || jsDay === 6) return null;
        return jsDay as 1 | 2 | 3 | 4 | 5;
    }

    private getNextDateForWeekday(target: 1 | 2 | 3 | 4 | 5): string {
        const today = new Date();
        const start = new Date(today.getFullYear(), today.getMonth(), today.getDate());
        const diff = (target - start.getDay() + 7) % 7;
        start.setDate(start.getDate() + diff);
        return this.toIsoDate(start);
    }

    private weekdayLabel(day: 1 | 2 | 3 | 4 | 5): string {
        const labels: Record<1 | 2 | 3 | 4 | 5, string> = {
            1: 'Lunes',
            2: 'Martes',
            3: 'Miercoles',
            4: 'Jueves',
            5: 'Viernes'
        };
        return labels[day];
    }
}

