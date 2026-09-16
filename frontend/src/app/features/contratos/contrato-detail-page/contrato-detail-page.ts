import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, ElementRef, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { Subscription, finalize } from 'rxjs';

import { CONTRATO_ESTADO_ETIQUETAS, Contrato } from '../../../core/models/contrato.model';
import { ContratosService } from '../../../core/services/contratos.service';
import { NotificacionesService } from '../../../core/services/notificaciones.service';
import { FechaCortaPipe } from '../../../shared/pipes/fecha-corta-pipe';
import { FechaHoraPipe } from '../../../shared/pipes/fecha-hora-pipe';
import { DialogoConfirmacionService } from '../../../shared/ui/dialogo-confirmacion/dialogo-confirmacion.service';
import { EstadoBadge } from '../../../shared/ui/estado-badge/estado-badge';
import { Spinner } from '../../../shared/ui/spinner/spinner';
import { formatearTamano } from '../../../shared/utils/tamano-archivo';

type Carga =
  | { tipo: 'cargando' }
  | { tipo: 'listo' }
  | { tipo: 'no-encontrado' }
  | { tipo: 'error'; mensaje: string };

type Visor =
  | { tipo: 'cerrado' }
  | { tipo: 'cargando' }
  | { tipo: 'abierto'; url: SafeResourceUrl }
  | { tipo: 'error'; mensaje: string };

/** Nombre legible del formato a partir del tipo de contenido. */
function formatoDocumento(contentType: string): string {
  if (contentType === 'application/pdf') return 'PDF';
  if (contentType.includes('word')) return 'Word';
  return 'Documento';
}

@Component({
  selector: 'app-contrato-detail-page',
  imports: [RouterLink, CurrencyPipe, FechaCortaPipe, FechaHoraPipe, EstadoBadge, Spinner],
  templateUrl: './contrato-detail-page.html',
  styleUrl: './contrato-detail-page.css',
})
export class ContratoDetailPage {
  private readonly contratos = inject(ContratosService);
  private readonly notificaciones = inject(NotificacionesService);
  private readonly dialogo = inject(DialogoConfirmacionService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly raiz = inject(ElementRef);

  /**
   * Identificador tomado de la ruta /contratos/:id.
   * Llega como input gracias a withComponentInputBinding() en app.config.ts.
   */
  readonly id = input.required<string>();

  protected readonly contrato = signal<Contrato | null>(null);
  protected readonly carga = signal<Carga>({ tipo: 'cargando' });
  protected readonly visor = signal<Visor>({ tipo: 'cerrado' });
  protected readonly descargando = signal(false);
  protected readonly cambiandoEstado = signal(false);

  /**
   * El navegador sabe mostrar PDF dentro de la pagina. En la mayoria de moviles
   * es false: alli solo se ofrece descargar, porque un iframe quedaria en blanco.
   */
  protected readonly visorDisponible = typeof navigator !== 'undefined' && navigator.pdfViewerEnabled === true;

  protected readonly esPdf = computed(() => this.contrato()?.archivoContentType === 'application/pdf');
  protected readonly formato = computed(() => formatoDocumento(this.contrato()?.archivoContentType ?? ''));
  protected readonly tamano = computed(() => formatearTamano(this.contrato()?.archivoTamanoBytes ?? 0));

  /** URL de objeto del documento abierto. Se libera al cerrar o salir de la pantalla. */
  private urlObjeto: string | null = null;
  private peticionCarga: Subscription | null = null;

  constructor() {
    // Carga el contrato cada vez que cambia el id (navegar de un detalle a otro
    // reutiliza el componente).
    effect(() => {
      const id = this.id();
      untracked(() => this.cargar(id));
    });

    inject(DestroyRef).onDestroy(() => {
      this.peticionCarga?.unsubscribe();
      this.liberarUrl();
    });
  }

  protected reintentar(): void {
    this.cargar(this.id());
  }

  protected verDocumento(): void {
    const contrato = this.contrato();
    if (!contrato || this.visor().tipo === 'cargando') {
      return;
    }

    this.visor.set({ tipo: 'cargando' });

    this.contratos.descargarArchivo(contrato.id).subscribe({
      next: (blob) => {
        this.liberarUrl();
        // Se fuerza el tipo declarado por la API. Asi el visor del navegador
        // trata el contenido como PDF y no intenta interpretarlo de otra forma.
        this.urlObjeto = URL.createObjectURL(new Blob([blob], { type: contrato.archivoContentType }));
        this.visor.set({
          tipo: 'abierto',
          // Seguro: la URL la acaba de crear la propia aplicacion con
          // createObjectURL (esquema blob:), nunca procede de datos externos.
          url: this.sanitizer.bypassSecurityTrustResourceUrl(this.urlObjeto),
        });

        // El visor se muestra debajo de las tarjetas: se lleva a la vista para
        // que no quede oculto fuera de la pantalla tras pulsar el boton.
        setTimeout(() =>
          (this.raiz.nativeElement as HTMLElement)
            .querySelector('.visor')
            ?.scrollIntoView({ behavior: 'smooth', block: 'start' }),
        );
      },
      error: (error: unknown) =>
        this.visor.set({ tipo: 'error', mensaje: mensajeDeDocumento(error) }),
    });
  }

  protected cerrarVisor(): void {
    this.liberarUrl();
    this.visor.set({ tipo: 'cerrado' });
  }

  /**
   * Descarga con el nombre original del archivo.
   *
   * No se usa un enlace directo a la API: la descarga exige el token JWT y un
   * <a href> no lo enviaria. Se obtiene el documento con HttpClient (que anade
   * el token) y se entrega al navegador como archivo.
   */
  protected descargar(): void {
    const contrato = this.contrato();
    if (!contrato || this.descargando()) {
      return;
    }

    this.descargando.set(true);

    this.contratos
      .descargarArchivo(contrato.id, true)
      .pipe(finalize(() => this.descargando.set(false)))
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const enlace = document.createElement('a');
          enlace.href = url;
          enlace.download = contrato.archivoNombre;
          enlace.rel = 'noopener';
          document.body.appendChild(enlace);
          enlace.click();
          enlace.remove();
          // Se libera en el siguiente ciclo: algunos navegadores cancelan la
          // descarga si la URL se revoca en el mismo instante del clic.
          setTimeout(() => URL.revokeObjectURL(url));
        },
        error: (error: unknown) => this.notificaciones.error(mensajeDeDocumento(error)),
      });
  }

  protected async cambiarEstado(): Promise<void> {
    const contrato = this.contrato();
    if (!contrato || this.cambiandoEstado()) {
      return;
    }

    const desactivar = contrato.estado !== 'Inactivo';

    const confirmado = await this.dialogo.confirmar(
      desactivar
        ? {
            titulo: '¿Desactivar este contrato?',
            mensaje:
              'Quedará marcado como Inactivo aunque sus fechas sigan vigentes. Podrá reactivarlo cuando lo necesite.',
            confirmar: 'Desactivar contrato',
            peligroso: true,
          }
        : {
            titulo: '¿Reactivar este contrato?',
            mensaje: 'Su estado volverá a calcularse con las fechas de vigencia.',
            confirmar: 'Reactivar contrato',
          },
    );

    if (!confirmado) {
      return;
    }

    this.cambiandoEstado.set(true);

    this.contratos
      .cambiarEstado(contrato.id, desactivar)
      .pipe(finalize(() => this.cambiandoEstado.set(false)))
      .subscribe({
        next: (actualizado) => {
          this.contrato.set(actualizado);
          this.notificaciones.exito(
            desactivar
              ? 'Contrato desactivado.'
              : // Se informa el estado resultante: reactivar un contrato cuyas
                // fechas ya pasaron lo deja Vencido, y el usuario debe saberlo.
                `Contrato reactivado. Estado actual: ${CONTRATO_ESTADO_ETIQUETAS[actualizado.estado]}.`,
          );
        },
        error: (error: unknown) => {
          if (error instanceof HttpErrorResponse && error.status === 401) {
            return;
          }
          this.notificaciones.error('No se pudo cambiar el estado del contrato. Inténtelo de nuevo.');
        },
      });
  }

  private cargar(id: string): void {
    this.peticionCarga?.unsubscribe();
    this.cerrarVisor();
    this.carga.set({ tipo: 'cargando' });

    this.peticionCarga = this.contratos.obtener(id).subscribe({
      next: (contrato) => {
        this.contrato.set(contrato);
        this.carga.set({ tipo: 'listo' });
      },
      error: (error: unknown) => {
        this.contrato.set(null);

        if (error instanceof HttpErrorResponse && error.status === 404) {
          this.carga.set({ tipo: 'no-encontrado' });
        } else if (error instanceof HttpErrorResponse && error.status === 0) {
          this.carga.set({
            tipo: 'error',
            mensaje: 'No se pudo conectar con el servidor. Compruebe su conexión e inténtelo de nuevo.',
          });
        } else {
          this.carga.set({ tipo: 'error', mensaje: 'No se pudo cargar el contrato. Inténtelo de nuevo.' });
        }
      },
    });
  }

  /** Libera la memoria del documento: cada URL de objeto retiene el archivo entero. */
  private liberarUrl(): void {
    if (this.urlObjeto) {
      URL.revokeObjectURL(this.urlObjeto);
      this.urlObjeto = null;
    }
  }
}

function mensajeDeDocumento(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 404) {
      return 'El documento de este contrato no está disponible.';
    }
    if (error.status === 0) {
      return 'No se pudo conectar con el servidor para obtener el documento.';
    }
  }

  return 'No se pudo obtener el documento. Inténtelo de nuevo.';
}
