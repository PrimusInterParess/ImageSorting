import { Routes } from '@angular/router';

export const routes: Routes = [
	{ path: '', loadComponent: () => import('./features/upload/upload.component').then(m => m.UploadComponent) },
	{ path: 'gallery', loadComponent: () => import('./features/gallery/gallery.component').then(m => m.GalleryComponent) }
];
