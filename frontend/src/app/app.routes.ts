import { Routes } from '@angular/router';

const TITULO_APP = 'Administración de Contratos';

/**
 * Rutas de la aplicacion. Todas las pantallas se cargan bajo demanda
 * (loadComponent), asi el login no descarga el codigo del resto.
 *
 * Los guards de autenticacion se anaden en la Fase 10 sobre la ruta del layout,
 * de modo que protegen a la vez todas las rutas hijas.
 */
export const routes: Routes = [
  {
    path: 'login',
    title: `Iniciar sesión | ${TITULO_APP}`,
    loadComponent: () =>
      import('./features/auth/login-page/login-page').then((m) => m.LoginPage),
  },
  {
    path: '',
    loadComponent: () =>
      import('./shared/layout/app-shell/app-shell').then((m) => m.AppShell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'contratos' },
      {
        path: 'dashboard',
        title: `Dashboard | ${TITULO_APP}`,
        loadComponent: () =>
          import('./features/dashboard/dashboard-page/dashboard-page').then(
            (m) => m.DashboardPage,
          ),
      },
      {
        path: 'contratos',
        title: `Contratos | ${TITULO_APP}`,
        loadComponent: () =>
          import('./features/contratos/contratos-list-page/contratos-list-page').then(
            (m) => m.ContratosListPage,
          ),
      },
      {
        // Debe ir antes de 'contratos/:id': el router evalua las rutas en orden
        // y ':id' capturaria tambien el segmento literal "nuevo".
        path: 'contratos/nuevo',
        title: `Nuevo contrato | ${TITULO_APP}`,
        loadComponent: () =>
          import('./features/contratos/contrato-form-page/contrato-form-page').then(
            (m) => m.ContratoFormPage,
          ),
      },
      // Catalogo de componentes para revisar la identidad visual, solo en desarrollo.
      // Se usa ngDevMode y no isDevMode(): el CLI reemplaza ngDevMode por false al
      // compilar en produccion, el bundler elimina la rama y el chunk ni se genera.
      // isDevMode() se evalua en ejecucion y dejaria el codigo publicado aunque
      // la ruta fuese inalcanzable.
      ...(typeof ngDevMode === 'undefined' || ngDevMode
        ? [
            {
              path: 'guia-estilos',
              title: `Guía de estilos | ${TITULO_APP}`,
              loadComponent: () =>
                import('./features/guia-estilos/guia-estilos-page/guia-estilos-page').then(
                  (m) => m.GuiaEstilosPage,
                ),
            },
          ]
        : []),
      {
        path: 'contratos/:id',
        title: `Detalle del contrato | ${TITULO_APP}`,
        loadComponent: () =>
          import('./features/contratos/contrato-detail-page/contrato-detail-page').then(
            (m) => m.ContratoDetailPage,
          ),
      },
    ],
  },
  { path: '**', redirectTo: 'contratos' },
];
