import { Routes } from '@angular/router';

export const routes: Routes = [
    {
        path: '',
        loadComponent: () => import('./layouts/auth-layout/auth-layout').then(m => m.AuthLayout)
    },
    {
        path: 'home',
        loadComponent: () => import('./layouts/home-layout/home-layout').then(m => m.HomeLayout)
    },
    {
        path: '**',
        redirectTo: ''
    }
];
