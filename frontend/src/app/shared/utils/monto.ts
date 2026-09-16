/**
 * Lectura y formato de importes escritos por el usuario.
 *
 * No se usa <input type="number">: con el navegador en espanol, escribir
 * "15000,50" deja el valor vacio sin avisar, y ademas acepta "e", "+" y "-".
 *
 * Formato admitido (convencion de Ecuador, que usa dolares): punto decimal y
 * coma opcional como separador de miles. "15000.50" y "15,000.50" son validos.
 */

/**
 * Importe maximo admitido en el formulario.
 *
 * La base admite numeric(18,2), pero number de JavaScript solo representa con
 * exactitud enteros hasta 2^53 (~9 * 10^15): por encima se perderian centimos al
 * enviarlo. Un billon cubre con holgura cualquier contrato de proveedor.
 */
export const MONTO_MAXIMO = 999_999_999_999.99;

const SIN_MILES = /^\d+(\.\d{1,2})?$/;
const CON_MILES = /^\d{1,3}(,\d{3})+(\.\d{1,2})?$/;

export type LecturaMonto =
  | { ok: true; valor: number }
  | { ok: false; motivo: 'vacio' | 'formato' | 'decimales' };

export function leerMonto(texto: string | null | undefined): LecturaMonto {
  const limpio = (texto ?? '').trim().replace(/^\$\s*/, '');

  if (limpio === '') {
    return { ok: false, motivo: 'vacio' };
  }

  if (!SIN_MILES.test(limpio) && !CON_MILES.test(limpio)) {
    // Distingue "demasiados decimales" de un formato ilegible para dar un
    // mensaje que sirva para corregirlo. Una coma despues del punto ("15.000,50",
    // formato europeo) es un problema de formato, no de decimales.
    const comaTrasPunto = /\.\d*,/.test(limpio);
    const sinComas = limpio.replace(/,/g, '');
    return !comaTrasPunto && /^\d+\.\d{3,}$/.test(sinComas)
      ? { ok: false, motivo: 'decimales' }
      : { ok: false, motivo: 'formato' };
  }

  return { ok: true, valor: Number(limpio.replace(/,/g, '')) };
}

/** Formatea un importe como "15,000.50" (sin simbolo: el campo ya muestra "$"). */
export function formatearMonto(valor: number): string {
  return valor.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}
