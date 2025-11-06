import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface FileMetadataDto {
	bestDateTakenUtc?: string | null;
	bestDateSource?: string | null;
	width?: number | null;
	height?: number | null;
	cameraMake?: string | null;
	cameraModel?: string | null;
	contentType?: string | null;
	extension?: string | null;
}

export interface BlobUploadItemWithMetadata {
	blobName: string;
	metadata: FileMetadataDto;
}

export interface BlobUploadWithMetadataResponse {
	uploaded: number;
	items: BlobUploadItemWithMetadata[];
}

export interface BlobItemInfo {
	name: string;
	size?: number | null;
	contentType?: string | null;
	lastModified?: string | null;
}

export interface BlobListResponse {
	count: number;
	items: BlobItemInfo[];
}

@Injectable({
  providedIn: 'root'
})
export class UploadService {

	constructor(private readonly http: HttpClient) {}

	upload(container: string, prefix: string | null, files: File[]): Observable<BlobUploadWithMetadataResponse> {
		const form = new FormData();
		form.append('container', container);
		if (prefix) { form.append('prefix', prefix); }
		for (const f of files) { form.append('files', f, f.name); }
		return this.http.post<BlobUploadWithMetadataResponse>('/api/upload/blob-with-metadata', form);
	}

	list(container: string, prefix?: string | null): Observable<BlobListResponse> {
		let params = new HttpParams().set('container', container);
		if (prefix) { params = params.set('prefix', prefix); }
		return this.http.get<BlobListResponse>('/api/blob/list', { params });
	}

	contentUrl(container: string, name: string): string {
		const p = new URLSearchParams({ container, name });
		return `/api/blob/content?${p.toString()}`;
	}
}
