# Proyecto Inicial

Aplicacion de gestion escolar con backend en ASP.NET Core y frontend en Angular.

## Stack

- Backend: ASP.NET Core 10, Entity Framework Core, SQLite, JWT Bearer, Swagger UI.
- Frontend: Angular 21 standalone, signals, Bootstrap 5, Bootstrap Icons.

## Estructura general

```text
proyectoInicial/
├── Back/                      # API REST + logica de negocio + SQLite
└── Front/                     # Aplicacion Angular (admin, profesor, alumno)
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

### Frontend

```bash
cd Front
npm install
npm start
```

- App: `http://localhost:4200`

## Credenciales semilla

El backend asegura un administrador inicial (configurable en `Back/appsettings.json`):

- correo: `admin@prueba.com`
- contrasena: `Prueba1`

## Flujo funcional recomendado

1. Crear cursos.
2. Crear asignaturas ligadas a curso.
3. Crear profesores y estudiantes.
4. Matricular estudiantes en asignaturas de su curso.
5. Asignar imparticiones profesor-asignatura-curso.
6. Crear tareas y calificar desde profesor.
7. Consultar progreso desde alumno.
8. Cargar datos iniciales con importacion CSV desde admin en este orden: cursos, asignaturas, profesores, estudiantes, imparticiones y matriculas.

## Calidad y verificacion

```bash
cd Back
dotnet build Back.Api.csproj
```

```bash
cd Front
npx ng build
```

## Documentacion detallada

- Backend: `Back/README.md`
- Frontend: `Front/README.md`
