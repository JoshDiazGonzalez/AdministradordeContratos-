import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, convertToParamMap, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { Contrato } from '../../../core/models/contrato.model';
import { ResultadoPaginado } from '../../../core/models/paginacion.model';
import { API_URL } from '../../../core/services/api-url.token';
import { ContratosListPage, leerPaginacion } from './contratos-list-page';

const API = 'https://api.test/api';

function contrato(n: number, cambios: Partial<Contrato> = {}): Contrato {
  return {
    id: `id-${n}`,
    nombreProveedor: `Proveedor ${n}`,
    montoContrato: 12500,
    fechaInicio: '2026-01-01',
    fechaVencimiento: '2026-12-31',
    estado: 'Activo',
    descripcion: `Descripcion ${n}`,
    archivoNombre: `contrato-${n}.pdf`,
    archivoContentType: 'application/pdf',
    archivoTamanoBytes: 2048,
    fechaCreacion: '2026-01-01T10:00:00Z',
    fechaActualizacion: null,
    ...cambios,
  };
}

function pagina(items: Contrato[], extra: Partial<ResultadoPaginado<Contrato>> = {}): ResultadoPaginado<Contrato> {
  return { items, page: 1, pageSize: 10, totalItems: items.length, totalPages: items.length ? 1 : 0, ...extra };
}

describe('leerPaginacion', () => {
  it.each([
    [{}, { page: 1, pageSize: 10 }],
    [{ page: '3', pageSize: '20' }, { page: 3, pageSize: 20 }],
    [{ page: 'abc' }, { page: 1, pageSize: 10 }],
    [{ page: '-2' }, { page: 1, pageSize: 10 }],
    [{ page: '1.5' }, { page: 1, pageSize: 10 }],
    [{ pageSize: '9999' }, { page: 1, pageSize: 10 }],
    [{ pageSize: '7' }, { page: 1, pageSize: 10 }],
  ])('%o se interpreta como %o', (parametros, esperado) => {
    // Valores escritos a mano en la URL nunca deben llegar tal cual a la API.
    expect(leerPaginacion(convertToParamMap(parametros))).toEqual(esperado);
  });
});

describe('ContratosListPage', () => {
  let http: HttpTestingController;
  let harness: RouterTestingHarness;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([
          { path: 'contratos', component: ContratosListPage },
          { path: 'contratos/nuevo', children: [] },
          { path: 'contratos/:id', children: [] },
        ]),
        { provide: API_URL, useValue: API },
      ],
    });

    http = TestBed.inject(HttpTestingController);
    harness = await RouterTestingHarness.create();
  });

  afterEach(() => http.verify());

  async function abrir(url = '/contratos'): Promise<TestRequest> {
    await harness.navigateByUrl(url);
    harness.detectChanges();
    return http.expectOne((r) => r.url === `${API}/contratos`);
  }

  async function responder(peticion: TestRequest, datos: ResultadoPaginado<Contrato>): Promise<HTMLElement> {
    peticion.flush(datos);
    await harness.fixture.whenStable();
    harness.detectChanges();
    return harness.routeNativeElement as HTMLElement;
  }

  it('mientras carga por primera vez muestra el esqueleto', async () => {
    const peticion = await abrir();
    const raiz = harness.routeNativeElement as HTMLElement;

    expect(raiz.querySelector('.esqueleto')).not.toBeNull();
    expect(raiz.querySelector('[role="status"]')?.textContent).toContain('Cargando contratos');

    peticion.flush(pagina([]));
  });

  it('pide la pagina indicada en la URL', async () => {
    const peticion = await abrir('/contratos?page=2&pageSize=20');

    expect(peticion.request.params.get('page')).toBe('2');
    expect(peticion.request.params.get('pageSize')).toBe('20');
    peticion.flush(pagina([], { page: 2, pageSize: 20, totalItems: 40, totalPages: 2 }));
  });

  it('muestra cada contrato con monto en dolares y fechas sin desfase', async () => {
    const raiz = await responder(
      await abrir(),
      pagina([contrato(1, { montoContrato: 12500, fechaInicio: '2026-01-01', estado: 'PorVencer' })]),
    );

    const celdas = [...raiz.querySelectorAll('tbody tr:first-child td')].map((td) =>
      td.textContent!.replace(/\s+/g, ' ').trim(),
    );

    expect(celdas[0]).toBe('Proveedor 1');
    expect(celdas[1]).toBe('$12,500.00');
    expect(celdas[2]).toBe('01/01/2026');
    expect(celdas[4]).toBe('Por vencer');
    expect(celdas[6]).toBe('contrato-1.pdf');
  });

  it('muestra las columnas que pide el requerimiento', async () => {
    const raiz = await responder(await abrir(), pagina([contrato(1)]));

    const encabezados = [...raiz.querySelectorAll('thead th')].map((th) => th.textContent!.trim());

    expect(encabezados).toEqual([
      'Proveedor',
      'Monto',
      'Fecha inicio',
      'Fecha vencimiento',
      'Estado',
      'Descripción',
      'Documento',
      'Acciones',
    ]);
  });

  it('el enlace de detalle identifica el contrato para lectores de pantalla', async () => {
    const raiz = await responder(await abrir(), pagina([contrato(7)]));

    const enlace = raiz.querySelector<HTMLAnchorElement>('tbody a')!;
    expect(enlace.textContent!.replace(/\s+/g, ' ').trim()).toBe('Ver detalle de Proveedor 7');
    expect(enlace.getAttribute('href')).toBe('/contratos/id-7');
  });

  it('sin contratos muestra un estado vacio con la accion de registrar', async () => {
    const raiz = await responder(await abrir(), pagina([]));

    expect(raiz.querySelector('.vacio h2')?.textContent).toContain('Aún no hay contratos');
    expect(raiz.querySelector('.vacio a')?.getAttribute('href')).toBe('/contratos/nuevo');
    expect(raiz.querySelector('table')).toBeNull();
  });

  it('ante un error muestra el mensaje y permite reintentar', async () => {
    const primera = await abrir();
    primera.flush({}, { status: 500, statusText: 'Server Error' });
    await harness.fixture.whenStable();
    harness.detectChanges();
    const raiz = harness.routeNativeElement as HTMLElement;

    expect(raiz.querySelector('[role="alert"]')?.textContent).toContain('No se pudieron cargar');

    raiz.querySelector<HTMLButtonElement>('[role="alert"] button')!.click();
    harness.detectChanges();

    const reintento = http.expectOne((r) => r.url === `${API}/contratos`);
    await responder(reintento, pagina([contrato(1)]));

    expect(raiz.querySelector('[role="alert"]')).toBeNull();
    expect(raiz.querySelectorAll('tbody tr').length).toBe(1);
  });

  it('sin conexion explica que no hay acceso al servidor', async () => {
    const peticion = await abrir();
    peticion.error(new ProgressEvent('error'), { status: 0 });
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect((harness.routeNativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent)
      .toContain('No se pudo conectar con el servidor');
  });

  it('un 401 no muestra error: el interceptor ya lleva al login', async () => {
    const peticion = await abrir();
    peticion.flush({}, { status: 401, statusText: 'Unauthorized' });
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect((harness.routeNativeElement as HTMLElement).querySelector('[role="alert"]')).toBeNull();
  });

  it('una pagina fuera de rango se corrige a la ultima disponible', async () => {
    const peticion = await abrir('/contratos?page=9');
    peticion.flush(pagina([], { page: 9, totalItems: 25, totalPages: 3 }));
    await harness.fixture.whenStable();

    // Tras corregir la URL se pide la pagina valida.
    const corregida = http.expectOne((r) => r.url === `${API}/contratos`);
    expect(corregida.request.params.get('page')).toBe('3');
    expect(TestBed.inject(Router).url).toBe('/contratos?page=3');
    corregida.flush(pagina([contrato(21)], { page: 3, totalItems: 25, totalPages: 3 }));
  });

  it('al cambiar de pagina rapido solo cuenta la ultima respuesta', async () => {
    const router = TestBed.inject(Router);
    await responder(await abrir(), pagina([contrato(1)], { totalItems: 30, totalPages: 3 }));

    await router.navigateByUrl('/contratos?page=2');
    const paginaDos = http.expectOne((r) => r.params.get('page') === '2');

    await router.navigateByUrl('/contratos?page=3');
    const paginaTres = http.expectOne((r) => r.params.get('page') === '3');

    // La peticion abandonada se cancela y su respuesta tardia no se aplica.
    expect(paginaDos.cancelled).toBe(true);

    const raiz = await responder(paginaTres, pagina([contrato(3)], { page: 3, totalItems: 30, totalPages: 3 }));
    expect(raiz.querySelector('tbody td')?.textContent?.trim()).toBe('Proveedor 3');
  });

  it('cambiar el tamano de pagina vuelve a la primera pagina', async () => {
    const raiz = await responder(
      await abrir('/contratos?page=3'),
      pagina([contrato(21)], { page: 3, totalItems: 30, totalPages: 3 }),
    );

    const selector = raiz.querySelector<HTMLSelectElement>('select')!;
    selector.value = '20';
    selector.dispatchEvent(new Event('change'));
    await harness.fixture.whenStable();

    expect(TestBed.inject(Router).url).toBe('/contratos?page=1&pageSize=20');
    http.expectOne((r) => r.url === `${API}/contratos`).flush(pagina([], { totalItems: 30, totalPages: 2 }));
  });
});
