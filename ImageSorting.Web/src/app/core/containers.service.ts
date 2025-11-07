import { HttpClient } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { ContainerDto } from "./models/container/container.dto";

@Injectable({ providedIn: 'root' })

export class ContainersService {
    constructor(private readonly http: HttpClient) {
    }
    // App-managed containers (DB)
    appContainers(): Observable<ContainerDto[]> {
        return this.http.get<ContainerDto[]>('/api/containers');
    }

    createContainer(name: string): Observable<ContainerDto> {
        return this.http.post<ContainerDto>('/api/containers', { name });
    }
}