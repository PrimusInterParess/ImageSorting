import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BlobUploadWithMetadataResponse } from './models/blob/blob-upload-withmetadata-response.dto';
import { BlobListResponse } from './models/blob/blob-list-response';
import { ContainerListResponse } from './models/container/container-lists-response';

export interface PrefixListResponse {
	count: number;
	items: string[];
}

@Injectable({
	providedIn: 'root'
})
export class UploadService {

	constructor(private readonly http: HttpClient) { }

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

	listContainers(): Observable<ContainerListResponse> {
		return this.http.get<ContainerListResponse>('/api/blob/containers');
	}

	listPrefixes(container: string, prefix?: string | null): Observable<PrefixListResponse> {
		let params = new HttpParams().set('container', container);
		if (prefix) { params = params.set('prefix', prefix); }
		return this.http.get<PrefixListResponse>('/api/blob/prefixes', { params });
	}


}
