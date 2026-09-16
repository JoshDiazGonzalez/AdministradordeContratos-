import { TestBed } from '@angular/core/testing';

import { DURACION_EXITO_MS, NotificacionesService } from './notificaciones.service';

describe('NotificacionesService', () => {
  let servicio: NotificacionesService;

  beforeEach(() => {
    vi.useFakeTimers();
    servicio = TestBed.inject(NotificacionesService);
  });

  afterEach(() => vi.useRealTimers());

  it('un aviso de exito desaparece solo', () => {
    servicio.exito('Contrato registrado.');
    expect(servicio.notificaciones().map((n) => n.mensaje)).toEqual(['Contrato registrado.']);

    vi.advanceTimersByTime(DURACION_EXITO_MS);

    expect(servicio.notificaciones()).toEqual([]);
  });

  it('un error permanece hasta que se cierra', () => {
    // Quien lee despacio o usa lector de pantalla no debe perderlo.
    servicio.error('No se pudo guardar.');

    vi.advanceTimersByTime(DURACION_EXITO_MS * 10);
    expect(servicio.notificaciones()).toHaveLength(1);

    servicio.cerrar(servicio.notificaciones()[0].id);
    expect(servicio.notificaciones()).toEqual([]);
  });

  it('conserva como mucho tres avisos', () => {
    for (const n of [1, 2, 3, 4, 5]) {
      servicio.error(`Error ${n}`);
    }

    expect(servicio.notificaciones().map((n) => n.mensaje)).toEqual(['Error 3', 'Error 4', 'Error 5']);
  });

  it('cerrar a mano un exito cancela su temporizador sin afectar a otros', () => {
    servicio.exito('Primero');
    servicio.exito('Segundo');
    const [primero] = servicio.notificaciones();

    servicio.cerrar(primero.id);
    vi.advanceTimersByTime(DURACION_EXITO_MS - 1);

    expect(servicio.notificaciones().map((n) => n.mensaje)).toEqual(['Segundo']);
  });
});
