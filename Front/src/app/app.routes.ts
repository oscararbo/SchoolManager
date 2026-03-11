import { AuthLayout } from './layouts/auth-layout/auth-layout';
import { Routes } from '@angular/router';

export const routes: Routes = [
    {
        path: '',
        loadComponent: () => import('./layouts/auth-layout/auth-layout').then(m => m.AuthLayout)
    }
];
