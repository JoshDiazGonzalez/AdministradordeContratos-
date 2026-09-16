import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

import { MONTO_MAXIMO, leerMonto } from '../../../shared/utils/monto';

/** Reglas del documento. Coinciden con la configuracion de la API (Storage). */
export const EXTENSIONES_PERMITIDAS = ['.pdf', '.doc', '.docx'] as const;
export const TAMANO_MAXIMO_BYTES = 10 * 1024 * 1024;

/**
 * Obligatorio y con contenido real: "   " no es un nombre de proveedor.
 * Validators.required no lo detecta porque la cadena no esta vacia.
 */
export const textoObligatorio: ValidatorFn = (control) => {
  const valor = control.value as string | null;
  return valor && valor.trim().length > 0 ? null : { obligatorio: true };
};

export const montoValido: ValidatorFn = (control) => {
  const lectura = leerMonto(control.value as string | null);

  if (!lectura.ok) {
    return { [lectura.motivo === 'vacio' ? 'obligatorio' : lectura.motivo]: true };
  }

  if (lectura.valor <= 0) {
    return { minimo: true };
  }

  if (lectura.valor > MONTO_MAXIMO) {
    return { maximo: true };
  }

  return null;
};

/**
 * Validador del grupo: el vencimiento no puede ser anterior al inicio.
 *
 * Esta validacion existe tambien en la API, que es la que manda. Aqui solo sirve
 * para avisar antes de enviar; no sustituye a la del servidor.
 */
export const rangoDeFechasValido: ValidatorFn = (grupo: AbstractControl): ValidationErrors | null => {
  const inicio = grupo.get('fechaInicio')?.value as string | null;
  const vencimiento = grupo.get('fechaVencimiento')?.value as string | null;

  // Las cadenas yyyy-MM-dd se comparan igual que las fechas que representan.
  return inicio && vencimiento && vencimiento < inicio ? { rangoFechas: true } : null;
};

/**
 * Comprueba el documento antes de subirlo, para no esperar a que termine una
 * subida de varios MB y descubrir que se rechaza. La API vuelve a validarlo,
 * incluida la firma binaria del archivo, que desde el navegador no se revisa.
 */
export const documentoValido: ValidatorFn = (control) => {
  const archivo = control.value as File | null;

  if (!archivo) {
    return { obligatorio: true };
  }

  const nombre = archivo.name.toLowerCase();
  if (!EXTENSIONES_PERMITIDAS.some((extension) => nombre.endsWith(extension))) {
    return { extension: true };
  }

  if (archivo.size === 0) {
    return { vacio: true };
  }

  if (archivo.size > TAMANO_MAXIMO_BYTES) {
    return { tamano: true };
  }

  return null;
};
