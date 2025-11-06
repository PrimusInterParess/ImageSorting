import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { UploadService, BlobUploadItemWithMetadata } from '../../core/upload.service';
import { debounce } from 'rxjs';

@Component({
  selector: 'app-upload',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <h2>Upload to Blob Storage</h2>
    <form (ngSubmit)="onUpload()">
      <div>
        <label>Container</label>
        <input type="text" [(ngModel)]="container" name="container" required />
      </div>
      <div>
        <label>Prefix</label>
        <input type="text" [(ngModel)]="prefix" name="prefix" />
      </div>
      <div>
        <label>Files</label>
        <input type="file" multiple (change)="onFilesSelected($event)" />
      </div>
      <div>
        <button type="submit" [disabled]="uploading || !container || selectedFiles.length === 0">Upload</button>
        <a [routerLink]="['/gallery']" style="margin-left:1rem">View Gallery</a>
      </div>
    </form>

    <div *ngIf="uploading">Uploading...</div>

    <div *ngIf="result?.length">
      <h3>Uploaded</h3>
      <ul>
        <li *ngFor="let item of result">{{ item.blobName }}</li>
      </ul>
    </div>
  `,
  styles: ``
})
export class UploadComponent {
  container = '';
  prefix: string | null = '';
  selectedFiles: File[] = [];
  uploading = false;
  result: BlobUploadItemWithMetadata[] | null = null;

  constructor(private readonly uploadService: UploadService) { }

  onFilesSelected(event: Event) {
    debugger;
    const input = event.target as HTMLInputElement;
    this.selectedFiles = Array.from(input.files ?? []);
  }

  onUpload() {
    debugger;
    if (!this.container || this.selectedFiles.length === 0) return;
    this.uploading = true;
    this.result = null;
    this.uploadService.upload(this.container, this.prefix, this.selectedFiles).subscribe({
      next: r => {
        this.result = r.items;
        this.uploading = false;
      },
      error: _ => {
        this.uploading = false;
      }
    });
  }
}
