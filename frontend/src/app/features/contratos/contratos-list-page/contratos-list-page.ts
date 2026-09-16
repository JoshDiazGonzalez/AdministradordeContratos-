import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, ParamMap, Router, RouterLink } from '@angular/router';
import { Subject, catchError, combineLatest, map, of, startWith, switchMap, tap } from 'rxjs';

import { Contrato, ContratoFiltro } from '../../../core/models/contrato.model';
import { ResultadoPaginado } from '../../../core/models/paginacion.model';
import { ContratosService } from '../../../core/services/contratos.service';
import { FechaCortaPipe } from '../../../shared/pipes/fecha-corta-pipe';
import { EstadoBadge } from '../../../shared/ui/estado-badge/estado-badge';
import { Paginador } from '../../../shared/ui/paginador/paginador';
import { Spinner } from '../../../shared/ui/spinner/spinner';

export const TAMANOS_PAGINA = [10, 20, 50] as const;
const TAMANO_POR_DEFECTO = 10;

/**
 * Lee la paginacion de la URL tolerando valores manipulados a mano
 * (?page=abc, ?page=-3, ?pageSize=9999): nunca deben llegar a la API.
 */
export function leerPaginacion(parametros: ParamMap): Required<Pick<ContratoFiltro, 'page' | 'pageSize'>> {
  const page = Number(parametros.get('page'));
  const pageSize = Number(parametros.get('pageSize'));

  return {
    page: Number.isInteger(page) && page >= 1 ? page : 1,
    pageSize: (TAMANOS_PAGINA as readonly number[]).includes(pageSize) ? pageSize : TAMANO_POR_DEFECTO,
  };
}

type Resultado =
  | { ok: true; filtro: ContratoFiltro; pagina: ResultadoPaginado<Contrato> }
  | { ok: false; error: unknown };

@Component({
  selector: 'app-contratos-list-page',
  imports: [RouterLink, CurrencyPipe, FechaCortaPipe, EstadoBadge, Paginador, Spinner],
  templateUrl: './contratos-list-page.html',
  styleUrl: './contratos-list-page.css',
})
export class ContratosListPage {
  private readonly contratos = inject(ContratosService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  private readonly recargar$ = new Subject<void>();

  protected readonly tamanosPagina = TAMANOS_PAGINA;
  protected readonly cargando = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly pagina = signal<ResultadoPaginado<Contrato> | null>(null);

  constructor() {
    combineLatest([this.route.queryParamMap, this.recargar$.pipe(startWith(undefined))])
      .pipe(
        map(([parametros]) => leerPaginacion(parametros)),
        tap(() => {
          this.cargando.set(true);
          this.error.set(null);
        }),
        // switchMap cancela la peticion anterior: si se pulsa "Siguiente" varias
        // veces seguidas, una respuesta lenta de una pagina ya abandonada no
        // puede sobrescribir la de la pagina actual.
        switchMap((filtro) =>
          this.contratos.listar(filtro).pipe(
            map((pagina): Resultado => ({ ok: true, filtro, pagina })),
            catchError((error: unknown) => of<Resultado>({ ok: false, error })),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((resultado) => this.aplicar(resultado));
  }

  protected irAPagina(page: number): void {
    this.navegar({ page });
  }

  protected cambiarTamano(pageSize: number): void {
    // Con otro tamano la pagina actual deja de tener sentido: se vuelve a la 1.
    this.navegar({ page: 1, pageSize });
  }

  protected reintentar(): void {
    this.recargar$.next();
  }

  private aplicar(resultado: Resultado): void {
    this.cargando.set(false);

    if (!resultado.ok) {
      // Un 401 ya lo gestiona el interceptor llevando al login.
      if (!(resultado.error instanceof HttpErrorResponse && resultado.error.status === 401)) {
        this.error.set(mensajeDeError(resultado.error));
      }
      return;
    }

    const { filtro, pagina } = resultado;

    // Una pagina fuera de rango (por ejemplo, un enlace antiguo a la pagina 9
    // cuando ya solo hay 3) se corrige a la ultima disponible en lugar de
    // mostrar una tabla vacia que parezca que no hay contratos.
    if (pagina.totalPages > 0 && (filtro.page ?? 1) > pagina.totalPages) {
      this.navegar({ page: pagina.totalPages }, true);
      return;
    }

    this.pagina.set(pagina);
  }

  private navegar(cambios: Partial<ContratoFiltro>, reemplazar = false): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: cambios,
      queryParamsHandling: 'merge',
      replaceUrl: reemplazar,
    });
  }
}

function mensajeDeError(error: unknown): string {
  if (error instanceof HttpErrorResponse && error.status === 0) {
    return 'No se pudo conectar con el servidor. Compruebe su conexión e inténtelo de nuevo.';
  }

  return 'No se pudieron cargar los contratos. Inténtelo de nuevo.';
}
