# DTO Conventions

## Purpose
- Keep DTOs focused on data transport only.
- Separate API input contracts from output contracts to prevent over-posting and over-exposure.

## Naming
- Request DTOs: `*RequestDto`
- Response DTOs: `*ResponseDto`
- Read models: `*ReadModelDto`
- Stats projections: `*StatsDto`

## Folder layout
- `Dtos/<Module>/Requests/`
- `Dtos/<Module>/Responses/`
- `Dtos/<Module>/ReadModels/`
- `Dtos/<Module>/Stats/`
- Shared primitives/value objects in `Dtos/Common/`

## Rules
- DTOs must not include business logic.
- DTOs should only expose serializable properties.
- Validate input DTOs with DataAnnotations when meaningful.
- Do not use the same type for request and response except very small and truly symmetric cases.

## Evolution
- Prefer additive changes to response DTOs.
- Avoid changing or reusing request DTO fields for unrelated endpoints.
- Version endpoint contracts when a breaking DTO change is required.