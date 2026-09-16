import { FechaHoraPipe } from './fecha-hora-pipe';

describe('FechaHoraPipe', () => {
  const pipe = new FechaHoraPipe();

  it('muestra el instante en la hora de Ecuador (UTC-5)', () => {
    // 16:13 UTC son las 11:13 en Guayaquil.
    expect(pipe.transform('2026-09-16T16:13:19Z')).toBe('16/09/2026, 11:13');
  });

  it('respeta el desfase que traiga la cadena', () => {
    expect(pipe.transform('2026-09-16T16:13:19+00:00')).toBe('16/09/2026, 11:13');
  });

  it('cambia de dia cuando corresponde en la zona del negocio', () => {
    // 02:00 UTC del 17 son las 21:00 del 16 en Ecuador.
    expect(pipe.transform('2026-09-17T02:00:00Z')).toBe('16/09/2026, 21:00');
  });

  it.each([null, undefined, '', 'no-es-fecha'])('un valor no valido (%s) se muestra como raya', (valor) => {
    expect(pipe.transform(valor)).toBe('—');
  });
});
