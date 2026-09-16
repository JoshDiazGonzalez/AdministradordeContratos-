import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';

import { LoginResponse } from '../models/auth.model';
import { API_URL } from './api-url.token';
import { AuthService, CLAVE_SESION } from './auth.service';

const API = 'https://api.test/api';

function respuestaLogin(minutosDeVida = 60): LoginResponse {
  return {
    token: 'token-de-prueba',
    expiresAt: new Date(Date.now() + minutosDeVida * 60_000).toISOString(),
    user: { username: 'admin', nombreCompleto: 'Administrador' },
  };
}

function configurar(): { auth: AuthService; http: HttpTestingController; router: Router } {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      provideRouter([]),
      { provide: API_URL, useValue: API },
    ],
  });

  return {
    auth: TestBed.inject(AuthService),
    http: TestBed.inject(HttpTestingController),
    router: TestBed.inject(Router),
  };
}

describe('AuthService', () => {
  beforeEach(() => sessionStorage.clear());
  afterEach(() => {
    vi.useRealTimers();
    sessionStorage.clear();
  });

  it('sin sesion previa no esta autenticado', () => {
    const { auth } = configurar();

    expect(auth.autenticado()).toBe(false);
    expect(auth.usuario()).toBeNull();
    expect(auth.token()).toBeNull();
  });

  it('login guarda la sesion y expone el usuario', () => {
    const { auth, http } = configurar();

    auth.login({ username: 'admin', password: 'Admin123*' }).subscribe();
    const peticion = http.expectOne(`${API}/auth/login`);
    expect(peticion.request.body).toEqual({ username: 'admin', password: 'Admin123*' });
    peticion.flush(respuestaLogin());

    expect(auth.autenticado()).toBe(true);
    expect(auth.usuario()?.nombreCompleto).toBe('Administrador');
    expect(auth.token()).toBe('token-de-prueba');
  });

  it('nunca guarda la contrasena en el navegador', () => {
    const { auth, http } = configurar();

    auth.login({ username: 'admin', password: 'Admin123*' }).subscribe();
    http.expectOne(`${API}/auth/login`).flush(respuestaLogin());

    const guardado = sessionStorage.getItem(CLAVE_SESION) ?? '';
    expect(guardado).not.toContain('Admin123*');
    expect(localStorage.length).toBe(0);
  });

  it('un login fallido no deja sesion', () => {
    const { auth, http } = configurar();

    auth.login({ username: 'admin', password: 'mala' }).subscribe({ error: () => undefined });
    http
      .expectOne(`${API}/auth/login`)
      .flush({ status: 401 }, { status: 401, statusText: 'Unauthorized' });

    expect(auth.autenticado()).toBe(false);
    expect(sessionStorage.getItem(CLAVE_SESION)).toBeNull();
  });

  it('restaura la sesion tras recargar la pagina', () => {
    sessionStorage.setItem(CLAVE_SESION, JSON.stringify(respuestaLogin()));

    const { auth } = configurar();

    expect(auth.autenticado()).toBe(true);
    expect(auth.usuario()?.username).toBe('admin');
  });

  it('descarta una sesion guardada que ya expiro', () => {
    sessionStorage.setItem(CLAVE_SESION, JSON.stringify(respuestaLogin(-5)));

    const { auth } = configurar();

    expect(auth.autenticado()).toBe(false);
    expect(sessionStorage.getItem(CLAVE_SESION)).toBeNull();
  });

  it.each([
    ['texto que no es JSON', '{no-es-json'],
    ['estructura incompleta', JSON.stringify({ token: 'x' })],
    ['fecha invalida', JSON.stringify({ ...respuestaLogin(), expiresAt: 'no-es-fecha' })],
  ])('descarta una sesion guardada corrupta: %s', (_caso, contenido) => {
    sessionStorage.setItem(CLAVE_SESION, contenido);

    const { auth } = configurar();

    expect(auth.autenticado()).toBe(false);
  });

  it('cerrarSesion borra la sesion y lleva al login', async () => {
    const { auth, http, router } = configurar();
    auth.login({ username: 'admin', password: 'Admin123*' }).subscribe();
    http.expectOne(`${API}/auth/login`).flush(respuestaLogin());
    const navegar = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    auth.cerrarSesion();

    expect(auth.autenticado()).toBe(false);
    expect(sessionStorage.getItem(CLAVE_SESION)).toBeNull();
    expect(navegar).toHaveBeenCalledWith(['/login'], { queryParams: {} });
  });

  it('cierra la sesion automaticamente al expirar el token', () => {
    vi.useFakeTimers();
    const { auth, http, router } = configurar();
    const navegar = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    auth.login({ username: 'admin', password: 'Admin123*' }).subscribe();
    http.expectOne(`${API}/auth/login`).flush(respuestaLogin(10));

    vi.advanceTimersByTime(9 * 60_000);
    expect(auth.autenticado()).toBe(true);

    vi.advanceTimersByTime(2 * 60_000);
    expect(auth.autenticado()).toBe(false);
    expect(navegar).toHaveBeenCalledWith(
      ['/login'],
      expect.objectContaining({ queryParams: expect.objectContaining({ motivo: 'expirada' }) }),
    );
  });

  it('token() detecta la expiracion aunque el temporizador no haya disparado', () => {
    // Caso real: el portatil estuvo suspendido y los temporizadores se retrasan.
    vi.useFakeTimers();
    const { auth, http, router } = configurar();
    vi.spyOn(router, 'navigate').mockResolvedValue(true);

    auth.login({ username: 'admin', password: 'Admin123*' }).subscribe();
    http.expectOne(`${API}/auth/login`).flush(respuestaLogin(10));

    vi.setSystemTime(Date.now() + 11 * 60_000);

    expect(auth.token()).toBeNull();
    expect(auth.autenticado()).toBe(false);
  });
});
