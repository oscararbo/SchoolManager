# Front

Cliente Angular del sistema de gestion escolar. Consume la API de `Back/` y ofrece paneles para admin, profesor y alumno.

## Stack

- Angular 21 (standalone)
- Signals
- Bootstrap 5 + Bootstrap Icons
- HttpClient + interceptors

## Docker

El frontend esta preparado para build de contenedor (`Front/Dockerfile`) y se ejecuta en `docker-compose.yml`.

```bash
docker compose up --build
```

Notas:

- El contenedor de front compila con configuracion docker (`npm run build -- --configuration docker`).
- `environment.docker.ts` usa `apiBaseUrl: '/api'` para enrutar al backend detras de nginx.

## Arranque local

```bash
cd Front
npm install
npm start
```

App en `http://localhost:4200`.

## Build y test

```bash
npm run build
```

```bash
npm test
```

## Estructura de archivos

```text
Front/
├── angular.json
├── package.json
├── tsconfig*.json
├── public/
└── src/
    ├── index.html
    ├── main.ts
    ├── styles.scss
    └── app/
        ├── app.config.ts
        ├── app.html
        ├── app.routes.ts
        ├── app.scss
        ├── app.ts
        ├── core/
        │   ├── guards/
        │   │   └── auth.guard.ts
        │   ├── interceptors/
        │   │   ├── auth.interceptor.ts
        │   │   └── error.interceptor.ts
        │   ├── services/
        │   │   ├── auth-state.service.ts
        │   │   ├── session.service.ts
        │   │   └── toast.service.ts
        │   └── validators/
        │       └── auth.validators.ts
        ├── features/
        │   ├── auth/
        │   │   ├── login/
        │   └── home/
        │       ├── admin-home/
        │       ├── alumno-home/
        │       └── profesor-home/
        ├── layouts/
        │   ├── auth-layout/
        │   └── home-layout/
        └── shared/
            ├── components/
            │   ├── logout-button/
            │   ├── session-expired-dialog/
            │   └── toasts/
            └── services/
                └── school-api.service.ts
```

## Paneles funcionales

### Admin

- CRUD de cursos, asignaturas, profesores y estudiantes.
- Matriculas e imparticiones.
- Busqueda por texto.
- Importacion CSV de cursos, asignaturas, profesores, estudiantes, imparticiones y matriculas.
- Indicador de carga "Por favor espere" durante importaciones CSV largas.

Arquitectura actual del panel admin:

- `AdminHomeComponent`: orquestador de seccion activa (`estadisticas` o `gestion`).
- `AdminStatsViewComponent`: vista y graficas de estadisticas.
- `AdminManagementViewComponent`: operaciones CRUD y tab de importacion.
- Card CSV reutilizable dentro de la vista de gestion para cada tipo de importacion (cambia titulo, descripcion y entidad).

Orden recomendado de carga CSV:

1. cursos
2. asignaturas
3. profesores
4. estudiantes
5. imparticiones
6. matriculas

### Profesor

- Selector por curso y asignatura.
- Creacion de tareas por trimestre.
- Calificacion por tarea en tarjetas.
- Resumen por alumno con detalle desplegable.

### Alumno

- Tarjetas por asignatura.
- Medias por trimestre y nota final.
- Detalle de tareas agrupadas por trimestre.

## Servicios e infraestructura

### `school-api.service.ts`

Cliente HTTP tipado que centraliza endpoints y normaliza errores.

### `auth.interceptor.ts`

- inyecta JWT
- reintenta con refresh ante `401`
- si refresh falla, activa dialogo de sesion expirada con mensaje contextual

### `error.interceptor.ts`

- muestra toast para errores distintos de `401`
- interpreta `ProblemDetails`, validaciones y errores de red

## Manejo de errores

El frontend combina:

1. validacion local
2. normalizacion en `school-api.service.ts`
3. interceptores globales para UX consistente

## Verificacion

```bash
cd Front
npx ng build
```

Tambien puedes validar localmente con:

```bash
cd Front
npm run build
```
