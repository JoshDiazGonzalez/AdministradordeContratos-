import { Pipe, PipeTransform } from '@angular/core';

/**
 * Formatea un instante ISO 8601 ("2026-09-16T16:13:19Z") como fecha y hora en la
 * zona horaria del negocio: "16/09/2026, 11:13".
 *
 * Se fija America/Guayaquil en lugar de la zona del navegador. Es la misma zona
 * con la que el backend calcula los estados; si un usuario abre el sistema desde
 * otro pais, las horas de registro siguen coincidiendo con las de la oficina.
 */
const FORMATO = new Intl.DateTimeFormat('es-EC', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
  hour12: false,
  timeZone: 'America/Guayaquil',
});

@Pipe({ name: 'fechaHora' })
export class FechaHoraPipe implements PipeTransform {
  transform(valor: string | null | undefined): string {
    if (!valor) {
      return '—';
    }

    const instante = new Date(valor);
    return Number.isNaN(instante.getTime()) ? '—' : FORMATO.format(instante);
  }
}
