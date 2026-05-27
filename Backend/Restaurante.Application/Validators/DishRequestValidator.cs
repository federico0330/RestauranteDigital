using FluentValidation;
using Restaurante.Application.DTOs;

namespace Restaurante.Application.Validators;

public class DishRequestValidator : AbstractValidator<DishRequest>
{
    public DishRequestValidator()
    {
        RuleFor(d => d.Name).NotEmpty().MaximumLength(255);
        RuleFor(d => d.Price).GreaterThan(0).WithMessage("El precio debe ser mayor a cero.");
        RuleFor(d => d.Category).GreaterThan(0);
        RuleFor(d => d.Description).MaximumLength(2000).When(d => d.Description is not null);
    }
}

public class DishUpdateRequestValidator : AbstractValidator<DishUpdateRequest>
{
    public DishUpdateRequestValidator()
    {
        Include(new DishRequestValidator());
    }
}
