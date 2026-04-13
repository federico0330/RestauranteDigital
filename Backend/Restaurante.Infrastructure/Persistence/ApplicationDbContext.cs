using Microsoft.EntityFrameworkCore;
using Restaurante.Domain.Entities;

namespace Restaurante.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<DeliveryType> DeliveryTypes { get; set; } = null!;
    public DbSet<Status> Statuses { get; set; } = null!;
    public DbSet<Dish> Dishes { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<OrderItem> OrderItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Category Configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Category");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasColumnType("varchar(25)").IsRequired();
            entity.Property(e => e.Description).HasColumnType("varchar(255)");
            entity.Property(e => e.Order).IsRequired();
        });

        // DeliveryType Configuration
        modelBuilder.Entity<DeliveryType>(entity =>
        {
            entity.ToTable("DeliveryType");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasColumnType("nvarchar(25)").IsRequired();
        });

        // Status Configuration
        modelBuilder.Entity<Status>(entity =>
        {
            entity.ToTable("Status");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasColumnType("varchar(25)").IsRequired();
        });

        // Dish Configuration
        modelBuilder.Entity<Dish>(entity =>
        {
            entity.ToTable("Dish");
            entity.HasKey(e => e.DishId);
            entity.Property(e => e.DishId).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).HasColumnType("varchar(255)").IsRequired();
            entity.HasIndex(e => e.Name).IsUnique(); // As per validation: Name must be unique
            entity.Property(e => e.Description).HasColumnType("varchar(MAX)");
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.Available).IsRequired();
            entity.Property(e => e.ImageUrl).HasColumnType("varchar(MAX)");
            entity.Property(e => e.CreateDate).HasColumnType("datetime").IsRequired();
            entity.Property(e => e.UpdateDate).HasColumnType("datetime").IsRequired();

            entity.HasOne(d => d.Category)
                .WithMany()
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Order Configuration
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

            entity.HasOne(o => o.DeliveryType)
                .WithMany()
                .HasForeignKey(o => o.DeliveryTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.OverallStatus)
                .WithMany()
                .HasForeignKey(o => o.OverallStatusId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // OrderItem Configuration
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
                .OnDelete(DeleteBehavior.Restrict); // "El plato no puede ser eliminado si existe una orden que dependa de esta."

            entity.HasOne(oi => oi.Status)
                .WithMany()
                .HasForeignKey(oi => oi.StatusId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
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
    }
}
