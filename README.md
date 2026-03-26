# Proyecto Inicial

Aplicacion de gestion escolar con backend en ASP.NET Core y frontend en Angular.

## Stack

- Backend: ASP.NET Core 10, Entity Framework Core, PostgreSQL, JWT Bearer, Swagger UI.
- Frontend: Angular 21 standalone, signals, Bootstrap 5, Bootstrap Icons.
- Contenedores: Docker + Docker Compose (PostgreSQL, API y Front).

## Estructura general

```text
proyectoInicial/
├── Back/                      # API REST por capas (Application/Domain/Infrastructure/Persistence/Presentation)
├── Front/                     # Aplicacion Angular (admin, profesor, alumno)
├── Back.Tests/                # Pruebas de backend
└── docker-compose.yml         # Orquestacion local (postgres + back + front)
```

## Arranque rapido

### Backend

```bash
cd Back
dotnet restore Back.Api.csproj
dotnet run --project Back.Api.csproj
```

- API: `http://localhost:5014`
- Swagger UI: `http://localhost:5014/swagger`

Base de datos local por defecto: PostgreSQL (`Host=localhost;Port=5432;Database=schooldb;Username=postgres;Password=postgres`).

### Frontend

```bash
cd Front
npm install
npm start
```

- App: `http://localhost:4200`

## Arranque con Docker Compose

```bash
docker compose up --build
```

Servicios:

- Front: `http://localhost:4200`
- Back (Swagger): `http://localhost:5014/swagger`
- PostgreSQL: `localhost:5432`

## Credenciales semilla

El backend asegura un administrador inicial (configurable en `Back/appsettings.json`):

- correo: `admin@prueba.com`
- contrasena: `Prueba1`

## Flujo funcional recomendado

1. Crear cursos.
2. Crear asignaturas ligadas a curso.
3. Crear profesores y estudiantes.
4. Asignar imparticiones profesor-asignatura-curso.
5. Crear tareas por asignatura/curso.
6. Matricular estudiantes en asignaturas de su curso.
7. Calificar desde profesor.
8. Consultar progreso desde alumno.
9. Cargar datos iniciales con importacion CSV desde admin en este orden: cursos, asignaturas, profesores, estudiantes, imparticiones, tareas, matriculas y notas.

## Funcionalidad destacada

- Panel admin con dos areas: gestion academica e indicadores.
- Estadisticas admin por curso con selector, detalle por curso y comparacion multi-curso.
- Vistas admin de matriculas e imparticiones servidas por endpoints dedicados.
- Panel profesor con creacion de tareas, calificacion y resumen del rendimiento de sus alumnos.
- Panel alumno con medias por trimestre, nota final y detalle por tareas.
- Importacion CSV con validacion por linea, reseteo del formulario tras procesar y overlay de carga por tarjeta.

## Calidad y verificacion

```bash
cd Back
dotnet build Back.slnx
```

```bash
dotnet test Back.Tests/Back.Tests.csproj
```

```bash
cd Front
npm run build
```

## Documentacion detallada

- Backend: `Back/README.md`
- Frontend: `Front/README.md`
