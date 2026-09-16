import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject } from 'rxjs';

import { Contrato } from '../../../core/models/contrato.model';
import { ContratosService } from '../../../core/services/contratos.service';
import { NotificacionesService } from '../../../core/services/notificaciones.service';
import { DialogoConfirmacionService } from '../../../shared/ui/dialogo-confirmacion/dialogo-confirmacion.service';
import { ContratoDetailPage } from './contrato-detail-page';

function contrato(cambios: Partial<Contrato> = {}): Contrato {
  return {
    id: 'id-1',
    nombreProveedor: 'Proveedor Alpha',
    montoContrato: 12500,
    fechaInicio: '2026-01-01',
    fechaVencimiento: '2026-12-31',
    estado: 'Activo',
    descripcion: 'Linea uno\nLinea dos',
    archivoNombre: 'contrato-alpha.pdf',
    archivoContentType: 'application/pdf',
    archivoTamanoBytes: 850 * 1024,
    fechaCreacion: '2026-09-16T16:13:19Z',
    fechaActualizacion: null,
    ...cambios,
  };
}

describe('ContratoDetailPage', () => {
  let fixture: ComponentFixture<ContratoDetailPage>;
  let raiz: HTMLElement;

  let obtener$: Subject<Contrato>;
  let archivo$: Subject<Blob>;
  let estado$: Subject<Contrato>;
  let servicio: {
    obtener: ReturnType<typeof vi.fn>;
    descargarArchivo: ReturnType<typeof vi.fn>;
    cambiarEstado: ReturnType<typeof vi.fn>;
  };
  let confirmar: ReturnType<typeof vi.fn>;
  let urlsCreadas: string[];
  let urlsLiberadas: string[];

  async function crear(opciones: { visorPdf?: boolean } = {}): Promise<void> {
    Object.defineProperty(navigator, 'pdfViewerEnabled', {
      value: opciones.visorPdf ?? true,
      configurable: true,
    });

    fixture = TestBed.createComponent(ContratoDetailPage);
    fixture.componentRef.setInput('id', 'id-1');
    await fixture.whenStable();
    raiz = fixture.nativeElement as HTMLElement;
  }

  async function responder(datos: Contrato = contrato()): Promise<void> {
    obtener$.next(datos);
    obtener$.complete();
    await fixture.whenStable();
  }

  const boton = (texto: string) =>
    [...raiz.querySelectorAll<HTMLButtonElement>('button')].find((b) => b.textContent?.includes(texto));

  const texto = () => raiz.textContent!.replace(/\s+/g, ' ');

  beforeEach(() => {
    obtener$ = new Subject();
    archivo$ = new Subject();
    estado$ = new Subject();
    servicio = {
      obtener: vi.fn(() => obtener$.asObservable()),
      descargarArchivo: vi.fn(() => archivo$.asObservable()),
      cambiarEstado: vi.fn(() => estado$.asObservable()),
    };
    confirmar = vi.fn();

    urlsCreadas = [];
    urlsLiberadas = [];
    let contador = 0;
    // jsdom no implementa estas APIs. Se registran para comprobar que cada URL
    // de objeto creada tambien se libera.
    URL.createObjectURL = vi.fn(() => {
      const url = `blob:http://localhost/${++contador}`;
      urlsCreadas.push(url);
      return url;
    });
    URL.revokeObjectURL = vi.fn((url: string) => void urlsLiberadas.push(url));
    Element.prototype.scrollIntoView = vi.fn();

    TestBed.configureTestingModule({
      imports: [ContratoDetailPage],
      providers: [
        provideRouter([]),
        { provide: ContratosService, useValue: servicio },
        { provide: DialogoConfirmacionService, useValue: { confirmar } },
      ],
    });
  });

  afterEach(() => vi.useRealTimers());

  it('mientras carga muestra el indicador', async () => {
    await crear();

    expect(servicio.obtener).toHaveBeenCalledWith('id-1');
    expect(raiz.querySelector('[role="status"]')?.textContent).toContain('Cargando contrato');
  });

  it('muestra los datos del contrato con formato', async () => {
    await crear();
    await responder();

    expect(raiz.querySelector('h1')?.textContent).toBe('Proveedor Alpha');
    expect(texto()).toContain('$12,500.00');
    expect(texto()).toContain('01/01/2026');
    expect(texto()).toContain('31/12/2026');
    expect(texto()).toContain('16/09/2026, 11:13');
    expect(texto()).toContain('Sin cambios');
    expect(raiz.querySelector('app-estado-badge')?.textContent).toContain('Activo');
    expect(raiz.querySelector('.documento-meta')?.textContent).toBe('PDF, 850 KB');
  });

  it('conserva los saltos de linea de la descripcion', async () => {
    await crear();
    await responder();

    expect(getComputedStyle(raiz.querySelector('.descripcion')!).whiteSpace).toBe('pre-line');
  });

  it('un contrato inexistente muestra un aviso con salida al listado', async () => {
    await crear();
    obtener$.error(new HttpErrorResponse({ status: 404 }));
    await fixture.whenStable();

    expect(raiz.querySelector('h1')?.textContent).toBe('No se encontró el contrato');
    expect(raiz.querySelector('.vacio a')?.getAttribute('href')).toBe('/contratos');
  });

  it('ante un error permite reintentar', async () => {
    await crear();
    obtener$.error(new HttpErrorResponse({ status: 500 }));
    await fixture.whenStable();

    obtener$ = new Subject();
    boton('Reintentar')!.click();
    await responder();

    expect(servicio.obtener).toHaveBeenCalledTimes(2);
    expect(raiz.querySelector('h1')?.textContent).toBe('Proveedor Alpha');
  });

  it('al cambiar el id de la ruta carga el otro contrato', async () => {
    await crear();
    await responder();

    obtener$ = new Subject();
    fixture.componentRef.setInput('id', 'id-2');
    await fixture.whenStable();

    expect(servicio.obtener).toHaveBeenLastCalledWith('id-2');
  });

  describe('documento', () => {
    it('ver documento abre el PDF en un visor dentro de la pagina', async () => {
      await crear();
      await responder();

      boton('Ver documento')!.click();
      await fixture.whenStable();
      expect(servicio.descargarArchivo).toHaveBeenCalledWith('id-1');

      archivo$.next(new Blob(['%PDF'], { type: 'application/octet-stream' }));
      await fixture.whenStable();

      const marco = raiz.querySelector('iframe');
      expect(marco?.getAttribute('src')).toBe(urlsCreadas[0]);
      expect(marco?.getAttribute('title')).toBe('Vista previa de contrato-alpha.pdf');
    });

    it('cerrar la vista previa libera la memoria del documento', async () => {
      await crear();
      await responder();
      boton('Ver documento')!.click();
      archivo$.next(new Blob(['%PDF']));
      await fixture.whenStable();

      boton('Cerrar vista previa')!.click();
      await fixture.whenStable();

      expect(raiz.querySelector('iframe')).toBeNull();
      expect(urlsLiberadas).toEqual(urlsCreadas);
    });

    it('salir de la pantalla con el visor abierto tambien libera la memoria', async () => {
      await crear();
      await responder();
      boton('Ver documento')!.click();
      archivo$.next(new Blob(['%PDF']));
      await fixture.whenStable();

      fixture.destroy();

      expect(urlsLiberadas).toEqual(urlsCreadas);
    });

    it('un documento Word solo se puede descargar', async () => {
      await crear();
      await responder(
        contrato({
          archivoNombre: 'anexo.docx',
          archivoContentType: 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
        }),
      );

      expect(boton('Ver documento')).toBeUndefined();
      expect(boton('Descargar')).toBeDefined();
      expect(raiz.querySelector('.documento-meta')?.textContent).toContain('Word');
    });

    it('sin visor de PDF en el navegador ofrece descargar y lo explica', async () => {
      await crear({ visorPdf: false });
      await responder();

      expect(boton('Ver documento')).toBeUndefined();
      expect(texto()).toContain('Este navegador no muestra PDF dentro de la página');
    });

    it('si el documento no esta disponible lo indica', async () => {
      await crear();
      await responder();

      boton('Ver documento')!.click();
      archivo$.error(new HttpErrorResponse({ status: 404 }));
      await fixture.whenStable();

      expect(raiz.querySelector('[role="alert"]')?.textContent).toContain('no está disponible');
    });

    it('descargar entrega el archivo con su nombre original', async () => {
      // Sin temporizadores falsos: Angular sin zone.js programa la deteccion de
      // cambios con temporizadores y whenStable() no terminaria nunca.
      await crear();
      await responder();

      let enlaceDescarga: HTMLAnchorElement | null = null;
      const clic = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(function (
        this: HTMLAnchorElement,
      ) {
        enlaceDescarga = this;
      });

      boton('Descargar')!.click();
      expect(servicio.descargarArchivo).toHaveBeenCalledWith('id-1', true);

      archivo$.next(new Blob(['%PDF']));
      archivo$.complete();

      expect(enlaceDescarga!.download).toBe('contrato-alpha.pdf');
      expect(enlaceDescarga!.href).toBe(urlsCreadas[0]);
      // El enlace temporal no queda en la pagina.
      expect(document.body.contains(enlaceDescarga)).toBe(false);

      // La URL se libera en el siguiente ciclo, no en el mismo instante del clic.
      expect(urlsLiberadas).toEqual([]);
      await new Promise((resolver) => setTimeout(resolver));
      expect(urlsLiberadas).toEqual(urlsCreadas);
      clic.mockRestore();
    });
  });

  describe('estado', () => {
    it('cancelar la confirmacion no cambia nada', async () => {
      await crear();
      await responder();
      confirmar.mockResolvedValue(false);

      boton('Desactivar contrato')!.click();
      await fixture.whenStable();

      expect(confirmar).toHaveBeenCalledWith(expect.objectContaining({ peligroso: true }));
      expect(servicio.cambiarEstado).not.toHaveBeenCalled();
    });

    it('desactivar actualiza el contrato y lo confirma', async () => {
      await crear();
      await responder();
      confirmar.mockResolvedValue(true);

      boton('Desactivar contrato')!.click();
      await fixture.whenStable();
      expect(servicio.cambiarEstado).toHaveBeenCalledWith('id-1', true);

      estado$.next(contrato({ estado: 'Inactivo', fechaActualizacion: '2026-09-16T17:00:00Z' }));
      estado$.complete();
      await fixture.whenStable();

      expect(raiz.querySelector('app-estado-badge')?.textContent).toContain('Inactivo');
      expect(boton('Reactivar contrato')).toBeDefined();
      expect(TestBed.inject(NotificacionesService).notificaciones()[0].mensaje).toBe('Contrato desactivado.');
    });

    it('al reactivar informa el estado resultante, que puede ser Vencido', async () => {
      await crear();
      await responder(contrato({ estado: 'Inactivo', fechaVencimiento: '2026-01-31' }));
      confirmar.mockResolvedValue(true);

      boton('Reactivar contrato')!.click();
      await fixture.whenStable();
      expect(servicio.cambiarEstado).toHaveBeenCalledWith('id-1', false);

      estado$.next(contrato({ estado: 'Vencido', fechaVencimiento: '2026-01-31' }));
      estado$.complete();
      await fixture.whenStable();

      expect(TestBed.inject(NotificacionesService).notificaciones()[0].mensaje).toBe(
        'Contrato reactivado. Estado actual: Vencido.',
      );
    });

    it('si falla muestra un error y conserva el estado anterior', async () => {
      await crear();
      await responder();
      confirmar.mockResolvedValue(true);

      boton('Desactivar contrato')!.click();
      await fixture.whenStable();
      estado$.error(new HttpErrorResponse({ status: 500 }));
      await fixture.whenStable();

      expect(raiz.querySelector('app-estado-badge')?.textContent).toContain('Activo');
      expect(TestBed.inject(NotificacionesService).notificaciones()[0].tipo).toBe('error');
      expect(boton('Desactivar contrato')!.disabled).toBe(false);
    });
  });
});
