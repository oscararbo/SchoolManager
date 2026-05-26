import { ChangeDetectionStrategy, Component, Input, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AlumnoHorarioClase, AlumnoPanelResumen, SchoolApiService } from '../../../../../shared/services/school-api.service';
import { CalendarEvent, SubjectColorLegendItem, WeeklyEventsCalendarComponent } from '../weekly-events-calendar/weekly-events-calendar.component';

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
    @Input({ required: true }) set panel(value: AlumnoPanelResumen | null) {
        const estudianteId = value?.id ?? null;
        if (estudianteId && estudianteId !== this.lastLoadedStudentId) {
            this.lastLoadedStudentId = estudianteId;
            void this.cargarHorarioAsignaturas(estudianteId);
        }
    }

    private api = inject(SchoolApiService);
    private lastLoadedStudentId: number | null = null;

    readonly loadingSchedule = signal(false);
    readonly scheduleError = signal<string | null>(null);

    // ── Admin events (read-only, set by admin) ──────────────────────────────
    readonly adminEvents = signal<CalendarEvent[]>([]);
    readonly adminSubjectLegend = signal<SubjectColorLegendItem[]>([]);

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

    private async cargarHorarioAsignaturas(estudianteId: number): Promise<void> {
        this.loadingSchedule.set(true);
        this.scheduleError.set(null);
        try {
            const horario = await this.api.getHorarioAlumno(estudianteId);
            const adminPalette: CalendarEvent['colorClass'][] = ['blue', 'green', 'orange', 'pink', 'slate', 'purple', 'red'];
            const colorByAsignaturaId = new Map<number, CalendarEvent['colorClass']>();

            for (const item of [...horario].sort((a, b) => a.asignatura.localeCompare(b.asignatura))) {
                if (colorByAsignaturaId.has(item.asignaturaId)) {
                    continue;
                }

                const nextColor = adminPalette[colorByAsignaturaId.size % adminPalette.length];
                colorByAsignaturaId.set(item.asignaturaId, nextColor);
            }

            const legendItems = [...horario]
                .sort((a, b) => a.asignatura.localeCompare(b.asignatura))
                .filter((item, index, all) => all.findIndex(x => x.asignaturaId === item.asignaturaId) === index)
                .map(item => ({
                    label: item.asignatura,
                    colorClass: this.getAdminColor(item.asignaturaId, colorByAsignaturaId)
                } satisfies SubjectColorLegendItem));

            this.adminEvents.set(horario
                .filter(item => item.diaSemana >= 1 && item.diaSemana <= 5)
                .map(item => this.toCalendarEvent(item, colorByAsignaturaId)));
            this.adminSubjectLegend.set(legendItems);
        } catch (error) {
            this.adminEvents.set([]);
            this.adminSubjectLegend.set([]);
            this.scheduleError.set((error as Error).message);
        } finally {
            this.loadingSchedule.set(false);
        }
    }

    private toCalendarEvent(
        item: AlumnoHorarioClase,
        colorByAsignaturaId: Map<number, CalendarEvent['colorClass']>
    ): CalendarEvent {
        return {
            id: `admin-${item.horarioId}`,
            title: item.asignatura,
            subtitle: item.profesor ?? undefined,
            location: item.aula ?? undefined,
            colorClass: this.getAdminColor(item.asignaturaId, colorByAsignaturaId),
            dayOfWeek: item.diaSemana as 1 | 2 | 3 | 4 | 5,
            startTime: this.normalizeTime(item.horaInicio),
            endTime: this.normalizeTime(item.horaFin),
            type: 'admin',
            repeatWeekly: true,
        };
    }

    private normalizeTime(value: string): string {
        const [hours = '00', minutes = '00'] = value.split(':');
        return `${hours.padStart(2, '0')}:${minutes.padStart(2, '0')}`;
    }

    private getAdminColor(
        asignaturaId: number,
        colorByAsignaturaId: Map<number, CalendarEvent['colorClass']>
    ): CalendarEvent['colorClass'] {
        return colorByAsignaturaId.get(asignaturaId) ?? 'blue';
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

