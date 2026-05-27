# Devolución técnica — RestauranteDigital

Qué capacidades demuestra este proyecto y cómo se traducen al dominio de
retail multi-sucursal.

## Lo que el proyecto modela explícitamente

El sistema no es un CRUD de platos: modela las tres cosas más dolorosas de
operar más de una sucursal:

1. **Stock por sucursal** (`BranchDishStock`) con umbral mínimo configurable
   por SKU, descuento transaccional al crear una orden, y alerta cuando una
   sucursal cae por debajo del mínimo.
2. **Pedidos mayoristas a proveedores** (`WholesaleOrder`) con flujo
   `Pending → Approved → Received`. Al recibirse, suma stock a la sucursal
   destino dentro de una transacción.
3. **Garantías sobre ítems específicos** (`WarrantyClaim`) trazables hasta
   la orden y el ítem original, con flujo `Pending → Approved | Rejected` y
   resolución guardada.

Cada una de esas piezas tiene su entidad, su servicio, su controller con
permisos por rol y sus tests.

## Cómo se traduce a retail textil

Cambiando el dominio de "plato" por "producto con talle y color" y poco
más, todo lo modelado aplica 1:1:

| Concepto en el proyecto                    | Equivalente en retail textil multi-sucursal                                                |
|--------------------------------------------|--------------------------------------------------------------------------------------------|
| `Branch` (sucursal con dirección y phone)  | Cada uno de los 175+ puntos de venta                                                       |
| `Dish` con `IsDeleted` (soft)              | SKU del producto, soft delete cuando hay órdenes históricas — sirve para discontinuados   |
| `BranchDishStock(BranchId, DishId, Qty, MinStock)` | `BranchSkuStock(BranchId, SkuId, Qty, MinStock)` — la PK compuesta es la misma           |
| Descuento de stock dentro de la transacción de la orden | Misma garantía de consistencia al cobrar en caja: la venta y el descuento son atómicos    |
| `LowStockAlertRaised` loggeado con Serilog | Reabastecimiento automático: el listener manda un mail o crea un draft de pedido mayorista |
| `WholesaleOrder` con `BranchId` destino    | Pedido a fábrica/proveedor para reponer una sucursal puntual                                |
| Estado `Received` que suma al stock         | Recepción de mercadería en sucursal, el sistema actualiza el inventario sin doble carga    |
| `WarrantyClaim` sobre `OrderItem` específico | Reclamo por prenda con falla, trazable hasta la venta original y el SKU exacto             |
| `OrderItem.Status` independiente por ítem  | Estado por unidad: una prenda devuelta no afecta el estado del resto de la orden           |
| Roles `Admin`, `Manager`, `User` en el JWT | Permisos diferenciados: dueño, gerentes de zona, vendedores                                |

La estructura de las tablas y servicios es portable. Lo único que cambia
para retail textil son las columnas específicas del producto (talle, color,
temporada, código de barra) y la integración con un POS o e-commerce
existente para los inputs de venta.

## Capacidades técnicas que demuestra

**Clean Architecture funcional**, no decorativa. Las 4 capas existen con
sus responsabilidades y dependencias correctas. Los servicios se testean
con mocks porque las interfaces de repo están en `Application`, no en
`Infrastructure`.

**Transacciones explícitas para operaciones multi-entidad**. El
`IUnitOfWork` expone `BeginTransactionAsync`, `CommitTransactionAsync` y
`RollbackTransactionAsync`. La creación de una orden (que toca `Order`,
`OrderItem` y `BranchDishStock` de varias filas) corre dentro de una sola
transacción. Si algo falla, rollback completo.

**Eventos de dominio observables sin coupling**. Cuando el stock cae bajo
el mínimo, se emite un warning estructurado con Serilog incluyendo
`branch`, `dish`, `qty`, `min`. Listo para enganchar a un sink remoto
(Seq, Elastic, Datadog) o a un job que dispare notificaciones, sin que el
servicio sepa quién consume el evento.

**Soft delete inteligente**, no automático. Solo se hace soft delete
cuando hay órdenes históricas que perderían la referencia al plato.
Cuando no hay órdenes, hard delete normal — no acumulamos basura.

**Seguridad razonable de fábrica**. Passwords con BCrypt
(`workFactor = 11`), JWT con lifetime corto (12h), roles validados a
nivel controller con `[Authorize(Roles = ...)]`, validación de inputs con
FluentValidation antes de tocar el servicio.

**Logging estructurado con Serilog**. No `Console.WriteLine`, no
`ILogger` sin estructura. Cada evento crítico tiene un mensaje template
con sus placeholders y se puede consultar después con queries.

**Migrations EF Core auto-aplicadas al arranque**. Sin pasos manuales,
sin `update-database` mental cada vez. Cuando el container arranca, la DB
queda en el último estado y con el seed cargado.

**Suite de tests pragmática**. 26 tests sobre los caminos de negocio
críticos (auth, soft delete, descuento de stock, transiciones de pedidos
mayoristas, resolución de garantías). No es 100% coverage; es la
cobertura que vale la pena defender en code review.

## Stack y por qué

- **.NET 8 + EF Core 8** — performance, tooling maduro, soporte LTS hasta
  2026. Mismo stack que el otro proyecto de showcase: refuerza dominio.
- **SQL Server** — transacciones serias, comportamiento predecible bajo
  concurrencia, support de migrations EF Core sin sorpresas.
- **Vanilla JS + Bootstrap** en frontend — el scope del proyecto no
  justifica React/Vue. Se sirve estático con Nginx, sin build step.
- **BCrypt + JWT** en lugar de Identity completo — Identity es demasiado
  pesado para 1 entidad `User` con 3 roles. Lo que se gana en simpleza
  vale más que las pocas features que se pierden.

## Lo que pulí en esta entrega

Sobre la base inicial (CRUD de platos + pedidos, sin auth, sin stock por
sucursal, sin tests, sin README):

- Auth real con BCrypt + JWT, persistencia frontend con localStorage.
- 6 entidades nuevas (`User`, `Branch`, `BranchDishStock`,
  `WholesaleOrder`, `WholesaleOrderItem`, `WarrantyClaim`).
- 4 servicios nuevos (`AuthService`, `BranchService`,
  `WholesaleOrderService`, `WarrantyService`).
- 4 controllers nuevos con permisos por rol.
- `OrderService` extendido: descuenta stock por sucursal, emite alerta
  de stock bajo.
- `DishService` con soft delete cuando hay histórico.
- FluentValidation para los requests críticos.
- Middleware global de manejo de errores.
- Serilog para logging estructurado.
- 26 tests xUnit + Moq.
- Migration `AddMultiBranchAuth` con seed de sucursales y admin.
- Workflow CI con build + test.
- README, CLAUDE.md y este documento.
