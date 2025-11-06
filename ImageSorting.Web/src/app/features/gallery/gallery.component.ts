import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UploadService, BlobItemInfo } from '../../core/upload.service';

@Component({
  selector: 'app-gallery',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: 'gallery.component.html',
  styleUrls: ['gallery.component.scss']
})
export class GalleryComponent {
  container = '';
  prefix: string | null = '';
  items: BlobItemInfo[] = [];
  loading = false;
  visibleCount = 30;
  pageSize = 30;
  hovering: string | null = null;

  constructor(private readonly uploadService: UploadService) { }

  load() {
    if (!this.container) return;
    this.loading = true;
    this.uploadService.list(this.container, this.prefix).subscribe({
      next: r => {
        this.items = r.items;
        this.visibleCount = this.pageSize;
        this.loading = false;
      },
      error: _ => {
        this.loading = false;
      }
    });
  }

  contentUrl(name: string) {
    return this.uploadService.contentUrl(this.container, name);
  }

  isVideo(name: string) {
    const lower = name.toLowerCase();
    return lower.endsWith('.mp4') || lower.endsWith('.mov') || lower.endsWith('.webm');
  }

  hoverEnter(name: string, video?: HTMLVideoElement) {
    this.hovering = name;
    if (video) {
      try {
        video.play().catch(() => { });
      } catch { }
    }
  }

  hoverLeave(_name: string, video?: HTMLVideoElement) {
    this.hovering = null;
    if (video) {
      try {
        video.pause();
        video.currentTime = 0;
      } catch { }
    }
  }
}
