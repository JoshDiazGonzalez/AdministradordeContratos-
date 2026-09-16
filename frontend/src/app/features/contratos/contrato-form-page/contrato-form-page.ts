import { HttpErrorResponse } from '@angular/common/http';
import { Component, ElementRef, HostListener, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize, map } from 'rxjs';

import { ConCambiosSinGuardar } from '../../../core/guards/cambios-sin-guardar.guard';
import { ProblemDetails } from '../../../core/models/problem-details.model';
import { ContratosService } from '../../../core/services/contratos.service';
import { NotificacionesService } from '../../../core/services/notificaciones.service';
import { Spinner } from '../../../shared/ui/spinner/spinner';
import { formatearMonto, leerMonto } from '../../../shared/utils/monto';
import { formatearTamano } from '../../../shared/utils/tamano-archivo';
import {
  EXTENSIONES_PERMITIDAS,
  documentoValido,
  montoValido,
  rangoDeFechasValido,
  textoObligatorio,
} from './contrato-validadores';

export const LONGITUD_MAXIMA_DESCRIPCION = 1000;

type Campo = 'nombreProveedor' | 'monto' | 'fechaInicio' | 'fechaVencimiento' | 'descripcion' | 'archivo';

/** Orden de los campos en pantalla: al enviar con errores se enfoca el primero. */
const ORDEN_CAMPOS: readonly Campo[] = [
  'nombreProveedor',
  'monto',
  'fechaInicio',
  'fechaVencimiento',
  'descripcion',
  'archivo',
];

/** Nombre del campo en los errores de la API (ProblemDetails.errors) -> campo del formulario. */
const CAMPO_DE_LA_API: Readonly<Record<string, Campo>> = {
  nombreproveedor: 'nombreProveedor',
  montocontrato: 'monto',
  fechainicio: 'fechaInicio',
  fechavencimiento: 'fechaVencimiento',
  descripcion: 'descripcion',
  archivo: 'archivo',
};

@Component({
  selector: 'app-contrato-form-page',
  imports: [ReactiveFormsModule, RouterLink, Spinner],
  templateUrl: './contrato-form-page.html',
  styleUrl: './contrato-form-page.css',
})
export class ContratoFormPage implements ConCambiosSinGuardar {
  private readonly contratos = inject(ContratosService);
  private readonly notificaciones = inject(NotificacionesService);
  private readonly router = inject(Router);
  private readonly raiz = inject(ElementRef<HTMLElement>);

  protected readonly longitudMaximaDescripcion = LONGITUD_MAXIMA_DESCRIPCION;
  protected readonly extensiones = EXTENSIONES_PERMITIDAS.join(',');

  protected readonly formulario = inject(NonNullableFormBuilder).group(
    {
      nombreProveedor: ['', [textoObligatorio, Validators.maxLength(200)]],
      monto: ['', [montoValido]],
      fechaInicio: ['', [Validators.required]],
      fechaVencimiento: ['', [Validators.required]],
      descripcion: ['', [textoObligatorio, Validators.maxLength(LONGITUD_MAXIMA_DESCRIPCION)]],
      archivo: new FormControl<File | null>(null, { validators: [documentoValido] }),
    },
    { validators: [rangoDeFechasValido] },
  );

  protected readonly enviando = signal(false);
  protected readonly intentoEnviar = signal(false);
  protected readonly errorGeneral = signal<string | null>(null);
  protected readonly arrastrando = signal(false);
  private guardado = false;

  protected readonly archivo = toSignal(this.formulario.controls.archivo.valueChanges, {
    initialValue: null,
  });

  protected readonly tamanoArchivo = computed(() => {
    const archivo = this.archivo();
    return archivo ? formatearTamano(archivo.size) : '';
  });

  protected readonly caracteresDescripcion = toSignal(
    this.formulario.controls.descripcion.valueChanges.pipe(map((texto) => texto.length)),
    { initialValue: 0 },
  );

  tieneCambiosSinGuardar(): boolean {
    return this.formulario.dirty && !this.guardado;
  }

  /** Aviso del navegador al recargar o cerrar la pestana con datos escritos. */
  @HostListener('window:beforeunload', ['$event'])
  protected alSalirDelNavegador(evento: BeforeUnloadEvent): void {
    if (this.tieneCambiosSinGuardar()) {
      evento.preventDefault();
    }
  }

  /** Muestra el error de un campo solo tras tocarlo o intentar enviar. */
  protected invalido(campo: Campo): boolean {
    const control = this.formulario.controls[campo];
    const visible = control.touched || this.intentoEnviar();

    if (campo === 'fechaVencimiento') {
      return visible && (control.invalid || this.formulario.hasError('rangoFechas'));
    }

    return visible && control.invalid;
  }

  protected error(campo: Campo): string | null {
    if (!this.invalido(campo)) {
      return null;
    }

    const errores = this.formulario.controls[campo].errors ?? {};

    // Un mensaje devuelto por la API tiene prioridad: es la validacion definitiva.
    if (typeof errores['servidor'] === 'string') {
      return errores['servidor'];
    }

    switch (campo) {
      case 'nombreProveedor':
        return errores['maxlength']
          ? 'El nombre no puede superar los 200 caracteres.'
          : 'El nombre del proveedor es obligatorio.';
      case 'monto':
        if (errores['obligatorio']) return 'El monto es obligatorio.';
        if (errores['decimales']) return 'El monto admite como máximo dos decimales.';
        if (errores['formato']) return 'Escriba el monto con punto decimal, por ejemplo 15000.50 o 15,000.50.';
        if (errores['minimo']) return 'El monto debe ser mayor que 0.';
        return 'El monto supera el máximo admitido.';
      case 'fechaInicio':
        return 'La fecha de inicio es obligatoria.';
      case 'fechaVencimiento':
        return errores['required']
          ? 'La fecha de vencimiento es obligatoria.'
          : 'La fecha de vencimiento no puede ser anterior a la fecha de inicio.';
      case 'descripcion':
        return errores['maxlength']
          ? `La descripción no puede superar los ${LONGITUD_MAXIMA_DESCRIPCION} caracteres.`
          : 'La descripción es obligatoria.';
      case 'archivo':
        if (errores['extension']) return 'Solo se aceptan documentos PDF, DOC o DOCX.';
        if (errores['vacio']) return 'El documento está vacío.';
        if (errores['tamano']) return 'El documento no puede superar los 10 MB.';
        return 'Debe seleccionar un documento.';
    }
  }

  /** Al salir del campo, el monto se reescribe con formato: "15000.5" -> "15,000.50". */
  protected formatearMontoEscrito(): void {
    const control = this.formulario.controls.monto;
    control.markAsTouched();

    const lectura = leerMonto(control.value);
    if (lectura.ok && lectura.valor > 0) {
      control.setValue(formatearMonto(lectura.valor));
    }
  }

  protected alElegirArchivo(evento: Event): void {
    const entrada = evento.target as HTMLInputElement;
    this.establecerArchivo(entrada.files?.item(0) ?? null);
    // Se limpia el input para poder volver a elegir el mismo archivo tras quitarlo.
    entrada.value = '';
  }

  protected quitarArchivo(): void {
    this.establecerArchivo(null);
  }

  protected alArrastrarSobre(evento: DragEvent): void {
    evento.preventDefault();
    this.arrastrando.set(true);
  }

  protected alSalirArrastre(): void {
    this.arrastrando.set(false);
  }

  protected alSoltar(evento: DragEvent): void {
    evento.preventDefault();
    this.arrastrando.set(false);
    this.establecerArchivo(evento.dataTransfer?.files.item(0) ?? null);
  }

  protected guardar(): void {
    this.intentoEnviar.set(true);
    this.errorGeneral.set(null);

    if (this.enviando()) {
      return;
    }

    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      this.enfocarPrimerError();
      return;
    }

    const valores = this.formulario.getRawValue();
    const monto = leerMonto(valores.monto);

    // El formulario es valido, asi que estas comprobaciones no deberian fallar;
    // se mantienen para que TypeScript garantice los tipos sin aserciones.
    if (!monto.ok || !valores.archivo) {
      return;
    }

    this.enviando.set(true);

    this.contratos
      .crear({
        nombreProveedor: valores.nombreProveedor.trim(),
        montoContrato: monto.valor,
        fechaInicio: valores.fechaInicio,
        fechaVencimiento: valores.fechaVencimiento,
        descripcion: valores.descripcion.trim(),
        archivo: valores.archivo,
      })
      .pipe(finalize(() => this.enviando.set(false)))
      .subscribe({
        next: (contrato) => {
          this.guardado = true;
          this.notificaciones.exito(`Contrato de ${contrato.nombreProveedor} registrado.`);
          void this.router.navigate(['/contratos']);
        },
        error: (error: unknown) => this.mostrarErrorDelServidor(error),
      });
  }

  private establecerArchivo(archivo: File | null): void {
    const control = this.formulario.controls.archivo;
    control.setValue(archivo);
    control.markAsDirty();
    control.markAsTouched();

    // Elegir o quitar el archivo reemplaza el bloque que tenia el foco. Sin
    // moverlo, quien navega con teclado quedaria en el inicio de la pagina.
    const destino = archivo ? '.quitar' : '#contrato-archivo';
    setTimeout(() =>
      (this.raiz.nativeElement as HTMLElement).querySelector<HTMLElement>(destino)?.focus(),
    );
  }

  private mostrarErrorDelServidor(error: unknown): void {
    if (!(error instanceof HttpErrorResponse)) {
      this.errorGeneral.set('No se pudo guardar el contrato. Inténtelo de nuevo.');
      return;
    }

    switch (error.status) {
      case 0:
        this.errorGeneral.set(
          'No se pudo conectar con el servidor. Los datos siguen en el formulario: inténtelo de nuevo.',
        );
        return;
      case 401:
        // El interceptor ya lleva al login.
        return;
      case 413:
        this.asignarErrorServidor('archivo', 'El documento no puede superar los 10 MB.');
        this.enfocarPrimerError();
        return;
      case 400: {
        const problema = error.error as ProblemDetails | null;
        const noAsignados = this.asignarErroresDeValidacion(problema?.errors ?? {});

        if (noAsignados.length > 0) {
          this.errorGeneral.set(noAsignados.join(' '));
        } else if (!problema?.errors) {
          this.errorGeneral.set(problema?.detail ?? 'Revise los datos del contrato.');
        }

        this.enfocarPrimerError();
        return;
      }
      default:
        this.errorGeneral.set('No se pudo guardar el contrato. Inténtelo de nuevo en unos minutos.');
    }
  }

  /**
   * Coloca cada error de la API junto a su campo. Devuelve los mensajes que no
   * corresponden a ningun campo, para mostrarlos arriba del formulario.
   */
  private asignarErroresDeValidacion(errores: Record<string, string[]>): string[] {
    const noAsignados: string[] = [];

    for (const [clave, mensajes] of Object.entries(errores)) {
      const campo = CAMPO_DE_LA_API[clave.toLowerCase()];
      const mensaje = mensajes[0];

      if (campo && mensaje) {
        this.asignarErrorServidor(campo, mensaje);
      } else if (mensaje) {
        noAsignados.push(mensaje);
      }
    }

    return noAsignados;
  }

  /**
   * El error se guarda en el control; en cuanto el usuario cambia el valor,
   * Angular vuelve a validar y el mensaje del servidor desaparece solo.
   */
  private asignarErrorServidor(campo: Campo, mensaje: string): void {
    const control = this.formulario.controls[campo];
    control.setErrors({ ...(control.errors ?? {}), servidor: mensaje });
    control.markAsTouched();
  }

  private enfocarPrimerError(): void {
    const campo = ORDEN_CAMPOS.find((nombre) => this.invalido(nombre));
    if (!campo) {
      return;
    }

    queueMicrotask(() => {
      const elemento = (this.raiz.nativeElement as HTMLElement).querySelector<HTMLElement>(
        `#contrato-${campo}`,
      );
      elemento?.focus();
    });
  }
}
