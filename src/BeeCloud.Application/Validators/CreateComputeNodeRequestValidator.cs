using BeeCloud.Application.DTOs.ComputeNodes;
using FluentValidation;

namespace BeeCloud.Application.Validators;

public class CreateComputeNodeRequestValidator
    : AbstractValidator<CreateComputeNodeRequest>
{
    public CreateComputeNodeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.GpuModel)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.GpuCount)
            .GreaterThan(0);
    }
}