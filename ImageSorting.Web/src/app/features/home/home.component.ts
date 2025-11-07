import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UploadService } from '../../core/upload.service';
import { BlobItemInfo } from '../../core/models/blob/blob-item-info';
import { tap } from 'rxjs';

@Component({
    selector: 'app-home',
    standalone: true,
    imports: [CommonModule, FormsModule],
    templateUrl: 'home.component.html',
    styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {
    // Hard-coded container and starting prefix as requested
    container: string = 'media-src';
    prefix: string | null = 'imports/';

    prefixes: string[] = [];
    items: BlobItemInfo[] = [];
    loading = false;
    visibleCount = 30;
    pageSize = 30;
    hovering: string | null = null;

    constructor(private readonly uploadService: UploadService) { }

    ngOnInit(): void {
        this.load();
    }

    private ensureTrailingSlash(p?: string | null): string | null {
        if (!p) return null;
        const trimmed = p.replace(/^\/+|\/+$/g, '');
        return trimmed ? trimmed + '/' : '';
    }

    private normalizeForQuery(p?: string | null): string | null {
        const withSlash = this.ensureTrailingSlash(p);
        // Use null when empty so the API lists root
        return withSlash === '' ? null : withSlash;
    }

    load() {
        if (!this.container) return;
        this.loading = true;
        const prefixForQuery = this.normalizeForQuery(this.prefix);
        this.uploadService.listPrefixes(this.container, prefixForQuery).subscribe({
            next: r => {
                // this.prefixes = r.items.length ?? [];
                if (r.items.length !== 0) {
                    this.prefixes = r.items
                }

                if ((r.items?.length || 0) > 0) {
                    // We have subfolders: show only folders on this load
                    this.items = [];
                    this.visibleCount = this.pageSize;
                    this.loading = false;
                } else {
                    // No subfolders: load files for the current prefix
                    this.uploadService.list(this.container, prefixForQuery).subscribe({
                        next: filesResp => {
                            this.items = filesResp.items ?? [];
                            this.visibleCount = Math.min(this.pageSize, this.items.length);
                            this.loading = false;
                        },
                        error: _ => {
                            this.items = [];
                            this.loading = false;
                        }
                    });
                }
            },
            error: _ => {
                this.prefixes = [];
                this.items = [];
                this.loading = false;
            }
        });
    }

    openFolder(p: string) {
        // p is full prefix without trailing slash from API
        const next = this.ensureTrailingSlash(p);
        this.prefix = next ?? '';
        this.load();
    }

    goUp() {
        const current = (this.prefix ?? '').replace(/\/+$/g, '');
        if (!current) return;
        const idx = current.lastIndexOf('/');
        const parent = idx >= 0 ? current.substring(0, idx + 1) : '';
        this.prefix = parent;
        this.load();
    }

    breadcrumbSegments(): string[] {
        const current = (this.prefix ?? '').replace(/\/+$/g, '');
        if (!current) return [];
        return current.split('/').filter(s => !!s);
    }

    navigateToCrumb(index: number) {
        const segs = this.breadcrumbSegments().slice(0, index + 1);
        const target = segs.length ? segs.join('/') + '/' : '';
        this.prefix = target;
        this.load();
    }

    displaySegment(p: string): string {
        const parts = p.split('/');
        return parts[parts.length - 1] || p;
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


