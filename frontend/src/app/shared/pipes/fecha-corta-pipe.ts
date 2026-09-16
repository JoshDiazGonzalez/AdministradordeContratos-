import { Pipe, PipeTransform } from '@angular/core';

const FECHA_ISO = /^(\d{4})-(\d{2})-(\d{2})$/;

/**
 * Formatea una fecha sin hora ("2026-01-01") como "01/01/2026".
 *
 * No usa new Date() a proposito. La API envia fechas sin hora, y
 * new Date('2026-01-01') las interpreta como medianoche UTC: en Ecuador (UTC-5)
 * eso es el 31/12/2025 a las 19:00. La tabla mostraria cada contrato un dia
 * antes, y uno que vence el 1 de enero apareceria con el ano anterior.
 * Leer los componentes de la cadena evita cualquier conversion de zona horaria.
 */
@Pipe({ name: 'fechaCorta' })
export class FechaCortaPipe implements PipeTransform {
  transform(valor: string | null | undefined): string {
    const partes = valor ? FECHA_ISO.exec(valor) : null;

    if (!partes) {
      return '—';
    }

    const [, anio, mes, dia] = partes;
    return `${dia}/${mes}/${anio}`;
  }
}
