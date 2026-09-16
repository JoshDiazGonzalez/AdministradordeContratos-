import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { AppShell, seccionDeUrl } from './app-shell';

@Component({ template: '' })
class PaginaVacia {}

describe('seccionDeUrl', () => {
  it.each([
    ['/contratos', 'contratos'],
    ['/contratos/nuevo', 'nuevo'],
    ['/contratos/01a0a852-e787-7335', 'contratos'],
    ['/contratos?estado=Vencido&page=2', 'contratos'],
    ['/contratos/nuevo?origen=dashboard', 'nuevo'],
    ['/dashboard', 'dashboard'],
    ['/login', null],
    ['/contratosx', null],
  ] as const)('%s pertenece a la seccion %s', (url, seccion) => {
    expect(seccionDeUrl(url)).toBe(seccion);
  });
});

describe('AppShell', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          {
            path: '',
            component: AppShell,
            children: [
              { path: 'dashboard', component: PaginaVacia },
              { path: 'contratos', component: PaginaVacia },
              { path: 'contratos/nuevo', component: PaginaVacia },
              { path: 'contratos/:id', component: PaginaVacia },
            ],
          },
        ]),
      ],
    });
  });

  async function crear(url: string): Promise<{ harness: RouterTestingHarness; raiz: HTMLElement }> {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl(url);
    harness.detectChanges();
    return { harness, raiz: harness.routeNativeElement as HTMLElement };
  }

  async function navegar(url: string): Promise<HTMLElement> {
    return (await crear(url)).raiz;
  }

  function enlacesActivos(raiz: HTMLElement): string[] {
    return [...raiz.querySelectorAll('nav a[aria-current="page"]')].map(
      (a) => a.textContent?.trim() ?? '',
    );
  }

  it('en /contratos/nuevo solo marca "Nuevo contrato"', async () => {
    // Regresion: con coincidencia por prefijo tambien se marcaba "Contratos".
    expect(enlacesActivos(await navegar('/contratos/nuevo'))).toEqual(['Nuevo contrato']);
  });

  it('el detalle de un contrato mantiene marcada la seccion Contratos', async () => {
    expect(enlacesActivos(await navegar('/contratos/abc-123'))).toEqual(['Contratos']);
  });

  it('el boton de menu abre y cierra la navegacion y lo anuncia', async () => {
    const { harness, raiz } = await crear('/contratos');
    const boton = raiz.querySelector<HTMLButtonElement>('.boton-menu')!;
    const nav = raiz.querySelector('nav')!;

    expect(boton.getAttribute('aria-expanded')).toBe('false');
    expect(nav.classList.contains('abierto')).toBe(false);

    boton.click();
    harness.detectChanges();

    expect(boton.getAttribute('aria-expanded')).toBe('true');
    expect(nav.classList.contains('abierto')).toBe(true);

    boton.click();
    harness.detectChanges();

    expect(boton.getAttribute('aria-expanded')).toBe('false');
    expect(nav.classList.contains('abierto')).toBe(false);
  });

  it('al navegar se cierra el menu movil', async () => {
    const { harness, raiz } = await crear('/contratos');
    raiz.querySelector<HTMLButtonElement>('.boton-menu')!.click();
    harness.detectChanges();

    await harness.navigateByUrl('/dashboard');
    harness.detectChanges();

    expect(raiz.querySelector('nav')!.classList.contains('abierto')).toBe(false);
  });

  it('usa el favicon proporcionado como marca, con texto alternativo vacio', async () => {
    const imagen = (await navegar('/contratos')).querySelector('.marca img');

    expect(imagen?.getAttribute('src')).toBe('favicon.ico');
    // Es decorativa: el nombre del sistema ya esta escrito al lado.
    expect(imagen?.getAttribute('alt')).toBe('');
  });
});
