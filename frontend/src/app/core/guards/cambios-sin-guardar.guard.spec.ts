import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';

import { DialogoConfirmacionService } from '../../shared/ui/dialogo-confirmacion/dialogo-confirmacion.service';
import { ConCambiosSinGuardar, cambiosSinGuardarGuard } from './cambios-sin-guardar.guard';

describe('cambiosSinGuardarGuard', () => {
  let confirmar: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    confirmar = vi.fn();
    TestBed.configureTestingModule({
      providers: [{ provide: DialogoConfirmacionService, useValue: { confirmar } }],
    });
  });

  function ejecutar(conCambios: boolean): unknown {
    const componente: ConCambiosSinGuardar = { tieneCambiosSinGuardar: () => conCambios };
    return TestBed.runInInjectionContext(() =>
      cambiosSinGuardarGuard(
        componente,
        {} as ActivatedRouteSnapshot,
        {} as RouterStateSnapshot,
        {} as RouterStateSnapshot,
      ),
    );
  }

  it('sin cambios deja salir sin preguntar', () => {
    expect(ejecutar(false)).toBe(true);
    expect(confirmar).not.toHaveBeenCalled();
  });

  it.each([true, false])('con cambios pregunta y respeta la respuesta (%s)', async (respuesta) => {
    confirmar.mockResolvedValue(respuesta);

    await expect(ejecutar(true)).resolves.toBe(respuesta);
    expect(confirmar).toHaveBeenCalledWith(expect.objectContaining({ peligroso: true }));
  });
});
