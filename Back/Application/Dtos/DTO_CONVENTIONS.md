# DTO Conventions

## Purpose
- Keep DTOs focused on data transport only.
- Separate API input contracts from output contracts to prevent over-posting and over-exposure.

## Naming
- Request DTOs: `*RequestDto`
- Response envelopes: `*ResponseDto`
- Read models: `*ReadModelDto`
- Stats projections: `*StatsDto`
- Lookup lists/minimal selectors: `*LookupDto`
- Detailed payloads: `*DetalleDto`
- Summary payloads: `*ResumenDto`

### Language policy
- Domain DTO names are in Spanish (`Detalle`, `Resumen`, `Alumno`, `Curso`, etc.).
- Technical suffixes remain in English (`Request`, `Response`, `ReadModel`, `Stats`, `Lookup`).
- Avoid `*SimpleDto`; use `*LookupDto` or `*ResumenDto` based on intent.

## Folder layout
- `Dtos/<Module>/Requests/`
- `Dtos/<Module>/Responses/`
- `Dtos/<Module>/Responses/Panel/` for dashboard/panel-specific payloads
- `Dtos/<Module>/ReadModels/`
- `Dtos/<Module>/Stats/`
<<<<<<< HEAD
- Shared primitives/value objects directly in `Dtos/`
- Shared response bases directly in `Dtos/`
=======
- Shared primitives/value objects in `Dtos/Common/`
- Shared response bases in `Dtos/Common/Bases/`
>>>>>>> f66dfb610f2a51b7c4a41d1cb70f9d8cf302f25a

### Optional deep grouping
- When a folder grows, group by feature/use case under `ReadModels` and `Stats`.
- Example: `Dtos/Admin/ReadModels/Matriculas/`, `Dtos/Admin/ReadModels/Imparticiones/`, `Dtos/Admin/Stats/Cursos/`.
- Keep namespaces stable (`Back.Api.Application.Dtos`) unless a broader namespace refactor is planned.

## Rules
- DTOs must not include business logic.
- DTOs should only expose serializable properties.
- Validate input DTOs with DataAnnotations when meaningful.
- Do not use the same type for request and response except very small and truly symmetric cases.

## Evolution
- Prefer additive changes to response DTOs.
- Avoid changing or reusing request DTO fields for unrelated endpoints.
- Version endpoint contracts when a breaking DTO change is required.