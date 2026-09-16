import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { Contrato, CrearContratoRequest } from '../models/contrato.model';
import { ResultadoPaginado } from '../models/paginacion.model';
import { API_URL } from './api-url.token';
import { ContratosService } from './contratos.service';

const API = 'https://api.test/api';

describe('ContratosService', () => {
  let servicio: ContratosService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_URL, useValue: API },
      ],
    });

    servicio = TestBed.inject(ContratosService);
    http = TestBed.inject(HttpTestingController);
  });

  // Falla el test si quedo alguna peticion sin atender o no esperada.
  afterEach(() => http.verify());

  describe('listar', () => {
    it('sin filtros no envia parametros', () => {
      servicio.listar().subscribe();

      const peticion = http.expectOne(`${API}/contratos`);
      expect(peticion.request.method).toBe('GET');
      expect(peticion.request.params.keys()).toEqual([]);
      peticion.flush(paginaVacia());
    });

    it('envia solo los filtros con valor', () => {
      servicio
        .listar({
          proveedor: 'software',
          estado: 'PorVencer',
          fechaVencimientoHasta: '2026-12-31',
          fechaInicioDesde: '',
          page: 2,
          pageSize: 10,
        })
        .subscribe();

      const peticion = http.expectOne((r) => r.url === `${API}/contratos`);
      const params = peticion.request.params;

      expect(params.get('proveedor')).toBe('software');
      expect(params.get('estado')).toBe('PorVencer');
      expect(params.get('fechaVencimientoHasta')).toBe('2026-12-31');
      expect(params.get('page')).toBe('2');
      expect(params.get('pageSize')).toBe('10');
      // Un filtro vacio no debe viajar como "fechaInicioDesde=".
      expect(params.has('fechaInicioDesde')).toBe(false);
      peticion.flush(paginaVacia());
    });

    it('ignora filtros formados solo por espacios', () => {
      servicio.listar({ proveedor: '   ' }).subscribe();

      const peticion = http.expectOne((r) => r.url === `${API}/contratos`);
      expect(peticion.request.params.has('proveedor')).toBe(false);
      peticion.flush(paginaVacia());
    });

    it('devuelve la pagina tal como llega de la API', () => {
      let recibida: ResultadoPaginado<Contrato> | undefined;
      servicio.listar().subscribe((pagina) => (recibida = pagina));

      const esperada = { ...paginaVacia(), items: [contratoDePrueba()], totalItems: 1 };
      http.expectOne(`${API}/contratos`).flush(esperada);

      expect(recibida).toEqual(esperada);
    });
  });

  it('obtener codifica el identificador en la URL', () => {
    servicio.obtener('a/b').subscribe();

    // Un id malformado no debe poder alterar la ruta del endpoint.
    http.expectOne(`${API}/contratos/a%2Fb`).flush(contratoDePrueba());
  });

  it('resumen consulta el endpoint de conteos', () => {
    servicio.resumen().subscribe();

    http
      .expectOne(`${API}/contratos/resumen`)
      .flush({ total: 0, activos: 0, porVencer: 0, vencidos: 0, inactivos: 0 });
  });

  describe('crear', () => {
    it('envia multipart con los campos que espera la API', () => {
      servicio.crear(solicitud({ nombreProveedor: 'Proveedor Alpha' })).subscribe();

      const peticion = http.expectOne(`${API}/contratos`);
      expect(peticion.request.method).toBe('POST');

      const cuerpo = peticion.request.body as FormData;
      expect(cuerpo).toBeInstanceOf(FormData);
      expect(cuerpo.get('NombreProveedor')).toBe('Proveedor Alpha');
      expect(cuerpo.get('FechaInicio')).toBe('2026-01-01');
      expect(cuerpo.get('FechaVencimiento')).toBe('2026-12-31');
      expect(cuerpo.get('Descripcion')).toBe('Servicios de software');
      expect((cuerpo.get('Archivo') as File).name).toBe('contrato.pdf');

      peticion.flush(contratoDePrueba());
    });

    it('envia el monto con punto decimal, sin depender del idioma', () => {
      // La API interpreta los numeros con cultura invariante. Un "15000,50"
      // generado segun el idioma del navegador se malinterpretaria.
      servicio.crear(solicitud({ montoContrato: 15000.5 })).subscribe();

      const peticion = http.expectOne(`${API}/contratos`);
      expect((peticion.request.body as FormData).get('MontoContrato')).toBe('15000.5');
      peticion.flush(contratoDePrueba());
    });

    it('no fija Content-Type a mano, para que el navegador anada el boundary', () => {
      servicio.crear(solicitud()).subscribe();

      const peticion = http.expectOne(`${API}/contratos`);
      expect(peticion.request.headers.has('Content-Type')).toBe(false);
      peticion.flush(contratoDePrueba());
    });
  });

  it('cambiarEstado envia solo la bandera de inactividad', () => {
    servicio.cambiarEstado('123', true).subscribe();

    const peticion = http.expectOne(`${API}/contratos/123/estado`);
    expect(peticion.request.method).toBe('PATCH');
    expect(peticion.request.body).toEqual({ inactivo: true });
    peticion.flush(contratoDePrueba());
  });

  describe('descargarArchivo', () => {
    it('pide el documento como blob para verlo en linea por defecto', () => {
      servicio.descargarArchivo('123').subscribe();

      const peticion = http.expectOne((r) => r.url === `${API}/contratos/123/archivo`);
      expect(peticion.request.responseType).toBe('blob');
      expect(peticion.request.params.get('download')).toBe('false');
      peticion.flush(new Blob(['%PDF']));
    });

    it('puede forzar la descarga', () => {
      servicio.descargarArchivo('123', true).subscribe();

      const peticion = http.expectOne((r) => r.url === `${API}/contratos/123/archivo`);
      expect(peticion.request.params.get('download')).toBe('true');
      peticion.flush(new Blob(['%PDF']));
    });
  });
});

function solicitud(cambios: Partial<CrearContratoRequest> = {}): CrearContratoRequest {
  return {
    nombreProveedor: 'Proveedor Alpha',
    montoContrato: 12500,
    fechaInicio: '2026-01-01',
    fechaVencimiento: '2026-12-31',
    descripcion: 'Servicios de software',
    archivo: new File(['%PDF-1.7'], 'contrato.pdf', { type: 'application/pdf' }),
    ...cambios,
  };
}

function paginaVacia(): ResultadoPaginado<Contrato> {
  return { items: [], page: 1, pageSize: 10, totalItems: 0, totalPages: 0 };
}

function contratoDePrueba(): Contrato {
  return {
    id: '01a0a852-e787-7335-a756-ff2e4494c85f',
    nombreProveedor: 'Proveedor Alpha',
    montoContrato: 15000.5,
    fechaInicio: '2026-01-01',
    fechaVencimiento: '2026-12-31',
    estado: 'Activo',
    descripcion: 'Servicios de software',
    archivoNombre: 'contrato.pdf',
    archivoContentType: 'application/pdf',
    archivoTamanoBytes: 2048,
    fechaCreacion: '2026-01-01T10:00:00Z',
    fechaActualizacion: null,
  };
}
