import { FechaCortaPipe } from './fecha-corta-pipe';

describe('FechaCortaPipe', () => {
  const pipe = new FechaCortaPipe();

  it.each([
    ['2026-06-15', '15/06/2026'],
    ['2027-12-31', '31/12/2027'],
    // Casos que new Date() desplaza al dia (y ano) anterior en zonas UTC-5.
    ['2026-01-01', '01/01/2026'],
    ['2026-03-01', '01/03/2026'],
  ])('%s se muestra como %s', (entrada, esperado) => {
    expect(pipe.transform(entrada)).toBe(esperado);
  });

  it('no depende de la zona horaria del navegador', () => {
    // Referencia del error que evita: en Ecuador new Date('2026-01-01')
    // cae en 2025. El pipe nunca construye un Date.
    const construirFecha = vi.spyOn(globalThis, 'Date');

    pipe.transform('2026-01-01');

    expect(construirFecha).not.toHaveBeenCalled();
    construirFecha.mockRestore();
  });

  it.each([null, undefined, '', 'no-es-fecha', '2026-1-5', '2026-01-01T10:00:00Z'])(
    'un valor no valido (%s) se muestra como raya',
    (entrada) => {
      expect(pipe.transform(entrada)).toBe('—');
    },
  );
});
