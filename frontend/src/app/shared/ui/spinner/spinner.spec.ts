import { TestBed } from '@angular/core/testing';

import { Spinner } from './spinner';

describe('Spinner', () => {
  it('anuncia la espera a lectores de pantalla con role="status"', () => {
    const fixture = TestBed.createComponent(Spinner);
    fixture.componentRef.setInput('etiqueta', 'Cargando contratos');
    fixture.detectChanges();

    const estado = (fixture.nativeElement as HTMLElement).querySelector('[role="status"]');
    expect(estado?.textContent).toContain('Cargando contratos');
  });

  it('por defecto el texto solo es visible para lectores de pantalla', () => {
    const fixture = TestBed.createComponent(Spinner);
    fixture.detectChanges();

    const texto = (fixture.nativeElement as HTMLElement).querySelector('[role="status"] > span:last-child');
    expect(texto?.classList).toContain('sr-only');
  });

  it('puede mostrar el texto junto al indicador', () => {
    const fixture = TestBed.createComponent(Spinner);
    fixture.componentRef.setInput('mostrarTexto', true);
    fixture.detectChanges();

    const texto = (fixture.nativeElement as HTMLElement).querySelector('[role="status"] > span:last-child');
    expect(texto?.classList).not.toContain('sr-only');
  });
});
