import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

/**
 * Estructura comun de las pantallas autenticadas: navegacion y area de contenido.
 * La pantalla de login queda fuera de este layout.
 */
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.css',
})
export class AppShell {
  protected readonly enlaces = [
    { ruta: '/dashboard', etiqueta: 'Dashboard' },
    { ruta: '/contratos', etiqueta: 'Contratos' },
    { ruta: '/contratos/nuevo', etiqueta: 'Nuevo contrato' },
  ] as const;
}
