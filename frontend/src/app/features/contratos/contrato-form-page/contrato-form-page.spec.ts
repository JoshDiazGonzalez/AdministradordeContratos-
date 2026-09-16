import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Subject } from 'rxjs';

import { Contrato, CrearContratoRequest } from '../../../core/models/contrato.model';
import { ContratosService } from '../../../core/services/contratos.service';
import { NotificacionesService } from '../../../core/services/notificaciones.service';
import { ContratoFormPage } from './contrato-form-page';

describe('ContratoFormPage', () => {
  let fixture: ComponentFixture<ContratoFormPage>;
  let raiz: HTMLElement;
  let respuesta: Subject<Contrato>;
  let crear: ReturnType<typeof vi.fn>;
  let navegar: ReturnType<typeof vi.spyOn>;

  beforeEach(async () => {
    respuesta = new Subject<Contrato>();
    crear = vi.fn(() => respuesta.asObservable());

    TestBed.configureTestingModule({
      imports: [ContratoFormPage],
      providers: [provideRouter([]), { provide: ContratosService, useValue: { crear } }],
    });

    navegar = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(ContratoFormPage);
    await fixture.whenStable();
    raiz = fixture.nativeElement as HTMLElement;
  });

  const campo = <T extends HTMLElement = HTMLInputElement>(id: string) =>
    raiz.querySelector<T>(`#contrato-${id}`)!;

  async function escribir(id: string, valor: string): Promise<void> {
    const elemento = campo<HTMLInputElement | HTMLTextAreaElement>(id);
    elemento.value = valor;
    elemento.dispatchEvent(new Event('input'));
    await fixture.whenStable();
  }

  async function elegirArchivo(nombre = 'contrato.pdf', bytes = 2048): Promise<File> {
    const archivo = new File(['%PDF'], nombre, { type: 'application/pdf' });
    Object.defineProperty(archivo, 'size', { value: bytes });

    const entrada = campo('archivo');
    Object.defineProperty(entrada, 'files', { value: { item: () => archivo }, configurable: true });
    entrada.dispatchEvent(new Event('change'));
    await fixture.whenStable();
    return archivo;
  }

  async function rellenarValido(): Promise<File> {
    await escribir('nombreProveedor', '  Proveedor Alpha  ');
    await escribir('monto', '15,000.50');
    await escribir('fechaInicio', '2026-01-01');
    await escribir('fechaVencimiento', '2026-12-31');
    await escribir('descripcion', 'Servicios de software.');
    return elegirArchivo();
  }

  async function enviar(): Promise<void> {
    raiz.querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
  }

  const errores = () => [...raiz.querySelectorAll('.field-error')].map((e) => e.textContent!.trim());

  it('recien abierto no muestra errores', () => {
    expect(errores()).toEqual([]);
  });

  it('al enviar vacio muestra todos los errores y no llama a la API', async () => {
    await enviar();

    expect(crear).not.toHaveBeenCalled();
    expect(errores()).toEqual([
      'El nombre del proveedor es obligatorio.',
      'El monto es obligatorio.',
      'La fecha de inicio es obligatoria.',
      'La fecha de vencimiento es obligatoria.',
      'La descripción es obligatoria.',
      'Debe seleccionar un documento.',
    ]);
  });

  it('enfoca el primer campo con error para poder corregirlo', async () => {
    await escribir('nombreProveedor', 'Proveedor Alpha');
    await enviar();
    await new Promise((r) => setTimeout(r));

    expect(document.activeElement?.id).toBe('contrato-monto');
  });

  it('un nombre formado solo por espacios no es valido', async () => {
    await escribir('nombreProveedor', '    ');
    await enviar();

    expect(errores()).toContain('El nombre del proveedor es obligatorio.');
  });

  it.each([
    ['0', 'El monto debe ser mayor que 0.'],
    ['15000,50', 'Escriba el monto con punto decimal, por ejemplo 15000.50 o 15,000.50.'],
    ['10.555', 'El monto admite como máximo dos decimales.'],
  ])('el monto "%s" muestra "%s"', async (valor, mensaje) => {
    await escribir('monto', valor);
    await enviar();

    expect(errores()).toContain(mensaje);
  });

  it('al salir del campo el monto se muestra con formato', async () => {
    await escribir('monto', '15000.5');
    campo('monto').dispatchEvent(new Event('blur'));
    await fixture.whenStable();

    expect(campo('monto').value).toBe('15,000.50');
  });

  it('avisa si el vencimiento es anterior al inicio', async () => {
    await escribir('fechaInicio', '2026-12-31');
    await escribir('fechaVencimiento', '2026-01-01');
    await enviar();

    expect(errores()).toContain('La fecha de vencimiento no puede ser anterior a la fecha de inicio.');
  });

  it('el contador de la descripcion refleja lo escrito', async () => {
    await escribir('descripcion', 'Hola');

    expect(campo<HTMLElement>('descripcion-contador').textContent!.replace(/\s+/g, ' ').trim()).toBe(
      '4 / 1000',
    );
  });

  describe('documento', () => {
    it('muestra el archivo elegido con su tamano y permite quitarlo', async () => {
      await elegirArchivo('contrato alpha.pdf', 850 * 1024);

      expect(raiz.querySelector('.archivo-nombre')?.textContent).toBe('contrato alpha.pdf');
      expect(raiz.querySelector('.archivo-tamano')?.textContent).toBe('850 KB');

      raiz.querySelector<HTMLButtonElement>('.quitar')!.click();
      await fixture.whenStable();

      expect(raiz.querySelector('.archivo-nombre')).toBeNull();
      expect(campo('archivo')).not.toBeNull();
    });

    it.each([
      ['foto.png', 2048, 'Solo se aceptan documentos PDF, DOC o DOCX.'],
      ['contrato.pdf', 11 * 1024 * 1024, 'El documento no puede superar los 10 MB.'],
      ['contrato.pdf', 0, 'El documento está vacío.'],
    ])('rechaza %s de %s bytes antes de subirlo', async (nombre, bytes, mensaje) => {
      await elegirArchivo(nombre, bytes);

      expect(errores()).toContain(mensaje);
    });
  });

  describe('al guardar', () => {
    it('envia los datos limpios: nombre recortado y monto como numero', async () => {
      const archivo = await rellenarValido();
      await enviar();

      expect(crear).toHaveBeenCalledWith<[CrearContratoRequest]>({
        nombreProveedor: 'Proveedor Alpha',
        montoContrato: 15000.5,
        fechaInicio: '2026-01-01',
        fechaVencimiento: '2026-12-31',
        descripcion: 'Servicios de software.',
        archivo,
      });
    });

    it('mientras guarda deshabilita el boton y evita envios dobles', async () => {
      await rellenarValido();
      await enviar();
      await enviar();

      expect(crear).toHaveBeenCalledTimes(1);
      const boton = raiz.querySelector<HTMLButtonElement>('button[type="submit"]')!;
      expect(boton.disabled).toBe(true);
      expect(boton.textContent).toContain('Guardando');
    });

    it('al terminar avisa del exito y vuelve al listado', async () => {
      await rellenarValido();
      await enviar();

      respuesta.next({ nombreProveedor: 'Proveedor Alpha' } as Contrato);
      respuesta.complete();
      await fixture.whenStable();

      expect(TestBed.inject(NotificacionesService).notificaciones()[0].mensaje).toBe(
        'Contrato de Proveedor Alpha registrado.',
      );
      expect(navegar).toHaveBeenCalledWith(['/contratos']);
      // Tras guardar, salir no debe pedir confirmacion.
      expect(fixture.componentInstance.tieneCambiosSinGuardar()).toBe(false);
    });

    it('coloca cada error de la API junto a su campo', async () => {
      await rellenarValido();
      await enviar();

      respuesta.error(
        new HttpErrorResponse({
          status: 400,
          error: {
            title: 'Error de validacion',
            errors: {
              NombreProveedor: ['El nombre del proveedor es obligatorio.'],
              Archivo: ['El contenido del archivo no corresponde a un documento .pdf válido.'],
            },
          },
        }),
      );
      await fixture.whenStable();

      expect(errores()).toContain('El nombre del proveedor es obligatorio.');
      expect(errores()).toContain('El contenido del archivo no corresponde a un documento .pdf válido.');
    });

    it('un error de la API desaparece al corregir el campo', async () => {
      await rellenarValido();
      await enviar();
      respuesta.error(
        new HttpErrorResponse({
          status: 400,
          error: { errors: { NombreProveedor: ['Ya existe un proveedor con ese nombre.'] } },
        }),
      );
      await fixture.whenStable();
      expect(errores()).toContain('Ya existe un proveedor con ese nombre.');

      await escribir('nombreProveedor', 'Proveedor Beta');

      expect(errores()).not.toContain('Ya existe un proveedor con ese nombre.');
    });

    it('un 413 se muestra en el campo del documento', async () => {
      await rellenarValido();
      await enviar();
      respuesta.error(new HttpErrorResponse({ status: 413 }));
      await fixture.whenStable();

      expect(errores()).toContain('El documento no puede superar los 10 MB.');
    });

    it('sin conexion conserva los datos escritos para reintentar', async () => {
      await rellenarValido();
      await enviar();
      respuesta.error(new HttpErrorResponse({ status: 0 }));
      await fixture.whenStable();

      expect(raiz.querySelector('[role="alert"]')?.textContent).toContain(
        'No se pudo conectar con el servidor',
      );
      expect(campo('nombreProveedor').value).toBe('  Proveedor Alpha  ');
      expect(raiz.querySelector<HTMLButtonElement>('button[type="submit"]')!.disabled).toBe(false);
      expect(navegar).not.toHaveBeenCalled();
    });
  });

  describe('cambios sin guardar', () => {
    it('sin escribir nada se puede salir sin confirmar', () => {
      expect(fixture.componentInstance.tieneCambiosSinGuardar()).toBe(false);
    });

    it('con datos escritos se considera que hay cambios', async () => {
      await escribir('nombreProveedor', 'Proveedor Alpha');

      expect(fixture.componentInstance.tieneCambiosSinGuardar()).toBe(true);
    });
  });
});
