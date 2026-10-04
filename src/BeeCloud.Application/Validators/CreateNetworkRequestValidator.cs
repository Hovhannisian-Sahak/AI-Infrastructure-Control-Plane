using BeeCloud.Application.DTOs.Networks;
using FluentValidation;

namespace BeeCloud.Application.Validators;

public class CreateNetworkRequestValidator
    : AbstractValidator<CreateNetworkRequest>
{
    public CreateNetworkRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => x.Description is not null);
    }
}