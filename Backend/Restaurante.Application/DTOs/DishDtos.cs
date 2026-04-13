using System;

namespace Restaurante.Application.DTOs;

public class GenericResponse
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public class DishRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Category { get; set; }
    public string? Image { get; set; }
}

public class DishUpdateRequest : DishRequest
{
    public bool IsActive { get; set; }
}

public class DishResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public GenericResponse Category { get; set; } = null!;
    public string? Image { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ApiError
{
    public string? Message { get; set; }
}
