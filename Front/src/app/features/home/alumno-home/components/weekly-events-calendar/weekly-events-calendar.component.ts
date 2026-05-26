import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, computed, signal } from '@angular/core';

export interface CalendarEvent {
    id: string;
    title: string;
    subtitle?: string;
    description?: string;
    dayOfWeek: 1 | 2 | 3 | 4 | 5;
    startTime: string;
    endTime: string;
    location?: string;
    colorClass: 'blue' | 'green' | 'orange' | 'pink' | 'slate' | 'purple' | 'red';
    type: 'admin' | 'personal';
    repeatWeekly?: boolean;
}

export interface SubjectColorLegendItem {
    label: string;
    colorClass: CalendarEvent['colorClass'];
}

/** Alias for backwards compatibility */
export type WeeklyCalendarEvent = CalendarEvent;

interface WeekDayCell {
    date: Date;
    dayOfWeek: 1 | 2 | 3 | 4 | 5;
    shortName: string;
    dayNumber: string;
    monthShort: string;
    isoDate: string;
}

interface PositionedCalendarEvent extends CalendarEvent {
    left: string;
    width: string;
}

@Component({
    selector: 'app-weekly-events-calendar',
    standalone: true,
    imports: [],
    templateUrl: './weekly-events-calendar.component.html',
    styleUrl: './weekly-events-calendar.component.scss',
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class WeeklyEventsCalendarComponent {
    @Input() adminEvents: CalendarEvent[] = [];
    @Input() adminSubjectLegend: SubjectColorLegendItem[] = [];
    @Input() personalEvents: CalendarEvent[] = [];
    @Output() readonly requestAddEvent = new EventEmitter<{ dayOfWeek: 1 | 2 | 3 | 4 | 5 }>();
    @Output() readonly requestDeletePersonalEvent = new EventEmitter<string>();

    readonly GRID_START_HOUR = 0;
    readonly GRID_END_HOUR = 24;
    readonly HOUR_HEIGHT = 64;
    readonly hours: number[] = Array.from(
        { length: this.GRID_END_HOUR - this.GRID_START_HOUR },
        (_, i) => this.GRID_START_HOUR + i
    );
    readonly totalGridHeight = (this.GRID_END_HOUR - this.GRID_START_HOUR) * this.HOUR_HEIGHT;

    private readonly focusedDate = signal<Date>(this.getDefaultFocusDate());
    readonly weekStart = signal<Date>(this.getDisplayWeekStartForNow());
    readonly viewMode = signal<'week' | 'day'>('week');

    readonly days = computed<WeekDayCell[]>(() => {
        const start = this.weekStart();
        const shortFmt = new Intl.DateTimeFormat('es-ES', { weekday: 'short' });
        const dayFmt = new Intl.DateTimeFormat('es-ES', { day: '2-digit' });
        const monthFmt = new Intl.DateTimeFormat('es-ES', { month: 'short' });
        return [0, 1, 2, 3, 4].map(offset => {
            const date = this.addDays(start, offset);
            return {
                date,
                dayOfWeek: (offset + 1) as 1 | 2 | 3 | 4 | 5,
                shortName: shortFmt.format(date).replace('.', ''),
                dayNumber: dayFmt.format(date),
                monthShort: monthFmt.format(date),
                isoDate: this.toIsoDate(date)
            };
        });
    });

    readonly weekLabel = computed(() => {
        const start = this.weekStart();
        const end = this.addDays(start, 4);
        const fmt = new Intl.DateTimeFormat('es-ES', { day: '2-digit', month: 'short' });
        return `${fmt.format(start)} - ${fmt.format(end)}`;
    });

    readonly dayLabel = computed(() => {
        const focused = this.focusedDate();
        const fmt = new Intl.DateTimeFormat('es-ES', { weekday: 'long', day: '2-digit', month: 'short' });
        return fmt.format(focused);
    });

    readonly periodLabel = computed(() => this.viewMode() === 'week' ? this.weekLabel() : this.dayLabel());

    readonly visibleDays = computed<WeekDayCell[]>(() => {
        const all = this.days();
        if (this.viewMode() === 'week') {
            return all;
        }

        const focusedIso = this.toIsoDate(this.focusedDate());
        const focused = all.find(day => day.isoDate === focusedIso);
        return focused ? [focused] : [all[0]];
    });

    readonly selectedWeekInputValue = computed(() => this.toIsoDate(this.weekStart()));
    readonly selectedDayInputValue = computed(() => this.toIsoDate(this.focusedDate()));

    allEventsByDay(dayOfWeek: 1 | 2 | 3 | 4 | 5): CalendarEvent[] {
        return [
            ...this.adminEvents.filter(e => e.dayOfWeek === dayOfWeek),
            ...this.personalEvents.filter(e => e.dayOfWeek === dayOfWeek)
        ].sort((a, b) => a.startTime.localeCompare(b.startTime));
    }

    getPositionedEvents(dayOfWeek: 1 | 2 | 3 | 4 | 5): PositionedCalendarEvent[] {
        const events = this.allEventsByDay(dayOfWeek)
            .map(event => ({
                ...event,
                _start: this.toMinutes(event.startTime),
                _end: this.toMinutes(event.endTime),
                _column: 0,
                _columns: 1,
            }))
            .sort((a, b) => a._start - b._start || a._end - b._end || a.title.localeCompare(b.title));

        if (events.length === 0) {
            return [];
        }

        const clusters: Array<typeof events> = [];
        let currentCluster: typeof events = [];
        let currentClusterEnd = -1;

        for (const event of events) {
            if (currentCluster.length === 0 || event._start < currentClusterEnd) {
                currentCluster.push(event);
                currentClusterEnd = Math.max(currentClusterEnd, event._end);
                continue;
            }

            clusters.push(currentCluster);
            currentCluster = [event];
            currentClusterEnd = event._end;
        }

        if (currentCluster.length > 0) {
            clusters.push(currentCluster);
        }

        const positioned: PositionedCalendarEvent[] = [];

        for (const cluster of clusters) {
            const columnEndTimes: number[] = [];

            for (const event of cluster) {
                let columnIndex = columnEndTimes.findIndex(endTime => endTime <= event._start);
                if (columnIndex < 0) {
                    columnIndex = columnEndTimes.length;
                    columnEndTimes.push(event._end);
                } else {
                    columnEndTimes[columnIndex] = event._end;
                }

                event._column = columnIndex;
            }

            const totalColumns = Math.max(1, columnEndTimes.length);
            for (const event of cluster) {
                event._columns = totalColumns;
                const leftPercent = (event._column / totalColumns) * 100;
                const widthPercent = 100 / totalColumns;

                positioned.push({
                    id: event.id,
                    title: event.title,
                    subtitle: event.subtitle,
                    description: event.description,
                    dayOfWeek: event.dayOfWeek,
                    startTime: event.startTime,
                    endTime: event.endTime,
                    location: event.location,
                    colorClass: event.colorClass,
                    type: event.type,
                    repeatWeekly: event.repeatWeekly,
                    left: `calc(${leftPercent}% + 2px)`,
                    width: `calc(${widthPercent}% - 4px)`
                });
            }
        }

        return positioned;
    }

    getEventTop(event: CalendarEvent): string {
        const [h, m] = event.startTime.split(':').map(Number);
        const minutes = (h - this.GRID_START_HOUR) * 60 + m;
        return `${(minutes / 60) * this.HOUR_HEIGHT}px`;
    }

    getEventHeight(event: CalendarEvent): string {
        const [sh, sm] = event.startTime.split(':').map(Number);
        const [eh, em] = event.endTime.split(':').map(Number);
        const duration = (eh * 60 + em) - (sh * 60 + sm);
        return `${Math.max((duration / 60) * this.HOUR_HEIGHT, 28)}px`;
    }

    getCurrentTimeTop(): string | null {
        const now = new Date();
        const total = (now.getHours() - this.GRID_START_HOUR) * 60 + now.getMinutes();
        if (total < 0 || total >= (this.GRID_END_HOUR - this.GRID_START_HOUR) * 60) return null;
        return `${(total / 60) * this.HOUR_HEIGHT}px`;
    }

    formatHour(hour: number): string {
        return `${String(hour).padStart(2, '0')}:00`;
    }

    onAddEventForDay(dayOfWeek: 1 | 2 | 3 | 4 | 5): void {
        this.requestAddEvent.emit({ dayOfWeek });
    }

    onDeletePersonalEvent(id: string, ev: MouseEvent): void {
        ev.stopPropagation();
        this.requestDeletePersonalEvent.emit(id);
    }

    goToPreviousWeek(): void {
        if (this.viewMode() === 'day') {
            const previous = this.addWeekdays(this.focusedDate(), -1);
            this.focusedDate.set(previous);
            this.weekStart.set(this.getWeekStart(previous));
            return;
        }

        this.weekStart.set(this.addDays(this.weekStart(), -7));
        this.focusedDate.set(this.addDays(this.focusedDate(), -7));
    }

    goToNextWeek(): void {
        if (this.viewMode() === 'day') {
            const next = this.addWeekdays(this.focusedDate(), 1);
            this.focusedDate.set(next);
            this.weekStart.set(this.getWeekStart(next));
            return;
        }

        this.weekStart.set(this.addDays(this.weekStart(), 7));
        this.focusedDate.set(this.addDays(this.focusedDate(), 7));
    }

    goToTodayContext(): void {
        this.focusedDate.set(this.getDefaultFocusDate());
        this.weekStart.set(this.getDisplayWeekStartForNow());
    }

    onWeekDateSelected(value: string): void {
        if (!value) return;
        const adjusted = this.adjustDateToWeekday(this.parseIsoDate(value));
        this.focusedDate.set(adjusted);
        this.weekStart.set(this.getWeekStart(adjusted));
    }

    onDayDateSelected(value: string): void {
        if (!value) return;
        const adjusted = this.adjustDateToWeekday(this.parseIsoDate(value));
        this.focusedDate.set(adjusted);
        this.weekStart.set(this.getWeekStart(adjusted));
    }

    onDatePickerChanged(value: string): void {
        if (this.viewMode() === 'week') {
            this.onWeekDateSelected(value);
            return;
        }

        this.onDayDateSelected(value);
    }

    setWeekView(): void {
        this.viewMode.set('week');
    }

    setDayView(): void {
        this.viewMode.set('day');
        this.focusedDate.set(this.adjustDateToWeekday(this.focusedDate()));
    }

    isFocusedDay(day: WeekDayCell): boolean {
        return day.isoDate === this.toIsoDate(this.focusedDate());
    }

    isToday(day: WeekDayCell): boolean {
        return day.isoDate === this.toIsoDate(this.startOfDay(new Date()));
    }

    private getDisplayWeekStartForNow(): Date {
        return this.getWeekStart(this.getDefaultFocusDate());
    }

    private getDefaultFocusDate(): Date {
        const today = this.startOfDay(new Date());
        const day = today.getDay();

        if (day === 6) {
            return this.addDays(today, 2);
        }
        if (day === 0) {
            return this.addDays(today, 1);
        }
        return today;
    }

    private adjustDateToWeekday(date: Date): Date {
        const day = date.getDay();
        if (day === 6) {
            return this.addDays(date, 2);
        }
        if (day === 0) {
            return this.addDays(date, 1);
        }
        return date;
    }

    private getWeekStart(date: Date): Date {
        const normalizedDate = this.startOfDay(date);
        const day = normalizedDate.getDay();
        const diff = day === 0 ? -6 : 1 - day;
        return this.addDays(normalizedDate, diff);
    }

    private startOfDay(date: Date): Date {
        return new Date(date.getFullYear(), date.getMonth(), date.getDate());
    }

    private addDays(date: Date, days: number): Date {
        const clone = this.startOfDay(date);
        clone.setDate(clone.getDate() + days);
        return clone;
    }

    private addWeekdays(date: Date, amount: number): Date {
        const step = amount >= 0 ? 1 : -1;
        let remaining = Math.abs(amount);
        let current = this.startOfDay(date);

        while (remaining > 0) {
            current = this.addDays(current, step);
            const day = current.getDay();
            if (day !== 0 && day !== 6) {
                remaining--;
            }
        }

        return current;
    }

    private toIsoDate(date: Date): string {
        const year = date.getFullYear();
        const month = `${date.getMonth() + 1}`.padStart(2, '0');
        const day = `${date.getDate()}`.padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    private parseIsoDate(value: string): Date {
        const [year, month, day] = value.split('-').map(Number);
        return this.startOfDay(new Date(year, month - 1, day));
    }

    private toMinutes(value: string): number {
        const [hours, minutes] = value.split(':').map(Number);
        return (hours * 60) + minutes;
    }
}
