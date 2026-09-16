import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { API_URL } from '../services/api-url.token';
import { AuthService } from '../services/auth.service';
import { authInterceptor } from './auth.interceptor';

const API = 'https://api.test/api';

describe('authInterceptor', () => {
  let http: HttpClient;
  let controlador: HttpTestingController;
  let token: string | null;
  let cerrarSesion: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    token = 'token-valido';
    cerrarSesion = vi.fn();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: API_URL, useValue: API },
        { provide: AuthService, useValue: { token: () => token, cerrarSesion } },
      ],
    });

    http = TestBed.inject(HttpClient);
    controlador = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controlador.verify());

  it('anade el token a las peticiones a la API', () => {
    http.get(`${API}/contratos`).subscribe();

    const peticion = controlador.expectOne(`${API}/contratos`);
    expect(peticion.request.headers.get('Authorization')).toBe('Bearer token-valido');
    peticion.flush({});
  });

  it('no envia el token a dominios externos', () => {
    // Adjuntarlo entregaria la credencial del usuario a un tercero.
    http.get('https://otro-servicio.com/datos').subscribe();

    const peticion = controlador.expectOne('https://otro-servicio.com/datos');
    expect(peticion.request.headers.has('Authorization')).toBe(false);
    peticion.flush({});
  });

  it('no confunde una ruta que solo empieza igual que la API', () => {
    http.get(`${API}extra/datos`).subscribe();

    const peticion = controlador.expectOne(`${API}extra/datos`);
    expect(peticion.request.headers.has('Authorization')).toBe(false);
    peticion.flush({});
  });

  it('sin sesion no anade la cabecera', () => {
    token = null;
    http.get(`${API}/contratos`).subscribe({ error: () => undefined });

    const peticion = controlador.expectOne(`${API}/contratos`);
    expect(peticion.request.headers.has('Authorization')).toBe(false);
    peticion.flush({});
  });

  it('un 401 de la API cierra la sesion', () => {
    http.get(`${API}/contratos`).subscribe({ error: () => undefined });

    controlador
      .expectOne(`${API}/contratos`)
      .flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(cerrarSesion).toHaveBeenCalledWith('no-autorizado', expect.any(String));
  });

  it('un 401 del propio login NO cierra la sesion: son credenciales incorrectas', () => {
    http.post(`${API}/auth/login`, {}).subscribe({ error: () => undefined });

    controlador
      .expectOne(`${API}/auth/login`)
      .flush({}, { status: 401, statusText: 'Unauthorized' });

    expect(cerrarSesion).not.toHaveBeenCalled();
  });

  it('otros errores no cierran la sesion y llegan a quien hizo la peticion', () => {
    let estadoRecibido = 0;
    http.get(`${API}/contratos`).subscribe({ error: (e) => (estadoRecibido = e.status) });

    controlador
      .expectOne(`${API}/contratos`)
      .flush({}, { status: 500, statusText: 'Server Error' });

    expect(cerrarSesion).not.toHaveBeenCalled();
    expect(estadoRecibido).toBe(500);
  });
});
