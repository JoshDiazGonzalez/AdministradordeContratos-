import { ParamMap, Params } from '@angular/router';

import { CONTRATO_ESTADOS, ContratoEstado, ContratoFiltro } from '../../core/models/contrato.model';

export const TAMANOS_PAGINA = [10, 20, 50] as const;
const TAMANO_POR_DEFECTO = 10;
const LONGITUD_MAXIMA_PROVEEDOR = 200;

/** Filtro del listado ya validado, con paginacion siempre presente. */
export type FiltroListado = ContratoFiltro & { page: number; pageSize: number };

/** Criterios de busqueda (todo lo que no es paginacion). */
export type CriteriosBusqueda = Omit<ContratoFiltro, 'page' | 'pageSize'>;

export const CAMPOS_FECHA = [
  'fechaInicioDesde',
  'fechaInicioHasta',
  'fechaVencimientoDesde',
  'fechaVencimientoHasta',
] as const;

/**
 * Comprueba que el texto sea una fecha real en formato yyyy-MM-dd.
 * "2026-02-30" tiene el formato correcto pero no existe: la API lo rechaza.
 */
export function esFechaIso(valor: string | null | undefined): valor is string {
  const partes = valor ? /^(\d{4})-(\d{2})-(\d{2})$/.exec(valor) : null;
  if (!partes) {
    return false;
  }

  const [anio, mes, dia] = [Number(partes[1]), Number(partes[2]), Number(partes[3])];
  // Date.UTC normaliza fechas imposibles (30 de febrero -> 2 de marzo): si el
  // resultado no coincide con lo escrito, la fecha no existe. Se usa UTC para
  // que la zona horaria del navegador no influya.
  const fecha = new Date(Date.UTC(anio, mes - 1, dia));
  return fecha.getUTCFullYear() === anio && fecha.getUTCMonth() === mes - 1 && fecha.getUTCDate() === dia;
}

/** true si ambos extremos existen y el inicial es posterior al final. */
export function rangoInvertido(desde?: string, hasta?: string): boolean {
  // Las cadenas yyyy-MM-dd se ordenan igual que las fechas que representan.
  return !!desde && !!hasta && desde > hasta;
}

/**
 * Lee filtros y paginacion de la URL.
 *
 * La URL la puede escribir cualquiera, asi que se descarta lo que no sea valido
 * (estados inexistentes, fechas imposibles, tamanos fuera de la lista) en lugar
 * de enviarlo a la API y mostrar un error por un enlace mal copiado.
 */
export function leerFiltroDeUrl(parametros: ParamMap): FiltroListado {
  const filtro: FiltroListado = { ...leerPaginacion(parametros) };

  const proveedor = parametros.get('proveedor')?.trim().slice(0, LONGITUD_MAXIMA_PROVEEDOR);
  if (proveedor) {
    filtro.proveedor = proveedor;
  }

  const estado = parametros.get('estado');
  if (estado && (CONTRATO_ESTADOS as readonly string[]).includes(estado)) {
    filtro.estado = estado as ContratoEstado;
  }

  for (const campo of CAMPOS_FECHA) {
    const valor = parametros.get(campo);
    if (esFechaIso(valor)) {
      filtro[campo] = valor;
    }
  }

  return filtro;
}

export function leerPaginacion(parametros: ParamMap): { page: number; pageSize: number } {
  const page = Number(parametros.get('page'));
  const pageSize = Number(parametros.get('pageSize'));

  return {
    page: Number.isInteger(page) && page >= 1 ? page : 1,
    pageSize: (TAMANOS_PAGINA as readonly number[]).includes(pageSize) ? pageSize : TAMANO_POR_DEFECTO,
  };
}

/** Mensaje si algun rango de fechas esta invertido; null si todo es coherente. */
export function errorDeRangos(criterios: CriteriosBusqueda): string | null {
  if (rangoInvertido(criterios.fechaInicioDesde, criterios.fechaInicioHasta)) {
    return 'La fecha de inicio "desde" no puede ser posterior a la fecha "hasta".';
  }

  if (rangoInvertido(criterios.fechaVencimientoDesde, criterios.fechaVencimientoHasta)) {
    return 'La fecha de vencimiento "desde" no puede ser posterior a la fecha "hasta".';
  }

  return null;
}

/**
 * Compara criterios campo a campo. No se usa JSON.stringify: dependeria del
 * orden en que se crearon las claves y un cambio inocente lo romperia.
 */
export function mismosCriterios(a: CriteriosBusqueda, b: CriteriosBusqueda): boolean {
  return (['proveedor', 'estado', ...CAMPOS_FECHA] as const).every(
    (campo) => (a[campo] || undefined) === (b[campo] || undefined),
  );
}

/** Cantidad de criterios aplicados, para el indicador "Filtros (2)". */
export function contarCriterios(criterios: CriteriosBusqueda): number {
  return (['proveedor', 'estado', ...CAMPOS_FECHA] as const).filter((campo) => !!criterios[campo]).length;
}

/**
 * Convierte criterios en parametros de URL. Los vacios van como null para que
 * el router los elimine de la URL en lugar de dejar "?proveedor=".
 */
export function criteriosAParametros(criterios: CriteriosBusqueda): Params {
  return {
    proveedor: criterios.proveedor?.trim() || null,
    estado: criterios.estado || null,
    fechaInicioDesde: criterios.fechaInicioDesde || null,
    fechaInicioHasta: criterios.fechaInicioHasta || null,
    fechaVencimientoDesde: criterios.fechaVencimientoDesde || null,
    fechaVencimientoHasta: criterios.fechaVencimientoHasta || null,
  };
}
