# CLAUDE.md — RestauranteDigital

Guía de trabajo para mantener y extender el proyecto.

## Stack

- .NET 8, EF Core 8, SQL Server 2022
- BCrypt.Net-Next, JWT Bearer, FluentValidation, Serilog
- xUnit + Moq + FluentAssertions
- HTML + Vanilla JS (ES modules) + Bootstrap 5
- Docker Compose para levantar el stack completo

## Capas y dónde va cada cosa

| Capa                              | Qué vive acá                                                                                |
|-----------------------------------|---------------------------------------------------------------------------------------------|
| `Restaurante.Domain`              | Entidades, constantes de status, `UserRoles`                                                |
| `Restaurante.Application`         | Servicios, interfaces de repos y servicios, DTOs, BCrypt hasher, JWT service, FluentValidation |
| `Restaurante.Infrastructure`      | DbContext, implementaciones EF Core de los repos, `UnitOfWork`, migrations                  |
| `Restaurante.API`                 | Controllers REST, middleware de errores, Serilog, Swagger, DI de todo                       |

**Regla**: `Application` no depende de `Infrastructure`. Si un servicio
necesita persistencia, va a través de `IUnitOfWork` que se inyecta.

## Cómo agregar una entidad nueva

1. **Entidad** en `Restaurante.Domain/Entities/`.
2. **Interface del repo** en `Restaurante.Domain/Interfaces/`.
3. **Implementación del repo** en `Restaurante.Infrastructure/Repositories/`,
   heredando de `GenericRepository<T>`.
4. **Registrar el repo** en `UnitOfWork` (campo, propiedad lazy, init).
5. **Configuración EF** en `ApplicationDbContext.OnModelCreating`, agregar
   `DbSet<T>` arriba.
6. **Migration**: `dotnet ef migrations add NombreMigracion --project Restaurante.Infrastructure --startup-project Restaurante.API`.
7. **DTOs** en `Restaurante.Application/DTOs/`.
8. **Interface del servicio** en `Restaurante.Application/Interfaces/`.
9. **Implementación del servicio** en `Restaurante.Application/Services/`.
10. **Validator** (si aplica) en `Restaurante.Application/Validators/`.
11. **Controller** en `Restaurante.API/Controllers/`, con `[Authorize]` y
    roles si aplica.
12. **DI** en `Program.cs`: `AddScoped<IService, Service>()`.
13. **Test** en `Tests/Restaurante.Application.Tests/Services/`.

## Convenciones

- IDs de `Dish` son `Guid` (los usa la PK), generados por la DB.
- IDs de `Order`, `OrderItem`, `WholesaleOrder`, `WholesaleOrderItem`,
  `WarrantyClaim` son `long`.
- IDs de `Category`, `Status`, `DeliveryType`, `Branch`, `User` son `int`.
- `BranchDishStock` tiene composite key `(BranchId, DishId)`.
- Strings de status (`WholesaleOrderStatus.Pending`, etc.) son constantes
  en `Domain/Entities/`, no enums.
- Operaciones que modifican estado y tocan más de una entidad pasan por
  `BeginTransactionAsync` / `CommitTransactionAsync` /
  `RollbackTransactionAsync` del `IUnitOfWork`.
- Passwords con BCrypt (`workFactor = 11`).
- JWT con claims `NameIdentifier`, `Email`, `Role`. Lifetime 12h.

## Testing

- xUnit + Moq + FluentAssertions.
- Mockear todas las dependencias del servicio bajo prueba.
- Usar `NullLogger<T>.Instance` para `ILogger<T>`.
- No usar `InMemoryDatabase` de EF Core — son tests unitarios.
- Path: `Tests/Restaurante.Application.Tests/Services/<NombreServicio>Tests.cs`.

## Migrations

Generar (vía docker si no hay SDK local):

```bash
docker run --rm -v "$PWD/Backend":/work -w /work mcr.microsoft.com/dotnet/sdk:8.0 \
  bash -c "dotnet tool install --global dotnet-ef --version 8.0.4 && \
           export PATH=\"\$PATH:/root/.dotnet/tools\" && \
           dotnet ef migrations add NombreMigracion \
             --project Restaurante.Infrastructure \
             --startup-project Restaurante.API"
```

Se aplican automáticamente al arrancar la API (`context.Database.Migrate()`
en `Program.cs`).

## CI

`.github/workflows/ci.yml` corre `dotnet restore + build + test` en cada
push y PR a `main`.

## Levantar local

```bash
docker compose up --build -d
# Frontend: http://localhost:8080
# API:      http://localhost:5000/api/v1
# Swagger:  http://localhost:5000/swagger
```

## Seed inicial (lo aplica la migration AddMultiBranchAuth)

- 3 sucursales: Casa Central, Sucursal Bernal, Sucursal Quilmes Centro
- 1 admin: `admin@restaurante.com` / `admin123`
- 10 categorías, 5 estados, 3 tipos de entrega
