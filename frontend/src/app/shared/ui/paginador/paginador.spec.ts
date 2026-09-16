import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Paginador } from './paginador';

describe('Paginador', () => {
  let fixture: ComponentFixture<Paginador>;
  let raiz: HTMLElement;

  async function crear(estado: {
    page: number;
    pageSize?: number;
    totalItems: number;
    totalPages: number;
    deshabilitado?: boolean;
  }): Promise<void> {
    fixture = TestBed.createComponent(Paginador);
    fixture.componentRef.setInput('page', estado.page);
    fixture.componentRef.setInput('pageSize', estado.pageSize ?? 10);
    fixture.componentRef.setInput('totalItems', estado.totalItems);
    fixture.componentRef.setInput('totalPages', estado.totalPages);
    fixture.componentRef.setInput('elementos', 'contratos');
    fixture.componentRef.setInput('deshabilitado', estado.deshabilitado ?? false);
    await fixture.whenStable();
    raiz = fixture.nativeElement as HTMLElement;
  }

  const boton = (texto: string) =>
    [...raiz.querySelectorAll<HTMLButtonElement>('button')].find((b) =>
      b.textContent?.includes(texto),
    )!;

  const resumen = () => raiz.querySelector('.resumen')!.textContent!.replace(/\s+/g, ' ').trim();

  it('muestra el rango visible y el total', async () => {
    await crear({ page: 2, totalItems: 42, totalPages: 5 });

    expect(resumen()).toBe('Mostrando 11–20 de 42 contratos');
  });

  it('en la ultima pagina el rango termina en el total, no en page*pageSize', async () => {
    await crear({ page: 5, totalItems: 42, totalPages: 5 });

    expect(resumen()).toBe('Mostrando 41–42 de 42 contratos');
  });

  it('sin resultados lo indica sin mostrar un rango 1-0', async () => {
    await crear({ page: 1, totalItems: 0, totalPages: 0 });

    expect(resumen()).toBe('Sin contratos');
  });

  it('en la primera pagina no permite ir atras', async () => {
    await crear({ page: 1, totalItems: 42, totalPages: 5 });

    expect(boton('Anterior').disabled).toBe(true);
    expect(boton('Siguiente').disabled).toBe(false);
  });

  it('en la ultima pagina no permite avanzar', async () => {
    await crear({ page: 5, totalItems: 42, totalPages: 5 });

    expect(boton('Siguiente').disabled).toBe(true);
  });

  it('emite la pagina elegida', async () => {
    await crear({ page: 2, totalItems: 42, totalPages: 5 });
    const emitidas: number[] = [];
    fixture.componentInstance.cambioPagina.subscribe((p) => emitidas.push(p));

    boton('Siguiente').click();
    boton('Anterior').click();

    expect(emitidas).toEqual([3, 1]);
  });

  it('emite el nuevo tamano de pagina', async () => {
    await crear({ page: 1, totalItems: 42, totalPages: 5 });
    const emitidos: number[] = [];
    fixture.componentInstance.cambioTamano.subscribe((t) => emitidos.push(t));

    const selector = raiz.querySelector<HTMLSelectElement>('select')!;
    selector.value = '20';
    selector.dispatchEvent(new Event('change'));

    expect(emitidos).toEqual([20]);
  });

  it('deshabilitado bloquea todos los controles mientras se carga', async () => {
    await crear({ page: 2, totalItems: 42, totalPages: 5, deshabilitado: true });

    expect(boton('Anterior').disabled).toBe(true);
    expect(boton('Siguiente').disabled).toBe(true);
    expect(raiz.querySelector<HTMLSelectElement>('select')!.disabled).toBe(true);
  });
});
