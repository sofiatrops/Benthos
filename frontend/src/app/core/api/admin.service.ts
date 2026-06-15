import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Campania, Centro, Empresa, PagedResult } from './models';

/**
 * Back-office del personal de Benthos. A diferencia del Portal, opera sobre una
 * empresa explícita (la `empresaId` viaja en la URL); la API exige rol de personal.
 */
@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/empresas`;

  empresas(search = '', page = 1, pageSize = 20): Observable<PagedResult<Empresa>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) {
      params = params.set('search', search);
    }
    return this.http.get<PagedResult<Empresa>>(this.base, { params });
  }

  empresa(id: string): Observable<Empresa> {
    return this.http.get<Empresa>(`${this.base}/${id}`);
  }

  centros(empresaId: string, page = 1, pageSize = 50): Observable<PagedResult<Centro>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<Centro>>(`${this.base}/${empresaId}/centros`, { params });
  }

  campanas(empresaId: string, page = 1, pageSize = 50): Observable<PagedResult<Campania>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<Campania>>(`${this.base}/${empresaId}/campanas`, { params });
  }

  // --- Escritura ---

  crearEmpresa(input: NuevaEmpresa): Observable<string> {
    return this.http.post<string>(this.base, input);
  }

  crearCentro(empresaId: string, input: NuevoCentro): Observable<string> {
    return this.http.post<string>(`${this.base}/${empresaId}/centros`, input);
  }

  crearCampana(empresaId: string, input: NuevaCampana): Observable<string> {
    return this.http.post<string>(`${this.base}/${empresaId}/campanas`, input);
  }
}

export interface NuevaEmpresa {
  razonSocial: string;
  rut: string;
  rubro: string;
}

export interface NuevoCentro {
  nombre: string;
  codigoInterno: string;
  latitud: number;
  longitud: number;
  region: string;
}

export interface NuevaCampana {
  nombre: string;
  descripcion: string;
  tipo: string;
  fechaInicio: string;
  fechaFin: string;
  centroIds: string[];
}

/** Tipos de campaña (coinciden con el enum del backend, enviados como texto). */
export const TIPOS_CAMPANA = ['CalidadAgua', 'Macroinvertebrados', 'Microinvertebrados', 'Mixta'] as const;
