import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, map, tap } from 'rxjs';

import { LoginRequest, LoginResponse, UsuarioAutenticado } from '../models/auth.model';
import { API_URL } from './api-url.token';

/** Lo unico que se guarda en el navegador. Nunca la contrasena. */
interface SesionGuardada {
  token: string;
  /** Instante ISO 8601 de expiracion, tal como lo envia la API. */
  expiresAt: string;
  user: UsuarioAutenticado;
}

/** Motivo del cierre de sesion. La pantalla de login lo usa para explicar que paso. */
export type MotivoCierre = 'manual' | 'expirada' | 'no-autorizado';

export const CLAVE_SESION = 'contratos.sesion';

/**
 * Autenticacion del usuario.
 *
 * Almacenamiento: sessionStorage, no localStorage.
 *  - Sobrevive a recargar la pagina, asi que F5 no expulsa al usuario.
 *  - Se borra al cerrar la pestana: en un equipo compartido de una oficina la
 *    sesion no queda abierta para la siguiente persona.
 *  - No se comparte entre pestanas.
 * Se guarda solo el token, su expiracion y el nombre visible del usuario.
 *
 * El estado se expone con signals porque la aplicacion es zoneless: la barra
 * superior y los guards reaccionan solos cuando cambia la sesion.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly loginUrl = `${inject(API_URL)}/auth/login`;

  private readonly sesion = signal<SesionGuardada | null>(this.restaurar());
  private temporizadorExpiracion: ReturnType<typeof setTimeout> | null = null;

  readonly usuario = computed(() => this.sesion()?.user ?? null);
  readonly autenticado = computed(() => this.sesion() !== null);

  constructor() {
    const actual = this.sesion();
    if (actual) {
      this.programarExpiracion(actual.expiresAt);
    }
  }

  login(credenciales: LoginRequest): Observable<UsuarioAutenticado> {
    return this.http.post<LoginResponse>(this.loginUrl, credenciales).pipe(
      tap((respuesta) => this.guardar(respuesta)),
      map((respuesta) => respuesta.user),
    );
  }

  /**
   * Token vigente, o null si no hay sesion o ya expiro.
   * Comprueba la expiracion en cada lectura: el temporizador puede no haber
   * disparado si el equipo estuvo suspendido.
   */
  token(): string | null {
    const actual = this.sesion();

    if (!actual) {
      return null;
    }

    if (haExpirado(actual.expiresAt)) {
      this.cerrarSesion('expirada');
      return null;
    }

    return actual.token;
  }

  cerrarSesion(motivo: MotivoCierre = 'manual', returnUrl?: string): void {
    this.limpiar();

    const queryParams: Record<string, string> = {};
    if (motivo !== 'manual') {
      queryParams['motivo'] = motivo;
    }
    if (returnUrl) {
      queryParams['returnUrl'] = returnUrl;
    }

    void this.router.navigate(['/login'], { queryParams });
  }

  private guardar(respuesta: LoginResponse): void {
    const sesion: SesionGuardada = {
      token: respuesta.token,
      expiresAt: respuesta.expiresAt,
      user: respuesta.user,
    };

    try {
      sessionStorage.setItem(CLAVE_SESION, JSON.stringify(sesion));
    } catch {
      // Almacenamiento bloqueado (modo privado estricto): la sesion sigue
      // funcionando en memoria, solo no sobrevivira a recargar la pagina.
    }

    this.sesion.set(sesion);
    this.programarExpiracion(sesion.expiresAt);
  }

  private limpiar(): void {
    if (this.temporizadorExpiracion !== null) {
      clearTimeout(this.temporizadorExpiracion);
      this.temporizadorExpiracion = null;
    }

    try {
      sessionStorage.removeItem(CLAVE_SESION);
    } catch {
      // Nada que limpiar si el almacenamiento no esta disponible.
    }

    this.sesion.set(null);
  }

  /** Recupera la sesion tras recargar, descartando datos corruptos o expirados. */
  private restaurar(): SesionGuardada | null {
    try {
      const texto = sessionStorage.getItem(CLAVE_SESION);
      if (!texto) {
        return null;
      }

      const sesion = JSON.parse(texto) as Partial<SesionGuardada>;

      const valida =
        typeof sesion.token === 'string' &&
        typeof sesion.expiresAt === 'string' &&
        typeof sesion.user?.username === 'string' &&
        !haExpirado(sesion.expiresAt);

      if (!valida) {
        sessionStorage.removeItem(CLAVE_SESION);
        return null;
      }

      return sesion as SesionGuardada;
    } catch {
      return null;
    }
  }

  /** Cierra la sesion automaticamente en el instante en que expira el token. */
  private programarExpiracion(expiresAt: string): void {
    if (this.temporizadorExpiracion !== null) {
      clearTimeout(this.temporizadorExpiracion);
    }

    const restante = Date.parse(expiresAt) - Date.now();

    // setTimeout no admite esperas mayores a ~24,8 dias; un token de la API
    // dura minutos, pero se protege el caso para no disparar de inmediato.
    const maximo = 2_147_483_647;
    if (restante > maximo) {
      return;
    }

    this.temporizadorExpiracion = setTimeout(
      () => this.cerrarSesion('expirada', this.router.url),
      Math.max(restante, 0),
    );
  }
}

function haExpirado(expiresAt: string): boolean {
  const instante = Date.parse(expiresAt);
  return Number.isNaN(instante) || instante <= Date.now();
}
