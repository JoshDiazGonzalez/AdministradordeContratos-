import { MONTO_MAXIMO, formatearMonto, leerMonto } from './monto';

describe('leerMonto', () => {
  it.each([
    ['15000', 15000],
    ['15000.5', 15000.5],
    ['15000.50', 15000.5],
    ['15,000.50', 15000.5],
    ['1,234,567.89', 1234567.89],
    ['$ 12,500.00', 12500],
    ['  0.01  ', 0.01],
  ])('"%s" se lee como %s', (texto, esperado) => {
    expect(leerMonto(texto)).toEqual({ ok: true, valor: esperado });
  });

  it.each([
    ['15000,50', 'coma como decimal: ambigua con el separador de miles'],
    ['15.000,50', 'formato europeo'],
    ['1,23', 'miles mal agrupados'],
    ['12,50.00', 'miles mal agrupados'],
    ['1e5', 'notacion cientifica'],
    ['-100', 'negativo'],
    ['+100', 'signo'],
    ['abc', 'texto'],
    ['12.3.4', 'dos puntos'],
  ])('rechaza "%s" (%s)', (texto) => {
    expect(leerMonto(texto)).toEqual({ ok: false, motivo: 'formato' });
  });

  it('detecta mas de dos decimales para explicarlo', () => {
    expect(leerMonto('100.555')).toEqual({ ok: false, motivo: 'decimales' });
    expect(leerMonto('1,000.555')).toEqual({ ok: false, motivo: 'decimales' });
  });

  it.each([null, undefined, '', '   ', '$'])('sin valor (%s) es vacio', (texto) => {
    expect(leerMonto(texto)).toEqual({ ok: false, motivo: 'vacio' });
  });

  it('el maximo admitido se representa sin perder centimos', () => {
    // Por encima de 2^53 number de JavaScript redondea: se perderian centimos.
    expect(Number.isSafeInteger(Math.round(MONTO_MAXIMO * 100))).toBe(true);
    expect(String(MONTO_MAXIMO)).toBe('999999999999.99');
  });
});

describe('formatearMonto', () => {
  it.each([
    [15000.5, '15,000.50'],
    [0.01, '0.01'],
    [1234567.891, '1,234,567.89'],
    [12500, '12,500.00'],
  ])('%s se muestra como %s', (valor, esperado) => {
    expect(formatearMonto(valor)).toBe(esperado);
  });

  it('lo formateado se vuelve a leer igual', () => {
    for (const valor of [0.01, 15000.5, 999999999999.99]) {
      const lectura = leerMonto(formatearMonto(valor));
      expect(lectura).toEqual({ ok: true, valor });
    }
  });
});
