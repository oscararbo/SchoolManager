import { APP_INITIALIZER, ApplicationConfig, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { AuthStateService } from './core/services/auth-state.service';
import { SessionService } from './core/services/session.service';

function initializeAuth(authState: AuthStateService, session: SessionService) {
    return async () => {
    // If session metadata exists but token is missing, it is stale state.
    // We clear it instead of forcing a refresh request on app bootstrap.
    if (session.getSession() && !session.getToken()) {
      authState.markExpired('Tu sesion ha caducado. Inicia sesion de nuevo.');
        }
    };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
    provideAnimationsAsync(),
    {
      provide: APP_INITIALIZER,
      useFactory: initializeAuth,
      deps: [AuthStateService, SessionService],
      multi: true
    }
  ]
};
