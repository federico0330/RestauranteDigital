namespace Restaurante.Domain.Entities;

public class Branch
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreateDate { get; set; }

    public ICollection<BranchDishStock> Stocks { get; set; } = new List<BranchDishStock>();
}
