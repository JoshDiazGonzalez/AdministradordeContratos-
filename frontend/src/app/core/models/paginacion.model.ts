/** Pagina de resultados tal como la devuelve la API (ResultadoPaginado<T>). */
export interface ResultadoPaginado<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}
