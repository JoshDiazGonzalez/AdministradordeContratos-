import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { Subject } from 'rxjs';

import { UsuarioAutenticado } from '../../../core/models/auth.model';
import { AuthService } from '../../../core/services/auth.service';
import { LoginPage } from './login-page';

describe('LoginPage', () => {
  let fixture: ComponentFixture<LoginPage>;
  let raiz: HTMLElement;
  let respuesta: Subject<UsuarioAutenticado>;
  let login: ReturnType<typeof vi.fn>;
  let navegar: ReturnType<typeof vi.spyOn>;

  async function crear(inputs: { returnUrl?: string; motivo?: string } = {}): Promise<void> {
    respuesta = new Subject<UsuarioAutenticado>();
    login = vi.fn(() => respuesta.asObservable());

    TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [provideRouter([]), { provide: AuthService, useValue: { login } }],
    });

    navegar = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);

    fixture = TestBed.createComponent(LoginPage);
    for (const [clave, valor] of Object.entries(inputs)) {
      fixture.componentRef.setInput(clave, valor);
    }
    await fixture.whenStable();
    raiz = fixture.nativeElement as HTMLElement;
  }

  function escribir(selector: string, valor: string): void {
    const campo = raiz.querySelector<HTMLInputElement>(selector)!;
    campo.value = valor;
    campo.dispatchEvent(new Event('input'));
  }

  async function enviar(): Promise<void> {
    raiz.querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
  }

  it('recien abierto no muestra errores', async () => {
    await crear();

    expect(raiz.querySelectorAll('.field-error').length).toBe(0);
    expect(raiz.querySelector('[role="alert"]')).toBeNull();
  });

  it('al enviar vacio muestra los errores y no llama a la API', async () => {
    await crear();

    await enviar();

    expect(login).not.toHaveBeenCalled();
    expect(raiz.querySelectorAll('.field-error').length).toBe(2);
    expect(raiz.querySelector('#login-username')!.getAttribute('aria-invalid')).toBe('true');
  });

  it('envia las credenciales escritas', async () => {
    await crear();
    escribir('#login-username', 'admin');
    escribir('#login-password', 'Admin123*');

    await enviar();

    expect(login).toHaveBeenCalledWith({ username: 'admin', password: 'Admin123*' });
  });

  it('mientras espera, deshabilita el boton y evita envios dobles', async () => {
    await crear();
    escribir('#login-username', 'admin');
    escribir('#login-password', 'Admin123*');

    await enviar();
    await enviar();

    const boton = raiz.querySelector<HTMLButtonElement>('button[type="submit"]')!;
    expect(boton.disabled).toBe(true);
    expect(boton.textContent).toContain('Iniciando sesión');
    expect(login).toHaveBeenCalledTimes(1);
  });

  it('con credenciales correctas navega a la pagina pedida', async () => {
    await crear({ returnUrl: '/contratos/abc-123' });
    escribir('#login-username', 'admin');
    escribir('#login-password', 'Admin123*');
    await enviar();

    respuesta.next({ username: 'admin', nombreCompleto: 'Administrador' });
    respuesta.complete();
    await fixture.whenStable();

    expect(navegar).toHaveBeenCalledWith('/contratos/abc-123');
  });

  it('ignora un returnUrl externo y va al listado', async () => {
    await crear({ returnUrl: 'https://sitio-falso.com' });
    escribir('#login-username', 'admin');
    escribir('#login-password', 'Admin123*');
    await enviar();

    respuesta.next({ username: 'admin', nombreCompleto: 'Administrador' });
    respuesta.complete();
    await fixture.whenStable();

    expect(navegar).toHaveBeenCalledWith('/contratos');
  });

  it.each([
    [401, 'Usuario o contraseña incorrectos.'],
    [0, 'No se pudo conectar con el servidor'],
    [503, 'El servicio no está disponible'],
  ])('ante un error %s muestra un mensaje claro', async (status, texto) => {
    await crear();
    escribir('#login-username', 'admin');
    escribir('#login-password', 'incorrecta');
    await enviar();

    respuesta.error(new HttpErrorResponse({ status }));
    await fixture.whenStable();

    expect(raiz.querySelector('[role="alert"]')?.textContent).toContain(texto);
    // Tras el error se puede volver a intentar.
    expect(raiz.querySelector<HTMLButtonElement>('button[type="submit"]')!.disabled).toBe(false);
  });

  it('tras un fallo vacia la contrasena pero conserva el usuario', async () => {
    await crear();
    escribir('#login-username', 'admin');
    escribir('#login-password', 'incorrecta');
    await enviar();

    respuesta.error(new HttpErrorResponse({ status: 401 }));
    await fixture.whenStable();

    expect(raiz.querySelector<HTMLInputElement>('#login-username')!.value).toBe('admin');
    expect(raiz.querySelector<HTMLInputElement>('#login-password')!.value).toBe('');
  });

  it('explica que la sesion expiro cuando se llega por ese motivo', async () => {
    await crear({ motivo: 'expirada' });

    expect(raiz.querySelector('[role="status"]')?.textContent).toContain('Su sesión expiró');
  });

  it('ignora motivos desconocidos en la URL', async () => {
    await crear({ motivo: '<script>alert(1)</script>' });

    expect(raiz.querySelector('.aviso')).toBeNull();
  });

  it('permite mostrar y ocultar la contrasena', async () => {
    await crear();
    const campo = raiz.querySelector<HTMLInputElement>('#login-password')!;
    const boton = raiz.querySelector<HTMLButtonElement>('.accion-campo')!;

    expect(campo.type).toBe('password');

    boton.click();
    await fixture.whenStable();

    expect(campo.type).toBe('text');
    expect(boton.getAttribute('aria-pressed')).toBe('true');
  });
});
