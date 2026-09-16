import { TestBed } from '@angular/core/testing';

import { CONTRATO_ESTADOS, ContratoEstado } from '../../../core/models/contrato.model';
import { EstadoBadge } from './estado-badge';

function renderizar(estado: ContratoEstado): HTMLElement {
  const fixture = TestBed.createComponent(EstadoBadge);
  fixture.componentRef.setInput('estado', estado);
  fixture.detectChanges();
  return fixture.nativeElement as HTMLElement;
}

describe('EstadoBadge', () => {
  it.each([
    ['Activo', 'Activo'],
    ['PorVencer', 'Por vencer'],
    ['Vencido', 'Vencido'],
    ['Inactivo', 'Inactivo'],
  ] as const)('el estado %s muestra el texto "%s"', (estado, texto) => {
    // El estado nunca se comunica solo con color.
    expect(renderizar(estado).textContent?.trim()).toBe(texto);
  });

  it('aplica una clase distinta por estado para el color', () => {
    const clases = CONTRATO_ESTADOS.map((estado) => renderizar(estado).className);

    expect(new Set(clases).size).toBe(CONTRATO_ESTADOS.length);
    expect(renderizar('PorVencer').classList).toContain('badge-PorVencer');
  });

  it('oculta el punto de color a los lectores de pantalla', () => {
    const punto = renderizar('Vencido').querySelector('.punto');

    expect(punto?.getAttribute('aria-hidden')).toBe('true');
  });
});
