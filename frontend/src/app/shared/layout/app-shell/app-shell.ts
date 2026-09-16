import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter, map, tap } from 'rxjs';

import { AuthService } from '../../../core/services/auth.service';

export type SeccionNavegacion = 'dashboard' | 'contratos' | 'nuevo';

interface EnlaceNavegacion {
  readonly seccion: SeccionNavegacion;
  readonly ruta: string;
  readonly etiqueta: string;
}

/**
 * Seccion del menu que corresponde a una URL.
 *
 * Se decide con una regla explicita en lugar de routerLinkActive: "/contratos"
 * y "/contratos/nuevo" comparten prefijo, y con coincidencia por prefijo ambas
 * entradas quedarian marcadas a la vez. El detalle (/contratos/:id) pertenece a
 * la seccion Contratos.
 */
export function seccionDeUrl(url: string): SeccionNavegacion | null {
  const ruta = url.split(/[?#]/)[0];

  if (ruta === '/contratos/nuevo') {
    return 'nuevo';
  }

  if (ruta === '/contratos' || ruta.startsWith('/contratos/')) {
    return 'contratos';
  }

  if (ruta === '/dashboard' || ruta.startsWith('/dashboard/')) {
    return 'dashboard';
  }

  return null;
}

/**
 * Estructura comun de las pantallas autenticadas: barra superior, navegacion
 * lateral y area de contenido. La pantalla de login queda fuera de este layout.
 */
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.css',
})
export class AppShell {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);

  protected readonly usuario = this.auth.usuario;

  protected readonly enlaces: readonly EnlaceNavegacion[] = [
    { seccion: 'dashboard', ruta: '/dashboard', etiqueta: 'Dashboard' },
    { seccion: 'contratos', ruta: '/contratos', etiqueta: 'Contratos' },
    { seccion: 'nuevo', ruta: '/contratos/nuevo', etiqueta: 'Nuevo contrato' },
  ];

  /** Menu desplegable en pantallas pequenas. */
  protected readonly menuAbierto = signal(false);

  protected readonly seccionActiva = toSignal(
    this.router.events.pipe(
      filter((evento): evento is NavigationEnd => evento instanceof NavigationEnd),
      // Al navegar se cierra el menu movil: si no, taparia la pagina recien abierta.
      tap(() => this.menuAbierto.set(false)),
      map((evento) => seccionDeUrl(evento.urlAfterRedirects)),
    ),
    { initialValue: seccionDeUrl(this.router.url) },
  );

  protected cerrarSesion(): void {
    this.auth.cerrarSesion('manual');
  }

  protected alternarMenu(): void {
    this.menuAbierto.update((abierto) => !abierto);
  }
}
