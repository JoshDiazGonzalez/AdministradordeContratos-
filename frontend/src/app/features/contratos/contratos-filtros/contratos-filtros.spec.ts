import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CriteriosBusqueda } from '../contratos-filtro-url';
import { ContratosFiltros, PAUSA_BUSQUEDA_MS } from './contratos-filtros';

describe('ContratosFiltros', () => {
  let fixture: ComponentFixture<ContratosFiltros>;
  let raiz: HTMLElement;
  let emitidos: CriteriosBusqueda[];

  async function crear(criterios: CriteriosBusqueda = {}): Promise<void> {
    vi.useFakeTimers();
    fixture = TestBed.createComponent(ContratosFiltros);
    fixture.componentRef.setInput('criterios', criterios);
    emitidos = [];
    fixture.componentInstance.cambio.subscribe((c) => emitidos.push(c));
    fixture.detectChanges();
    raiz = fixture.nativeElement as HTMLElement;
  }

  /** Simula que la pantalla aplico los criterios a la URL y los devolvio. */
  function aplicarEnUrl(criterios: CriteriosBusqueda): void {
    fixture.componentRef.setInput('criterios', criterios);
    fixture.detectChanges();
  }

  function escribir(selector: string, valor: string): void {
    const campo = raiz.querySelector<HTMLInputElement | HTMLSelectElement>(selector)!;
    campo.value = valor;
    campo.dispatchEvent(new Event(campo instanceof HTMLSelectElement ? 'change' : 'input'));
    fixture.detectChanges();
  }

  const campo = (selector: string) => raiz.querySelector<HTMLInputElement>(selector)!;

  afterEach(() => vi.useRealTimers());

  it('la busqueda por proveedor espera a que se deje de escribir', async () => {
    await crear();

    escribir('#filtro-proveedor', 'b');
    escribir('#filtro-proveedor', 'be');
    escribir('#filtro-proveedor', 'beta');

    vi.advanceTimersByTime(PAUSA_BUSQUEDA_MS - 1);
    expect(emitidos).toEqual([]);

    vi.advanceTimersByTime(1);
    // Una sola busqueda con el texto final, no una por tecla.
    expect(emitidos).toEqual([{ proveedor: 'beta' }]);
  });

  it('el estado se aplica al instante', async () => {
    await crear();

    escribir('#filtro-estado', 'Vencido');

    expect(emitidos).toEqual([{ estado: 'Vencido' }]);
  });

  it('combina todos los criterios', async () => {
    await crear();

    escribir('#filtro-estado', 'Activo');
    aplicarEnUrl({ estado: 'Activo' });
    escribir('#filtro-vence-desde', '2026-01-01');

    expect(emitidos.at(-1)).toEqual({ estado: 'Activo', fechaVencimientoDesde: '2026-01-01' });
  });

  it('un rango invertido muestra el error y no se envia', async () => {
    await crear();

    escribir('#filtro-inicio-desde', '2026-12-31');
    aplicarEnUrl({ fechaInicioDesde: '2026-12-31' });
    emitidos = [];

    escribir('#filtro-inicio-hasta', '2026-01-01');

    expect(emitidos).toEqual([]);
    expect(raiz.querySelector('[role="alert"]')?.textContent).toContain('fecha de inicio');
  });

  it('un rango invertido que llega por la URL tambien muestra el error', async () => {
    // Regresion: la sincronizacion desde la URL es silenciosa y el mensaje no
    // se calculaba.
    await crear({ fechaVencimientoDesde: '2026-12-31', fechaVencimientoHasta: '2026-01-01' });

    expect(raiz.querySelector('[role="alert"]')?.textContent).toContain('fecha de vencimiento');
  });

  it('refleja en el formulario los criterios de la URL', async () => {
    await crear({ proveedor: 'gamma', estado: 'PorVencer', fechaInicioDesde: '2026-01-01' });

    expect(campo('#filtro-proveedor').value).toBe('gamma');
    expect(raiz.querySelector<HTMLSelectElement>('#filtro-estado')!.value).toBe('PorVencer');
    expect(campo('#filtro-inicio-desde').value).toBe('2026-01-01');
    // Reflejar la URL no debe disparar una busqueda.
    expect(emitidos).toEqual([]);
  });

  it('al volver atras y repetir la misma busqueda, se aplica de nuevo', async () => {
    // Regresion: se comparaba con lo ultimo emitido en vez de con la URL, y la
    // busqueda repetida se descartaba aunque la tabla mostrara todo.
    await crear();

    escribir('#filtro-proveedor', 'beta');
    vi.advanceTimersByTime(PAUSA_BUSQUEDA_MS);
    aplicarEnUrl({ proveedor: 'beta' });

    aplicarEnUrl({}); // boton atras
    expect(campo('#filtro-proveedor').value).toBe('');

    escribir('#filtro-proveedor', 'beta');
    vi.advanceTimersByTime(PAUSA_BUSQUEDA_MS);

    expect(emitidos).toEqual([{ proveedor: 'beta' }, { proveedor: 'beta' }]);
  });

  it('no borra el espacio final mientras se escribe', async () => {
    await crear();

    escribir('#filtro-proveedor', 'servicios ');
    vi.advanceTimersByTime(PAUSA_BUSQUEDA_MS);
    aplicarEnUrl({ proveedor: 'servicios' });

    // Si el campo se reescribiera con el valor de la URL, se perderia el
    // espacio y la siguiente palabra quedaria pegada.
    expect(campo('#filtro-proveedor').value).toBe('servicios ');
  });

  it('limpiar vacia todo, emite sin criterios y quita el error', async () => {
    await crear({ proveedor: 'beta', estado: 'Vencido' });
    escribir('#filtro-inicio-desde', '2026-12-31');
    escribir('#filtro-inicio-hasta', '2026-01-01');
    expect(raiz.querySelector('[role="alert"]')).not.toBeNull();

    [...raiz.querySelectorAll<HTMLButtonElement>('button')]
      .find((b) => b.textContent?.includes('Limpiar'))!
      .click();
    fixture.detectChanges();

    expect(emitidos.at(-1)).toEqual({});
    expect(campo('#filtro-proveedor').value).toBe('');
    expect(raiz.querySelector('[role="alert"]')).toBeNull();
  });

  it('una busqueda pendiente no se aplica despues de limpiar', async () => {
    await crear();

    escribir('#filtro-proveedor', 'beta');
    raiz.querySelectorAll<HTMLButtonElement>('button')[1].click();
    fixture.detectChanges();
    aplicarEnUrl({});

    vi.advanceTimersByTime(PAUSA_BUSQUEDA_MS);

    expect(emitidos).toEqual([{}]);
  });

  it('muestra cuantos filtros hay aplicados', async () => {
    await crear({ proveedor: 'beta', estado: 'Activo' });

    expect(raiz.querySelector('.contador')?.textContent).toContain('2');
  });

  it('limpiar esta deshabilitado si no hay nada que limpiar', async () => {
    await crear();

    const limpiar = [...raiz.querySelectorAll<HTMLButtonElement>('button')].find((b) =>
      b.textContent?.includes('Limpiar'),
    )!;
    expect(limpiar.disabled).toBe(true);
  });

  it('los rangos de fecha limitan el calendario del otro extremo', async () => {
    await crear({ fechaInicioDesde: '2026-03-01', fechaInicioHasta: '2026-09-30' });

    expect(campo('#filtro-inicio-hasta').min).toBe('2026-03-01');
    expect(campo('#filtro-inicio-desde').max).toBe('2026-09-30');
  });
});
