import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import {
  Contrato,
  ContratoFiltro,
  ContratoResumen,
  CrearContratoRequest,
} from '../models/contrato.model';
import { ResultadoPaginado } from '../models/paginacion.model';
import { API_URL } from './api-url.token';

/**
 * Cliente HTTP de /api/contratos.
 *
 * Solo traduce entre la API y los modelos: no contiene logica de pantalla ni
 * reglas de negocio. El token JWT lo anade un interceptor, no este servicio.
 */
@Injectable({ providedIn: 'root' })
export class ContratosService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${inject(API_URL)}/contratos`;

  listar(filtro: ContratoFiltro = {}): Observable<ResultadoPaginado<Contrato>> {
    return this.http.get<ResultadoPaginado<Contrato>>(this.baseUrl, {
      params: this.aParametros(filtro),
    });
  }

  obtener(id: string): Observable<Contrato> {
    return this.http.get<Contrato>(`${this.baseUrl}/${encodeURIComponent(id)}`);
  }

  resumen(): Observable<ContratoResumen> {
    return this.http.get<ContratoResumen>(`${this.baseUrl}/resumen`);
  }

  crear(request: CrearContratoRequest): Observable<Contrato> {
    const formulario = new FormData();
    formulario.append('NombreProveedor', request.nombreProveedor);
    // String() de un number usa siempre punto decimal, independientemente del
    // idioma del navegador. La API interpreta los numeros con cultura invariante.
    formulario.append('MontoContrato', String(request.montoContrato));
    formulario.append('FechaInicio', request.fechaInicio);
    formulario.append('FechaVencimiento', request.fechaVencimiento);
    formulario.append('Descripcion', request.descripcion);
    formulario.append('Archivo', request.archivo, request.archivo.name);

    // No se fija Content-Type: el navegador lo pone con el boundary correcto.
    return this.http.post<Contrato>(this.baseUrl, formulario);
  }

  /**
   * Descarga el documento como Blob.
   *
   * No se usa un enlace directo (<a href>) porque el endpoint exige JWT y el
   * navegador no enviaria la cabecera Authorization. El nombre del archivo se
   * toma del propio contrato (archivoNombre), no de Content-Disposition.
   */
  descargarArchivo(id: string, forzarDescarga = false): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/${encodeURIComponent(id)}/archivo`, {
      params: new HttpParams().set('download', forzarDescarga),
      responseType: 'blob',
    });
  }

  /** Omite filtros vacios para no enviar parametros sin valor a la API. */
  private aParametros(filtro: ContratoFiltro): HttpParams {
    let params = new HttpParams();

    for (const [clave, valor] of Object.entries(filtro)) {
      if (valor === undefined || valor === null) {
        continue;
      }

      const texto = String(valor).trim();
      if (texto !== '') {
        params = params.set(clave, texto);
      }
    }

    return params;
  }
}
