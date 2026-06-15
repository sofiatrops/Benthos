import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Dashboard, InformePublicadoDetalle, InformeResumen, PagedResult } from './models';

/**
 * Acceso al Portal Cliente. El tenant se deriva del JWT en el backend (RF-07-010):
 * ningún endpoint lleva `empresaId`, es imposible pedir datos de otra empresa.
 */
@Injectable({ providedIn: 'root' })
export class PortalService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/portal`;

  dashboard(): Observable<Dashboard> {
    return this.http.get<Dashboard>(`${this.base}/dashboard`);
  }

  informesPublicados(page = 1, pageSize = 20): Observable<PagedResult<InformeResumen>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<InformeResumen>>(`${this.base}/informes`, { params });
  }

  informe(id: string): Observable<InformePublicadoDetalle> {
    return this.http.get<InformePublicadoDetalle>(`${this.base}/informes/${id}`);
  }
}
