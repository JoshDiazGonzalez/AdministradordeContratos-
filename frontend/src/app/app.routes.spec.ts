import { Type } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { Router, provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { routes } from './app.routes';
import { AuthService } from './core/services/auth.service';
import { LoginPage } from './features/auth/login-page/login-page';
import { ContratoDetailPage } from './features/contratos/contrato-detail-page/contrato-detail-page';
import { ContratoFormPage } from './features/contratos/contrato-form-page/contrato-form-page';
import { ContratosListPage } from './features/contratos/contratos-list-page/contratos-list-page';

/**
 * Navega y devuelve la pagina hoja que quedo renderizada.
 *
 * Las pantallas viven dentro del layout (AppShell), que tiene su propio
 * router-outlet. El harness devuelve el componente del outlet superior, es
 * decir el layout, asi que la pagina real se busca dentro del arbol renderizado.
 */
async function navegarA(url: string): Promise<RouterTestingHarness> {
  // Angular admite un unico harness por test.
  const harness = await RouterTestingHarness.create();
  await harness.navigateByUrl(url);
  harness.detectChanges();
  return harness;
}

function paginaRenderizada<T>(harness: RouterTestingHarness, pagina: Type<T>): T | null {
  const elemento = harness.fixture.debugElement.query(By.directive(pagina));
  return elemento ? (elemento.componentInstance as T) : null;
}

describe('Rutas', () => {
  // Sesion simulada: estas pruebas verifican el enrutado, no la autenticacion.
  let token: string | null;

  beforeEach(() => {
    token = 'token-valido';

    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes, withComponentInputBinding()),
        {
          provide: AuthService,
          useValue: {
            token: () => token,
            usuario: () => ({ username: 'admin', nombreCompleto: 'Administrador' }),
            login: vi.fn(),
            cerrarSesion: vi.fn(),
          },
        },
      ],
    });
  });

  it('sin sesion, cualquier pantalla del sistema lleva al login', async () => {
    token = null;

    const harness = await navegarA('/contratos/nuevo');

    expect(paginaRenderizada(harness, LoginPage)).not.toBeNull();
    expect(paginaRenderizada(harness, ContratoFormPage)).toBeNull();
    expect(TestBed.inject(Router).url).toBe('/login?returnUrl=%2Fcontratos%2Fnuevo');
  });

  it('/contratos/nuevo abre el formulario y no el detalle', async () => {
    // Regresion: si ':id' se declarara antes, "nuevo" se tomaria como un id.
    const harness = await navegarA('/contratos/nuevo');

    expect(paginaRenderizada(harness, ContratoFormPage)).not.toBeNull();
    expect(paginaRenderizada(harness, ContratoDetailPage)).toBeNull();
  });

  it('/contratos/:id abre el detalle y recibe el id como input', async () => {
    const harness = await navegarA('/contratos/abc-123');

    const pagina = paginaRenderizada(harness, ContratoDetailPage);
    expect(pagina).not.toBeNull();
    expect(pagina!.id()).toBe('abc-123');
  });

  it('la raiz redirige al listado de contratos', async () => {
    const harness = await navegarA('/');

    expect(paginaRenderizada(harness, ContratosListPage)).not.toBeNull();
    expect(TestBed.inject(Router).url).toBe('/contratos');
  });

  it('una ruta inexistente redirige al listado de contratos', async () => {
    await navegarA('/esto-no-existe');

    expect(TestBed.inject(Router).url).toBe('/contratos');
  });
});
