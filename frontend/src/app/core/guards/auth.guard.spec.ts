import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { AuthService } from '../services/auth.service';
import { authGuard, invitadoGuard } from './auth.guard';

@Component({ template: 'privada' })
class PaginaPrivada {}

@Component({ template: 'login' })
class PaginaLogin {}

describe('Guards de autenticacion', () => {
  let token: string | null;

  beforeEach(() => {
    token = null;

    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'login', component: PaginaLogin, canActivate: [invitadoGuard] },
          { path: 'contratos', component: PaginaPrivada, canActivate: [authGuard] },
          { path: 'contratos/:id', component: PaginaPrivada, canActivate: [authGuard] },
        ]),
        { provide: AuthService, useValue: { token: () => token } },
      ],
    });
  });

  it('sin sesion envia al login recordando la pagina pedida', async () => {
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/contratos/abc-123?download=true');

    const url = TestBed.inject(Router).parseUrl(TestBed.inject(Router).url);
    expect(url.root.children['primary'].segments.map((s) => s.path)).toEqual(['login']);
    expect(url.queryParams['returnUrl']).toBe('/contratos/abc-123?download=true');
  });

  it('con sesion deja pasar', async () => {
    token = 'valido';
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/contratos');

    expect(TestBed.inject(Router).url).toBe('/contratos');
  });

  it('con sesion iniciada, el login redirige al listado', async () => {
    token = 'valido';
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/login');

    expect(TestBed.inject(Router).url).toBe('/contratos');
  });

  it('sin sesion el login se muestra', async () => {
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/login');

    expect(TestBed.inject(Router).url).toBe('/login');
  });
});
