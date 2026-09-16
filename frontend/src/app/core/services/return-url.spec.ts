import { RUTA_INICIO, returnUrlSegura } from './return-url';

describe('returnUrlSegura', () => {
  it.each([
    ['/contratos', '/contratos'],
    ['/contratos/abc-123', '/contratos/abc-123'],
    ['/contratos?estado=Vencido&page=2', '/contratos?estado=Vencido&page=2'],
    ['/dashboard', '/dashboard'],
  ])('acepta la ruta interna %s', (entrada, esperada) => {
    expect(returnUrlSegura(entrada)).toBe(esperada);
  });

  it.each([
    ['https://sitio-falso.com', 'URL absoluta externa'],
    ['http://sitio-falso.com/login', 'URL absoluta externa'],
    ['//sitio-falso.com', 'URL relativa al protocolo'],
    ['/\\sitio-falso.com', 'barra invertida que el navegador trata como /'],
    ['javascript:alert(1)', 'esquema javascript'],
    ['contratos', 'ruta sin barra inicial'],
    ['/login', 'el propio login, que causaria un bucle'],
    ['/login?returnUrl=/contratos', 'el propio login con parametros'],
  ])('rechaza %s (%s)', (entrada) => {
    // Evita redirecciones abiertas: tras iniciar sesion nunca se sale del sitio.
    expect(returnUrlSegura(entrada)).toBe(RUTA_INICIO);
  });

  it.each([null, undefined, '', '   '])('sin valor vuelve al inicio (%s)', (entrada) => {
    expect(returnUrlSegura(entrada)).toBe(RUTA_INICIO);
  });
});
