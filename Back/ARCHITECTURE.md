# Arquitectura del Back

El proyecto se ha reorganizado en 5 capas con esta correspondencia:

## Capas

### Presentation
Responsable de exponer HTTP y traducir peticiones/respuestas.

- `Presentation/Controllers/`

### Application
Responsable de casos de uso, orquestacion, validaciones y DTOs.

- `Application/Services/`
- `Application/Dtos/`

### Domain
Responsable del modelo del negocio y contratos estables.

- `Domain/Entities/`
- `Domain/Repositories/`

### Infrastructure
Responsable de componentes tecnicos transversales.

- `Infrastructure/ErrorHandling/`
- `Infrastructure/Security/`

### Persistence
Responsable del acceso a base de datos y EF Core.

- `Persistence/Context/`
- `Persistence/Repositories/`

## Estado actual

La separacion fisica por capas esta aplicada.

El codigo legado previo en `Controllers/` y `Services/` se ha eliminado, y el proyecto compila directamente con las capas actuales (`Presentation`, `Application`, `Domain`, `Infrastructure`, `Persistence`) sin exclusiones especiales en `Back.Api.csproj`.

## Puntos pendientes para una separacion estricta

1. Revisar y alinear los namespaces historicos para que reflejen al 100% las capas fisicas actuales.

## Siguiente fase recomendada

1. Crear un `ApplicationResult<T>` o usar un Result Pattern.
2. Hacer que los controladores traduzcan ese resultado a HTTP.
3. Extraer la logica de importacion CSV a Application.
4. Renombrar namespaces para que reflejen las capas nuevas.
