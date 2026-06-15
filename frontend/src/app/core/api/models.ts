/** Modelos del Portal Cliente, alineados con los DTOs del backend (camelCase JSON). */

export interface InformeResumen {
  id: string;
  titulo: string;
  tipoEstudio: string;
  estado: string;
  periodoDesde: string;
  periodoHasta: string;
  versionVigenteNumero: number;
  creadoUtc: string;
}

export interface Kpi {
  nombre: string;
  valor: number;
  unidad: string;
}

export interface Dashboard {
  campanasActivas: number;
  ultimosInformesPublicados: InformeResumen[];
  kpis: Kpi[];
  resumenAnalisis: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface PortalAnexo {
  descripcion: string;
  fechaUtc: string;
  urlDescarga: string;
}

// --- Back-office (personal Benthos) ---

export interface Empresa {
  id: string;
  razonSocial: string;
  rut: string;
  rubro: string;
  activa: boolean;
  creadaUtc: string;
}

export interface Centro {
  id: string;
  empresaId: string;
  nombre: string;
  codigoInterno: string;
  latitud: number;
  longitud: number;
  region: string;
  activo: boolean;
}

export interface Campania {
  id: string;
  empresaId: string;
  nombre: string;
  descripcion: string;
  tipo: string;
  estado: string;
  fechaInicio: string;
  fechaFin: string;
  centroIds: string[];
}

export interface InformePublicadoDetalle {
  id: string;
  titulo: string;
  tipoEstudio: string;
  periodoDesde: string;
  periodoHasta: string;
  campanaId: string | null;
  centroId: string | null;
  fechaAprobacionUtc: string | null;
  versionVigenteNumero: number;
  urlDescarga: string | null;
  anexos: PortalAnexo[];
}
