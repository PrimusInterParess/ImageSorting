import { Routes } from '@angular/router';

export const routes: Routes = [
	{ path: '', loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent) },
	{ path: 'upload', loadComponent: () => import('./features/upload/upload.component').then(m => m.UploadComponent) },
]
