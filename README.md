# RestauranteDigital

[![CI](https://github.com/federico0330/RestauranteDigital/actions/workflows/ci.yml/badge.svg)](https://github.com/federico0330/RestauranteDigital/actions/workflows/ci.yml)

Sistema de gestión de restaurante **multi-sucursal**: pedidos, stock por
sucursal, pedidos mayoristas a proveedores y reclamos de garantía sobre platos
defectuosos. Backend en .NET 8 con Clean Architecture, JWT, FluentValidation
y logging estructurado con Serilog. Frontend en HTML + Vanilla JS sobre
Bootstrap 5. Stack levantable con un solo `docker compose up`.

## Por qué multi-sucursal

El modelo de un solo local no escala. Apenas hay más de una sucursal aparecen
problemas reales: el mismo plato tiene stock distinto en cada local, los
pedidos a proveedores se hacen por sucursal, y las garantías hay que
trazarlas hasta la orden y el ítem específico. El proyecto modela
explícitamente esos tres ejes (`Branch`, `WholesaleOrder`, `WarrantyClaim`)
en lugar de tratarlos como un afterthought.

## Stack y arquitectura

```
Backend/
├── Restaurante.Domain/          entidades, constantes
├── Restaurante.Application/     servicios, interfaces, DTOs, JWT, BCrypt, FluentValidation
├── Restaurante.Infrastructure/  EF Core, DbContext, repos, migrations
├── Restaurante.API/             controllers, middleware, Serilog, Swagger
└── Tests/
    └── Restaurante.Application.Tests/  xUnit + Moq + FluentAssertions

Frontend/
├── index.html                   SPA única
├── js/
│   ├── app.js                   estado, router, vistas
│   └── services/api.js          fetch wrapper + auth localStorage
└── css/
```

Dependencias entre capas: `API → Application → Domain`, y `Infrastructure →
Application + Domain`. `Application` no depende de `Infrastructure`, define
interfaces (`IUnitOfWork`, `IBranchRepository`, etc.) que `Infrastructure`
implementa. Eso permite testear servicios sin tocar EF Core ni SQL Server.

## Requisitos

- **Docker** y **Docker Compose** — única dependencia para levantar el stack.
- Para compilar o testear local: **.NET SDK 8.0**.

## Levantar el proyecto

Desde la raíz del repo:

```bash
docker compose up --build -d
```

| Servicio    | URL                                    |
|-------------|----------------------------------------|
| Frontend    | http://localhost:8080                  |
| Backend API | http://localhost:5000/api/v1           |
| Swagger UI  | http://localhost:5000/swagger          |
| SQL Server  | localhost:1433 (sa / Restaurante_P@ssw0rd!) |

Las migraciones (incluyendo el seed de 3 sucursales, categorías, estados,
tipos de entrega y el usuario admin) se aplican automáticamente al iniciar la
API.

## Credenciales seed

| Email                       | Password   | Rol   |
|-----------------------------|------------|-------|
| `admin@restaurante.com`     | `admin123` | Admin |

Para crear cuentas de cliente: `POST /api/v1/Auth/register` con
`{ "name", "email", "password" }`.

## Modelo de dominio

```
Branch 1───∗ BranchDishStock ∗───1 Dish
  │                                  │
  │                                  └──∗ OrderItem ∗───1 Order ∗───1 Branch
  │                                            │
  │                                            └──∗ WarrantyClaim
  │
  └──∗ WholesaleOrder ∗───∗ WholesaleOrderItem ∗───1 Dish

User (Admin | Manager | User)  →  Auth JWT
```

- **Branch**: sucursal del restaurante.
- **BranchDishStock**: stock de cada plato en cada sucursal, con `MinStock`
  como umbral para alertas. Composite key `(BranchId, DishId)`.
- **Dish**: el plato. Soft delete (`IsDeleted`) cuando ya tiene órdenes
  asociadas, para no romper la integridad referencial del histórico.
- **Order**: pedido del cliente. Siempre asociado a una `Branch`.
- **OrderItem**: ítem del pedido. Tiene status propio que transiciona
  independientemente.
- **WholesaleOrder**: pedido mayorista a un proveedor para abastecer una
  sucursal. Flujo: `Pending → Approved → Received`. Al pasar a `Received` se
  suma stock a la sucursal destino, en una transacción.
- **WarrantyClaim**: reclamo de garantía sobre un `OrderItem` específico.
  Flujo: `Pending → Approved | Rejected`.
- **User**: autenticación con BCrypt + JWT, roles `Admin`, `Manager`, `User`.

## Decisiones técnicas

- **Stock descontado dentro de la transacción de la orden**. Cuando se crea
  un pedido, el descuento del `BranchDishStock` ocurre en el mismo
  `Begin/Commit` que el insert del `Order`. Si alguno falla, todo rollback.
- **Evento de dominio `LowStockAlertRaised`**. Cuando un descuento deja
  stock por debajo del mínimo, se loguea como warning con Serilog. Listo
  para enganchar a una notificación (mail, Slack, webhook) sin tocar el
  servicio.
- **Soft delete sobre `Dish` solo cuando hay histórico**. Si no hay órdenes
  asociadas, hard delete. Si las hay, `IsDeleted = true` y `Available =
  false`. Las órdenes pasadas siguen mostrando el nombre del plato.
- **JWT con roles resueltos en el token**. El claim `Role` se incluye al
  generar el JWT y `[Authorize(Roles = "Admin,Manager")]` filtra a nivel
  controller. Sin lookup adicional en cada request.
- **BCrypt con `workFactor = 11`**. Balance entre seguridad y latencia de
  login. Los passwords nunca se guardan en claro.
- **FluentValidation con auto-validation**. Cada request validado antes de
  llegar al controller. Reglas declarativas, testeables.
- **Middleware global de errores**. Cualquier excepción no manejada se
  convierte a un 500 con un body JSON consistente, y el detalle del error
  se incluye solo en `Development`.
- **EF Core con migrations auto-aplicadas al boot**. El docker-compose
  arranca SQL Server, la API espera, y al primer arranque aplica
  `InitialCreate + AddMultiBranchAuth`. Sin pasos manuales.

## Endpoints principales

### Auth
- `POST /api/v1/Auth/login`
- `POST /api/v1/Auth/register`

### Dishes (catálogo de platos)
- `GET    /api/v1/Dish`
- `GET    /api/v1/Dish/{id}`
- `POST   /api/v1/Dish`
- `PUT    /api/v1/Dish/{id}`
- `DELETE /api/v1/Dish/{id}` *(soft delete si tiene órdenes)*

### Orders (pedidos)
- `GET  /api/v1/Order`
- `GET  /api/v1/Order/{id}`
- `POST /api/v1/Order` *(requiere `branchId`)*
- `PUT  /api/v1/Order/item/{orderItemId}/status/{statusId}`

### Branches (sucursales y stock)
- `GET    /api/v1/Branch`
- `POST   /api/v1/Branch` *(Admin)*
- `GET    /api/v1/Branch/{branchId}/stock`
- `PUT    /api/v1/Branch/{branchId}/stock/{dishId}` *(Admin / Manager)*
- `GET    /api/v1/Branch/stock/alerts` *(stock por debajo del mínimo en toda la red)*

### Wholesale Orders (pedidos mayoristas)
- `POST /api/v1/WholesaleOrder`
- `GET  /api/v1/WholesaleOrder/{id}`
- `GET  /api/v1/WholesaleOrder/branch/{branchId}?status=...`
- `POST /api/v1/WholesaleOrder/{id}/approve`
- `POST /api/v1/WholesaleOrder/{id}/receive` *(suma stock a la sucursal)*
- `POST /api/v1/WholesaleOrder/{id}/cancel`

### Warranty (garantías)
- `POST /api/v1/Warranty`
- `GET  /api/v1/Warranty/{id}`
- `GET  /api/v1/Warranty?status=Pending` *(Admin / Manager)*
- `POST /api/v1/Warranty/{id}/resolve` *(Admin / Manager)*

Swagger UI lista el contrato completo con autenticación JWT integrada
("Authorize" arriba a la derecha).

## Tests

26 tests con xUnit + Moq + FluentAssertions sobre los servicios críticos:
auth, soft delete de platos, descuento de stock por sucursal, transición de
estados de pedidos mayoristas, resolución de reclamos de garantía.

```bash
cd Backend
dotnet test
```

CI: `dotnet restore + build + test` en cada push y PR a `main`.

## Flujo end-to-end para probar multi-sucursal

1. Login como admin en `http://localhost:8080`.
2. Crear platos en "Gestionar Platos".
3. En Swagger:
   - `PUT /api/v1/Branch/1/stock/{dishId}` con `{ "quantity": 50, "minStock": 10 }`.
   - `PUT /api/v1/Branch/2/stock/{dishId}` con `{ "quantity": 5, "minStock": 10 }`.
4. Volver al frontend, agregar el plato al carrito, elegir "Sucursal Bernal"
   (id=2), confirmar el pedido.
5. En Swagger: `GET /api/v1/Branch/2/stock` — el stock bajó.
   `GET /api/v1/Branch/stock/alerts` — la sucursal 2 aparece con alerta.
6. `POST /api/v1/WholesaleOrder` para la sucursal 2; `approve` → `receive`.
   El stock vuelve a estar por encima del mínimo.
7. `POST /api/v1/Warranty` referenciando una orden y un ítem; resolverlo
   con `/resolve`.
