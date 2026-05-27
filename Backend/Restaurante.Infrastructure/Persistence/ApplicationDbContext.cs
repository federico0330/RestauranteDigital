using Microsoft.EntityFrameworkCore;
using Restaurante.Domain.Entities;

namespace Restaurante.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<DeliveryType> DeliveryTypes { get; set; } = null!;
    public DbSet<Status> Statuses { get; set; } = null!;
    public DbSet<Dish> Dishes { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Branch> Branches { get; set; } = null!;
    public DbSet<BranchDishStock> BranchDishStocks { get; set; } = null!;
    public DbSet<WholesaleOrder> WholesaleOrders { get; set; } = null!;
    public DbSet<WholesaleOrderItem> WholesaleOrderItems { get; set; } = null!;
    public DbSet<WarrantyClaim> WarrantyClaims { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Category");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasColumnType("varchar(25)").IsRequired();
            entity.Property(e => e.Description).HasColumnType("varchar(255)");
            entity.Property(e => e.Order).IsRequired();
        });

        modelBuilder.Entity<DeliveryType>(entity =>
        {
            entity.ToTable("DeliveryType");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasColumnType("nvarchar(25)").IsRequired();
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.ToTable("Status");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasColumnType("varchar(25)").IsRequired();
        });

        modelBuilder.Entity<Dish>(entity =>
        {
            entity.ToTable("Dish");
            entity.HasKey(e => e.DishId);
            entity.Property(e => e.DishId).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasColumnType("varchar(255)").IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Description).HasColumnType("varchar(MAX)");
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.Available).IsRequired();
            entity.Property(e => e.IsDeleted).HasDefaultValue(false);
            entity.Property(e => e.ImageUrl).HasColumnType("varchar(MAX)");
            entity.Property(e => e.CreateDate).HasColumnType("datetime").IsRequired();
            entity.Property(e => e.UpdateDate).HasColumnType("datetime").IsRequired();

            entity.HasOne(d => d.Category)
                .WithMany()
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Order");
            entity.HasKey(e => e.OrderId);
            entity.Property(e => e.OrderId).ValueGeneratedOnAdd();
            entity.Property(e => e.DeliveryTo).HasColumnType("varchar(255)");
            entity.Property(e => e.Notes).HasColumnType("varchar(MAX)");
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.CreateDate).HasColumnType("datetime").IsRequired();
            entity.Property(e => e.UpdateDate).HasColumnType("datetime").IsRequired();
            entity.Property(e => e.BranchId).IsRequired().HasDefaultValue(1);

            entity.HasOne(o => o.DeliveryType)
                .WithMany()
                .HasForeignKey(o => o.DeliveryTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.OverallStatus)
                .WithMany()
                .HasForeignKey(o => o.OverallStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Branch)
                .WithMany()
                .HasForeignKey(o => o.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItem");
            entity.HasKey(e => e.OrderItemId);
            entity.Property(e => e.OrderItemId).ValueGeneratedOnAdd();
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.Notes).HasColumnType("varchar(MAX)");
            entity.Property(e => e.CreateDate).HasColumnType("datetime").IsRequired();

            entity.HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(oi => oi.Dish)
                .WithMany()
                .HasForeignKey(oi => oi.DishId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(oi => oi.Status)
                .WithMany()
                .HasForeignKey(oi => oi.StatusId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Name).HasColumnType("varchar(255)").IsRequired();
            entity.Property(u => u.Email).HasColumnType("varchar(255)").IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).HasColumnType("varchar(255)").IsRequired();
            entity.Property(u => u.Role).HasColumnType("varchar(25)").IsRequired();
            entity.Property(u => u.CreateDate).HasColumnType("datetime").IsRequired();
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.ToTable("Branch");
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Id).ValueGeneratedOnAdd();
            entity.Property(b => b.Name).HasColumnType("varchar(255)").IsRequired();
            entity.Property(b => b.Address).HasColumnType("varchar(500)").IsRequired();
            entity.Property(b => b.Phone).HasColumnType("varchar(50)");
            entity.Property(b => b.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(b => b.CreateDate).HasColumnType("datetime").IsRequired();
        });

        modelBuilder.Entity<BranchDishStock>(entity =>
        {
            entity.ToTable("BranchDishStock");
            entity.HasKey(s => new { s.BranchId, s.DishId });
            entity.Property(s => s.Quantity).IsRequired().HasDefaultValue(0);
            entity.Property(s => s.MinStock).IsRequired().HasDefaultValue(0);
            entity.Property(s => s.UpdateDate).HasColumnType("datetime").IsRequired();
            entity.Ignore(s => s.IsBelowMinimum);

            entity.HasOne(s => s.Branch)
                .WithMany(b => b.Stocks)
                .HasForeignKey(s => s.BranchId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.Dish)
                .WithMany()
                .HasForeignKey(s => s.DishId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WholesaleOrder>(entity =>
        {
            entity.ToTable("WholesaleOrder");
            entity.HasKey(o => o.Id);
            entity.Property(o => o.Id).ValueGeneratedOnAdd();
            entity.Property(o => o.SupplierName).HasColumnType("varchar(255)").IsRequired();
            entity.Property(o => o.Status).HasColumnType("varchar(25)").IsRequired();
            entity.Property(o => o.TotalCost).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(o => o.Notes).HasColumnType("varchar(MAX)");
            entity.Property(o => o.CreateDate).HasColumnType("datetime").IsRequired();
            entity.Property(o => o.ApprovedAt).HasColumnType("datetime");
            entity.Property(o => o.ReceivedAt).HasColumnType("datetime");

            entity.HasOne(o => o.Branch)
                .WithMany()
                .HasForeignKey(o => o.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WholesaleOrderItem>(entity =>
        {
            entity.ToTable("WholesaleOrderItem");
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Id).ValueGeneratedOnAdd();
            entity.Property(i => i.Quantity).IsRequired();
            entity.Property(i => i.UnitCost).HasColumnType("decimal(18,2)").IsRequired();
            entity.Ignore(i => i.Subtotal);

            entity.HasOne(i => i.WholesaleOrder)
                .WithMany(o => o.Items)
                .HasForeignKey(i => i.WholesaleOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.Dish)
                .WithMany()
                .HasForeignKey(i => i.DishId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WarrantyClaim>(entity =>
        {
            entity.ToTable("WarrantyClaim");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).ValueGeneratedOnAdd();
            entity.Property(c => c.Reason).HasColumnType("varchar(1000)").IsRequired();
            entity.Property(c => c.Status).HasColumnType("varchar(25)").IsRequired();
            entity.Property(c => c.Resolution).HasColumnType("varchar(1000)");
            entity.Property(c => c.CreateDate).HasColumnType("datetime").IsRequired();
            entity.Property(c => c.ResolvedAt).HasColumnType("datetime");

            entity.HasOne(c => c.Order)
                .WithMany()
                .HasForeignKey(c => c.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.OrderItem)
                .WithMany()
                .HasForeignKey(c => c.OrderItemId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<DeliveryType>().HasData(
            new DeliveryType { Id = 1, Name = "Delivery" },
            new DeliveryType { Id = 2, Name = "Take away" },
            new DeliveryType { Id = 3, Name = "Dine in" }
        );

        modelBuilder.Entity<Status>().HasData(
            new Status { Id = 1, Name = "Pending" },
            new Status { Id = 2, Name = "In progress" },
            new Status { Id = 3, Name = "Ready" },
            new Status { Id = 4, Name = "Delivery" },
            new Status { Id = 5, Name = "Closed" }
        );

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Entradas", Description = "Pequeñas porciones para abrir el apetito antes del plato principal.", Order = 1 },
            new Category { Id = 2, Name = "Ensaladas", Description = "Opciones frescas y livianas, ideales como acompañamiento o plato principal.", Order = 2 },
            new Category { Id = 3, Name = "Minutas", Description = "Platos rápidos y clásicos de bodegón: milanesas, tortillas, revueltos.", Order = 3 },
            new Category { Id = 4, Name = "Pastas", Description = "Variedad de pastas caseras y salsas tradicionales.", Order = 5 },
            new Category { Id = 5, Name = "Parrilla", Description = "Cortes de carne asados a la parrilla, servidos con guarniciones.", Order = 4 },
            new Category { Id = 6, Name = "Pizzas", Description = "Pizzas artesanales con masa casera y variedad de ingredientes.", Order = 7 },
            new Category { Id = 7, Name = "Sandwiches", Description = "Sandwiches y lomitos completos preparados al momento.", Order = 6 },
            new Category { Id = 8, Name = "Bebidas", Description = "Gaseosas, jugos, aguas y opciones sin alcohol.", Order = 8 },
            new Category { Id = 9, Name = "Cerveza Artesanal", Description = "Cervezas de producción artesanal, rubias, rojas y negras.", Order = 9 },
            new Category { Id = 10, Name = "Postres", Description = "Clásicos dulces caseros para cerrar la comida.", Order = 10 }
        );

        modelBuilder.Entity<Branch>().HasData(
            new Branch { Id = 1, Name = "Casa Central", Address = "Av. Mitre 1200, Berazategui", Phone = "+54 11 5000-1000", IsActive = true, CreateDate = seedDate },
            new Branch { Id = 2, Name = "Sucursal Bernal", Address = "9 de Julio 450, Bernal", Phone = "+54 11 5000-2000", IsActive = true, CreateDate = seedDate },
            new Branch { Id = 3, Name = "Sucursal Quilmes Centro", Address = "Rivadavia 320, Quilmes", Phone = "+54 11 5000-3000", IsActive = true, CreateDate = seedDate }
        );

        // Admin con password "admin123" hasheado con BCrypt (workFactor 11). Bootstrap inicial
        // para que exista al menos un Admin después de aplicar la migración.
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Name = "Administrador",
                Email = "admin@restaurante.com",
                PasswordHash = "$2a$11$Dlj/.AbWqsRq8KQS6LIcmO.IkmU.q4tu5OPxNw8sjBu24/EFCxOgi",
                Role = UserRoles.Admin,
                CreateDate = seedDate
            }
        );
    }
}
