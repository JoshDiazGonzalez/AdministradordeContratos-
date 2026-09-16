import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { EMPTY, Subject, catchError, combineLatest, map, of, startWith, switchMap, tap } from 'rxjs';

import { Contrato } from '../../../core/models/contrato.model';
import { ResultadoPaginado } from '../../../core/models/paginacion.model';
import { ContratosService } from '../../../core/services/contratos.service';
import { FechaCortaPipe } from '../../../shared/pipes/fecha-corta-pipe';
import { EstadoBadge } from '../../../shared/ui/estado-badge/estado-badge';
import { Paginador } from '../../../shared/ui/paginador/paginador';
import { Spinner } from '../../../shared/ui/spinner/spinner';
import {
  CriteriosBusqueda,
  FiltroListado,
  TAMANOS_PAGINA,
  contarCriterios,
  criteriosAParametros,
  errorDeRangos,
  leerFiltroDeUrl,
} from '../contratos-filtro-url';
import { ContratosFiltros } from '../contratos-filtros/contratos-filtros';

type Resultado =
  | { ok: true; filtro: FiltroListado; pagina: ResultadoPaginado<Contrato> }
  | { ok: false; error: unknown };

@Component({
  selector: 'app-contratos-list-page',
  imports: [RouterLink, CurrencyPipe, FechaCortaPipe, EstadoBadge, Paginador, Spinner, ContratosFiltros],
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

  /** Filtro actual segun la URL. La URL es la unica fuente de verdad. */
  protected readonly filtro = toSignal(this.route.queryParamMap.pipe(map(leerFiltroDeUrl)), {
    initialValue: leerFiltroDeUrl(this.route.snapshot.queryParamMap),
  });

  protected readonly criterios = computed<CriteriosBusqueda>(() => {
    const { page: _page, pageSize: _pageSize, ...criterios } = this.filtro();
    return criterios;
  });

  protected readonly hayFiltros = computed(() => contarCriterios(this.criterios()) > 0);

  constructor() {
    combineLatest([this.route.queryParamMap, this.recargar$.pipe(startWith(undefined))])
      .pipe(
        map(([parametros]) => leerFiltroDeUrl(parametros)),
        tap(() => {
          this.cargando.set(true);
          this.error.set(null);
        }),
        // switchMap cancela la peticion anterior: si se cambia de filtro o de
        // pagina varias veces seguidas, una respuesta lenta ya abandonada no
        // puede sobrescribir la actual.
        switchMap((filtro) => {
          // Un rango invertido que llega por URL (escrito a mano) no se envia:
          // la API lo rechazaria. El panel de filtros ya muestra el motivo.
          if (errorDeRangos(filtro)) {
            this.cargando.set(false);
            this.pagina.set(null);
            return EMPTY;
          }

          return this.contratos.listar(filtro).pipe(
            map((pagina): Resultado => ({ ok: true, filtro, pagina })),
            catchError((error: unknown) => of<Resultado>({ ok: false, error })),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe((resultado) => this.aplicar(resultado));
  }

  protected aplicarCriterios(criterios: CriteriosBusqueda): void {
    // Cambiar un filtro reinicia la paginacion: la pagina 3 de la busqueda
    // anterior normalmente no existe en la nueva.
    this.navegar({ ...criteriosAParametros(criterios), page: null });
  }

  protected limpiarFiltros(): void {
    this.aplicarCriterios({});
  }

  protected irAPagina(page: number): void {
    this.navegar({ page });
  }

  protected cambiarTamano(pageSize: number): void {
    this.navegar({ page: null, pageSize });
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

    // Una pagina fuera de rango (un enlace antiguo a la pagina 9 cuando ya solo
    // hay 3) se corrige a la ultima disponible en lugar de mostrar una tabla vacia.
    if (pagina.totalPages > 0 && filtro.page > pagina.totalPages) {
      this.navegar({ page: pagina.totalPages }, true);
      return;
    }

    this.pagina.set(pagina);
  }

  private navegar(parametros: Record<string, string | number | null>, reemplazar = false): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: parametros,
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
