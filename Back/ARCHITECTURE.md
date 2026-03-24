# Arquitectura del Back

El proyecto se ha reorganizado en 5 capas con esta correspondencia:

## Capas

### Presentation
Responsable de exponer HTTP y traducir peticiones/respuestas.

- `Presentation/Controllers/`

### Application
Responsable de casos de uso, orquestacion, validaciones, DTOs y abstracciones del nucleo.

- `Application/Services/`
- `Application/Dtos/`
- `Application/Abstractions/Repositories/`
- `Application/Abstractions/Security/`

### Domain
Responsable del modelo del negocio puro.

- `Domain/Entities/`

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

## Reglas de dependencia (Clean Architecture)

1. `Presentation` depende de `Application`.
2. `Application` depende de `Domain` y define abstracciones (`Repositories`, `Security`).
3. `Domain` no depende de ninguna otra capa.
4. `Infrastructure` implementa abstracciones de `Application`.
5. `Persistence` implementa abstracciones de `Application` y usa EF Core.
6. `Program.cs` actua como composition root para enlazar interfaces e implementaciones.

## Estado de cumplimiento

Separacion estricta aplicada:

1. Dominio sin dependencias hacia Application/Infrastructure/Persistence.
2. Contratos de repositorios y seguridad ubicados en `Application/Abstractions`.
3. Implementaciones tecnicas en `Infrastructure` y `Persistence`.
4. Capa web (`Presentation`) sin acceso directo a EF Core.
