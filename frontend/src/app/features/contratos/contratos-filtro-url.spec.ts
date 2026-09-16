import { convertToParamMap } from '@angular/router';

import {
  contarCriterios,
  criteriosAParametros,
  errorDeRangos,
  esFechaIso,
  leerFiltroDeUrl,
  mismosCriterios,
} from './contratos-filtro-url';

describe('esFechaIso', () => {
  it.each(['2026-01-01', '2026-12-31', '2028-02-29'])('acepta la fecha real %s', (fecha) => {
    expect(esFechaIso(fecha)).toBe(true);
  });

  it.each([
    ['2026-02-30', 'dia que no existe'],
    ['2026-02-29', '29 de febrero en ano no bisiesto'],
    ['2026-13-01', 'mes 13'],
    ['2026-1-5', 'sin ceros a la izquierda'],
    ['01/01/2026', 'formato local'],
    ['2026-01-01T00:00:00Z', 'con hora'],
    ['', 'vacio'],
  ])('rechaza %s (%s)', (fecha) => {
    expect(esFechaIso(fecha)).toBe(false);
  });
});

describe('leerFiltroDeUrl', () => {
  it('lee todos los criterios validos', () => {
    const filtro = leerFiltroDeUrl(
      convertToParamMap({
        proveedor: '  beta  ',
        estado: 'PorVencer',
        fechaInicioDesde: '2026-01-01',
        fechaInicioHasta: '2026-06-30',
        fechaVencimientoDesde: '2026-07-01',
        fechaVencimientoHasta: '2026-12-31',
        page: '2',
        pageSize: '20',
      }),
    );

    expect(filtro).toEqual({
      proveedor: 'beta',
      estado: 'PorVencer',
      fechaInicioDesde: '2026-01-01',
      fechaInicioHasta: '2026-06-30',
      fechaVencimientoDesde: '2026-07-01',
      fechaVencimientoHasta: '2026-12-31',
      page: 2,
      pageSize: 20,
    });
  });

  it('descarta lo que la API rechazaria en lugar de provocar un error', () => {
    // Un enlace mal copiado no debe dejar la pantalla en error.
    const filtro = leerFiltroDeUrl(
      convertToParamMap({
        estado: 'Inexistente',
        fechaInicioDesde: '2026-02-30',
        fechaVencimientoHasta: 'no-es-fecha',
        proveedor: '   ',
      }),
    );

    expect(filtro).toEqual({ page: 1, pageSize: 10 });
  });

  it('recorta un proveedor excesivamente largo al maximo que admite la API', () => {
    const filtro = leerFiltroDeUrl(convertToParamMap({ proveedor: 'x'.repeat(500) }));

    expect(filtro.proveedor).toHaveLength(200);
  });
});

describe('errorDeRangos', () => {
  it('sin fechas o con rangos correctos no hay error', () => {
    expect(errorDeRangos({})).toBeNull();
    expect(errorDeRangos({ fechaInicioDesde: '2026-01-01', fechaInicioHasta: '2026-01-01' })).toBeNull();
    expect(errorDeRangos({ fechaVencimientoDesde: '2026-12-31' })).toBeNull();
  });

  it('detecta un rango de inicio invertido', () => {
    expect(errorDeRangos({ fechaInicioDesde: '2026-12-31', fechaInicioHasta: '2026-01-01' }))
      .toContain('fecha de inicio');
  });

  it('detecta un rango de vencimiento invertido', () => {
    expect(errorDeRangos({ fechaVencimientoDesde: '2026-12-31', fechaVencimientoHasta: '2026-01-01' }))
      .toContain('fecha de vencimiento');
  });
});

describe('mismosCriterios', () => {
  it('no depende del orden de las claves', () => {
    expect(
      mismosCriterios({ proveedor: 'a', estado: 'Activo' }, { estado: 'Activo', proveedor: 'a' }),
    ).toBe(true);
  });

  it('trata un campo vacio igual que un campo ausente', () => {
    expect(mismosCriterios({ proveedor: '' }, {})).toBe(true);
  });

  it('detecta cualquier diferencia', () => {
    expect(mismosCriterios({ proveedor: 'a' }, { proveedor: 'b' })).toBe(false);
    expect(mismosCriterios({ estado: 'Activo' }, {})).toBe(false);
  });
});

describe('contarCriterios y criteriosAParametros', () => {
  it('cuenta solo los criterios con valor', () => {
    expect(contarCriterios({ proveedor: 'beta', estado: 'Vencido', fechaInicioDesde: '' })).toBe(2);
  });

  it('los criterios vacios se envian como null para quitarlos de la URL', () => {
    expect(criteriosAParametros({ proveedor: '  ', estado: 'Activo' })).toEqual({
      proveedor: null,
      estado: 'Activo',
      fechaInicioDesde: null,
      fechaInicioHasta: null,
      fechaVencimientoDesde: null,
      fechaVencimientoHasta: null,
    });
  });
});
