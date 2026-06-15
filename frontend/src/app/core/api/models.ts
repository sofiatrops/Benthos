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
