/**
 * Modelos del dominio de contratos.
 *
 * Reflejan exactamente los DTO de la API .NET (ContratoDto, ContratoFiltro,
 * ResultadoPaginado, ContratoResumenDto). Si la API cambia, estos tipos deben
 * cambiar con ella: son el contrato entre frontend y backend.
 */

/**
 * Estados de vigencia. La API los serializa como texto.
 *
 * El frontend nunca los calcula: los recibe resueltos por el backend, que es
 * la unica fuente de verdad de las reglas de vigencia.
 */
export type ContratoEstado = 'Activo' | 'PorVencer' | 'Vencido' | 'Inactivo';

/** Lista ordenada de estados, util para selectores y filtros. */
export const CONTRATO_ESTADOS: readonly ContratoEstado[] = [
  'Activo',
  'PorVencer',
  'Vencido',
  'Inactivo',
];

/** Texto legible de cada estado. Los badges siempre muestran texto, no solo color. */
export const CONTRATO_ESTADO_ETIQUETAS: Readonly<Record<ContratoEstado, string>> = {
  Activo: 'Activo',
  PorVencer: 'Por vencer',
  Vencido: 'Vencido',
  Inactivo: 'Inactivo',
};

export interface Contrato {
  id: string;
  nombreProveedor: string;
  /**
   * Importe con dos decimales. En la base es numeric(18,2); number de JavaScript
   * representa con exactitud cualquier importe realista de un contrato.
   */
  montoContrato: number;
  /** Fecha sin hora, formato ISO yyyy-MM-dd. */
  fechaInicio: string;
  /** Fecha sin hora, formato ISO yyyy-MM-dd. */
  fechaVencimiento: string;
  estado: ContratoEstado;
  descripcion: string;
  archivoNombre: string;
  archivoContentType: string;
  archivoTamanoBytes: number;
  /** Instante ISO 8601 con zona horaria. */
  fechaCreacion: string;
  fechaActualizacion: string | null;
}

/** Filtros del listado. Todos opcionales y combinables. */
export interface ContratoFiltro {
  /** Coincidencia parcial, sin distinguir mayusculas. */
  proveedor?: string;
  estado?: ContratoEstado;
  fechaInicioDesde?: string;
  fechaInicioHasta?: string;
  fechaVencimientoDesde?: string;
  fechaVencimientoHasta?: string;
  page?: number;
  pageSize?: number;
}

/** Datos del formulario de alta. El documento es obligatorio. */
export interface CrearContratoRequest {
  nombreProveedor: string;
  montoContrato: number;
  fechaInicio: string;
  fechaVencimiento: string;
  descripcion: string;
  archivo: File;
}

/** Conteo por estado para el dashboard. */
export interface ContratoResumen {
  total: number;
  activos: number;
  porVencer: number;
  vencidos: number;
  inactivos: number;
}
