import { CurrencyPipe } from '@angular/common';
import { Component } from '@angular/core';

import { CONTRATO_ESTADOS } from '../../../core/models/contrato.model';
import { EstadoBadge } from '../../../shared/ui/estado-badge/estado-badge';
import { Spinner } from '../../../shared/ui/spinner/spinner';

/**
 * Catalogo visual de los componentes compartidos. Solo existe en desarrollo:
 * sirve para revisar que botones, campos y estados se ven igual en todas las
 * pantallas. No se incluye en la build de produccion (ver app.routes.ts).
 */
@Component({
  selector: 'app-guia-estilos-page',
  imports: [EstadoBadge, Spinner, CurrencyPipe],
  templateUrl: './guia-estilos-page.html',
  styleUrl: './guia-estilos-page.css',
})
export class GuiaEstilosPage {
  protected readonly estados = CONTRATO_ESTADOS;

  protected readonly filasEjemplo = [
    { proveedor: 'Proveedor Alpha', monto: 12500, vence: '2027-03-31', estado: 'Activo' },
    { proveedor: 'Proveedor Beta', monto: 8340.5, vence: '2026-10-02', estado: 'PorVencer' },
    { proveedor: 'Proveedor Gamma', monto: 150000, vence: '2026-05-20', estado: 'Vencido' },
    { proveedor: 'Proveedor Delta', monto: 2200, vence: '2026-12-15', estado: 'Inactivo' },
  ] as const;
}
