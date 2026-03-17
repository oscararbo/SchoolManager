import { Routes } from '@angular/router';
import { authGuard, unauthGuard } from './core/guards/auth.guard';

export const routes: Routes = [
    {
        path: '',
        canActivate: [unauthGuard],
        loadComponent: () => import('./layouts/auth-layout/auth-layout').then(m => m.AuthLayout)
    },
    {
        path: 'home',
        canActivate: [authGuard],
        loadComponent: () => import('./layouts/home-layout/home-layout').then(m => m.HomeLayout)
    },
    {
        path: '**',
        redirectTo: ''
    }
];
