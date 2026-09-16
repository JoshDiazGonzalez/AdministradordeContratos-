import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DialogoConfirmacion } from './dialogo-confirmacion';
import { DialogoConfirmacionService } from './dialogo-confirmacion.service';

/**
 * jsdom no implementa showModal() ni close(). Se sustituyen por lo minimo para
 * probar la logica del componente. El comportamiento modal real (foco atrapado,
 * Escape, fondo bloqueado) lo da el navegador y se verifica en la aplicacion.
 */
beforeAll(() => {
  const prototipo = HTMLDialogElement.prototype as HTMLDialogElement & Record<string, unknown>;
  prototipo.showModal ??= function (this: HTMLDialogElement) {
    this.setAttribute('open', '');
  };
  prototipo.close ??= function (this: HTMLDialogElement) {
    this.removeAttribute('open');
  };
});

describe('DialogoConfirmacion', () => {
  let fixture: ComponentFixture<DialogoConfirmacion>;
  let servicio: DialogoConfirmacionService;
  let dialogo: HTMLDialogElement;

  beforeEach(async () => {
    fixture = TestBed.createComponent(DialogoConfirmacion);
    servicio = TestBed.inject(DialogoConfirmacionService);
    await fixture.whenStable();
    dialogo = (fixture.nativeElement as HTMLElement).querySelector('dialog')!;
  });

  /**
   * La promesa se devuelve dentro de un objeto a proposito: una funcion async
   * que devuelve una promesa la aplana, y "await abrir()" esperaria la
   * respuesta del usuario, que en el test nunca llega.
   */
  async function abrir(peligroso = false): Promise<{ respuesta: Promise<boolean> }> {
    const respuesta = servicio.confirmar({
      titulo: '¿Salir sin guardar?',
      mensaje: 'Se perderán los datos.',
      confirmar: 'Salir sin guardar',
      cancelar: 'Seguir editando',
      peligroso,
    });
    await fixture.whenStable();
    return { respuesta };
  }

  const boton = (texto: string) =>
    [...dialogo.querySelectorAll('button')].find((b) => b.textContent?.includes(texto))!;

  it('se abre con el titulo, el mensaje y los textos de las acciones', async () => {
    await abrir();

    expect(dialogo.open).toBe(true);
    expect(dialogo.textContent).toContain('¿Salir sin guardar?');
    expect(boton('Seguir editando')).toBeDefined();
    expect(boton('Salir sin guardar')).toBeDefined();
  });

  it('confirmar resuelve true y cierra', async () => {
    const { respuesta } = await abrir();

    boton('Salir sin guardar').click();
    await fixture.whenStable();

    await expect(respuesta).resolves.toBe(true);
    expect(dialogo.open).toBe(false);
  });

  it('cancelar resuelve false', async () => {
    const { respuesta } = await abrir();

    boton('Seguir editando').click();

    await expect(respuesta).resolves.toBe(false);
  });

  it('Escape equivale a cancelar', async () => {
    const { respuesta } = await abrir();

    dialogo.dispatchEvent(new Event('cancel', { cancelable: true }));

    await expect(respuesta).resolves.toBe(false);
  });

  it('pulsar el fondo equivale a cancelar, pulsar el contenido no', async () => {
    const { respuesta } = await abrir();

    dialogo.querySelector('.contenido')!.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    expect(servicio.solicitud()).not.toBeNull();

    dialogo.dispatchEvent(new MouseEvent('click', { bubbles: true }));
    await expect(respuesta).resolves.toBe(false);
  });

  it('una accion peligrosa usa el estilo de peligro', async () => {
    await abrir(true);

    expect(boton('Salir sin guardar').classList).toContain('btn-peligro');
  });

  it('abrir otro dialogo cancela el anterior en lugar de dejarlo esperando', async () => {
    const { respuesta: primera } = await abrir();
    const { respuesta: segunda } = await abrir();

    await expect(primera).resolves.toBe(false);
    boton('Salir sin guardar').click();
    await expect(segunda).resolves.toBe(true);
  });
});
