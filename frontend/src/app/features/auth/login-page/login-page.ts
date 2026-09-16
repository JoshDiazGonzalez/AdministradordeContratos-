import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';

import { AuthService } from '../../../core/services/auth.service';
import { returnUrlSegura } from '../../../core/services/return-url';
import { Spinner } from '../../../shared/ui/spinner/spinner';

/** Mensaje que explica por que se llego al login, segun ?motivo= */
const AVISOS_POR_MOTIVO: Readonly<Record<string, string>> = {
  expirada: 'Su sesión expiró. Inicie sesión de nuevo para continuar.',
  'no-autorizado': 'Su sesión ya no es válida. Inicie sesión de nuevo para continuar.',
};

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule, Spinner],
  templateUrl: './login-page.html',
  styleUrl: './login-page.css',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  /** Parametros de consulta, recibidos como inputs gracias al router. */
  readonly returnUrl = input<string>();
  readonly motivo = input<string>();

  protected readonly formulario = inject(NonNullableFormBuilder).group({
    username: ['', [Validators.required, Validators.maxLength(50)]],
    password: ['', [Validators.required, Validators.maxLength(128)]],
  });

  protected readonly enviando = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly mostrarPassword = signal(false);

  /**
   * Los errores de campo se muestran solo tras intentar enviar o tras salir del
   * campo: marcar en rojo un formulario recien abierto no ayuda a nadie.
   */
  protected readonly intentoEnviar = signal(false);

  protected readonly aviso = computed(() => {
    const motivo = this.motivo();
    return motivo ? (AVISOS_POR_MOTIVO[motivo] ?? null) : null;
  });

  protected campoInvalido(nombre: 'username' | 'password'): boolean {
    const control = this.formulario.controls[nombre];
    return control.invalid && (control.touched || this.intentoEnviar());
  }

  protected alternarPassword(): void {
    this.mostrarPassword.update((visible) => !visible);
  }

  protected enviar(): void {
    this.intentoEnviar.set(true);
    this.error.set(null);

    if (this.formulario.invalid || this.enviando()) {
      this.formulario.markAllAsTouched();
      return;
    }

    this.enviando.set(true);

    this.auth
      .login(this.formulario.getRawValue())
      .pipe(finalize(() => this.enviando.set(false)))
      .subscribe({
        next: () => {
          void this.router.navigateByUrl(returnUrlSegura(this.returnUrl()));
        },
        error: (error: unknown) => {
          this.error.set(mensajeDeError(error));
          // Tras un fallo se vacia la contrasena y se enfoca para reintentar.
          this.formulario.controls.password.reset();
          this.formulario.controls.password.markAsUntouched();
          this.intentoEnviar.set(false);
          queueMicrotask(() => document.getElementById('login-password')?.focus());
        },
      });
  }
}

function mensajeDeError(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'No se pudo iniciar sesión. Inténtelo de nuevo.';
  }

  switch (error.status) {
    case 401:
      // Mismo texto exista o no el usuario: no se revela cual de los dos fallo.
      return 'Usuario o contraseña incorrectos.';
    case 0:
      return 'No se pudo conectar con el servidor. Compruebe su conexión e inténtelo de nuevo.';
    case 429:
      return 'Demasiados intentos. Espere unos minutos antes de volver a intentarlo.';
    default:
      return error.status >= 500
        ? 'El servicio no está disponible en este momento. Inténtelo de nuevo en unos minutos.'
        : 'No se pudo iniciar sesión. Inténtelo de nuevo.';
  }
}
