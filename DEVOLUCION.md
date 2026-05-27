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

Las 4 capas de Clean Architecture están separadas en serio: las interfaces
de los repositorios viven en `Application` y `Infrastructure` las
implementa, así que los servicios se pueden testear con mocks sin levantar
EF Core ni SQL Server. No es decoración, es lo que hace que los 26 tests
corran en 37 ms.

Las operaciones que tocan más de una entidad pasan por
`BeginTransactionAsync` / `CommitTransactionAsync` /
`RollbackTransactionAsync` del `IUnitOfWork`. Crear una orden inserta el
`Order`, los `OrderItem` y descuenta de varias filas de `BranchDishStock`
dentro de la misma transacción. Si una de esas operaciones falla, vuelve
todo atrás.

Cuando el descuento deja una sucursal por debajo del mínimo, el
`OrderService` emite un warning con Serilog incluyendo `branch`, `dish`,
`qty` y `min`. Es un evento de dominio loggeado, no acoplado a una
notificación particular. Para mandarlo a un mail, Slack o un sink remoto
(Seq, Elastic, Datadog) alcanza con configurar el sink — el servicio no se
entera.

El soft delete sobre `Dish` no es automático. Si el plato nunca tuvo
órdenes, hard delete y listo. Si las tuvo, `IsDeleted = true` y
`Available = false`, para que las órdenes históricas sigan resolviendo el
nombre del plato. No acumulamos basura por defecto.

Sobre seguridad: passwords con BCrypt (`workFactor = 11`), JWT con
lifetime de 12h, roles validados a nivel controller con
`[Authorize(Roles = ...)]`, validación de inputs con FluentValidation
antes de que el request llegue al servicio.

El logging es Serilog estructurado, no `Console.WriteLine` ni `ILogger`
plano. Los mensajes están templateados, así que después se pueden filtrar
por `branch=2` o `dish={guid}` en cualquier sink que entienda JSON.

Las migrations EF Core se aplican solas al arrancar el container. No hay
que acordarse de correr `update-database` antes de cada deploy.

26 tests con xUnit + Moq sobre los caminos de negocio que importan: auth,
soft delete, descuento de stock por sucursal, transiciones de pedidos
mayoristas, resolución de garantías. No busqué 100% de coverage; busqué
los tests que vale la pena defender en una review.

## Stack y por qué

.NET 8 con EF Core 8 porque el tooling está maduro y el soporte LTS llega
hasta 2026. Es el mismo stack que uso en `TicketingSystem`, así que
refuerza dominio del entorno en vez de dispersarlo.

SQL Server porque las transacciones se comportan como uno espera y la
historia de EF Core sobre SQL Server no tiene sorpresas raras bajo
concurrencia.

Frontend vanilla con Bootstrap. El scope no justifica meterse con React o
Vue: se sirve estático con Nginx, no hay build step, y el HTML es
auditable a ojo.

BCrypt + JWT directo en vez de ASP.NET Identity. Para una sola entidad
`User` con 3 roles, Identity trae mucho más de lo que necesito. Lo que se
gana en simpleza vale más que las pocas features que se pierden.

## Lo que pulí en esta entrega

La base original era un CRUD de platos y pedidos sin auth, sin stock por
sucursal, sin tests y sin README. Sobre eso agregué:

- Auth real con BCrypt y JWT, persistencia del token en localStorage.
- 6 entidades nuevas: `User`, `Branch`, `BranchDishStock`,
  `WholesaleOrder`, `WholesaleOrderItem`, `WarrantyClaim`.
- 4 servicios y 4 controllers nuevos, con permisos por rol donde aplica.
- `OrderService` descontando stock por sucursal y emitiendo alerta cuando
  cae bajo el mínimo.
- `DishService` con soft delete cuando hay órdenes históricas asociadas.
- FluentValidation, middleware global de errores, Serilog estructurado.
- 26 tests xUnit + Moq.
- Migration `AddMultiBranchAuth` con seed de 3 sucursales y un admin.
- Workflow CI con build y test.
- README extendido, CLAUDE.md y este documento.
