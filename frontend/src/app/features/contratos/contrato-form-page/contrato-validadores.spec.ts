import { FormControl, FormGroup } from '@angular/forms';

import {
  TAMANO_MAXIMO_BYTES,
  documentoValido,
  montoValido,
  rangoDeFechasValido,
  textoObligatorio,
} from './contrato-validadores';

function archivo(nombre: string, bytes: number): File {
  const archivo = new File(['x'], nombre);
  // Se fija el tamano sin crear un blob de 10 MB en memoria.
  Object.defineProperty(archivo, 'size', { value: bytes });
  return archivo;
}

describe('textoObligatorio', () => {
  it.each(['', '   ', null])('rechaza %o', (valor) => {
    expect(textoObligatorio(new FormControl(valor))).toEqual({ obligatorio: true });
  });

  it('acepta texto con contenido', () => {
    expect(textoObligatorio(new FormControl('Proveedor Alpha'))).toBeNull();
  });
});

describe('montoValido', () => {
  it.each([
    ['', { obligatorio: true }],
    ['abc', { formato: true }],
    ['100.555', { decimales: true }],
    ['0', { minimo: true }],
    ['0.00', { minimo: true }],
    ['1,000,000,000,000.00', { maximo: true }],
  ])('"%s" produce %o', (valor, esperado) => {
    expect(montoValido(new FormControl(valor))).toEqual(esperado);
  });

  it.each(['0.01', '15000.50', '15,000.50', '999,999,999,999.99'])('acepta "%s"', (valor) => {
    expect(montoValido(new FormControl(valor))).toBeNull();
  });
});

describe('rangoDeFechasValido', () => {
  const grupo = (inicio: string, vencimiento: string) =>
    new FormGroup({ fechaInicio: new FormControl(inicio), fechaVencimiento: new FormControl(vencimiento) });

  it('rechaza un vencimiento anterior al inicio', () => {
    expect(rangoDeFechasValido(grupo('2026-12-31', '2026-01-01'))).toEqual({ rangoFechas: true });
  });

  it('acepta el mismo dia y rangos correctos', () => {
    expect(rangoDeFechasValido(grupo('2026-01-01', '2026-01-01'))).toBeNull();
    expect(rangoDeFechasValido(grupo('2026-01-01', '2026-12-31'))).toBeNull();
  });

  it('no opina mientras falte una de las dos fechas', () => {
    // El error de "obligatorio" ya lo muestra cada campo.
    expect(rangoDeFechasValido(grupo('', '2026-01-01'))).toBeNull();
  });
});

describe('documentoValido', () => {
  it('sin archivo es obligatorio', () => {
    expect(documentoValido(new FormControl(null))).toEqual({ obligatorio: true });
  });

  it.each(['contrato.pdf', 'CONTRATO.PDF', 'anexo.doc', 'anexo.docx'])('acepta %s', (nombre) => {
    expect(documentoValido(new FormControl(archivo(nombre, 2048)))).toBeNull();
  });

  it.each(['foto.png', 'hoja.xlsx', 'script.exe', 'contrato.pdf.exe', 'sin-extension'])(
    'rechaza la extension de %s',
    (nombre) => {
      expect(documentoValido(new FormControl(archivo(nombre, 2048)))).toEqual({ extension: true });
    },
  );

  it('rechaza un archivo vacio', () => {
    expect(documentoValido(new FormControl(archivo('contrato.pdf', 0)))).toEqual({ vacio: true });
  });

  it('acepta exactamente 10 MB y rechaza un byte mas', () => {
    expect(documentoValido(new FormControl(archivo('a.pdf', TAMANO_MAXIMO_BYTES)))).toBeNull();
    expect(documentoValido(new FormControl(archivo('a.pdf', TAMANO_MAXIMO_BYTES + 1)))).toEqual({
      tamano: true,
    });
  });
});
