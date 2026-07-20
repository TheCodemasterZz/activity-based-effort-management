using EforTakip.Application.Projects.Commands.CreateProject;
using FluentValidation.TestHelper;

namespace EforTakip.Application.Tests.Projects.Commands;

public class CreateProjectCommandValidatorTests
{
    private readonly CreateProjectCommandValidator _validator = new();

    [Fact]
    public void Validate_WithEmptyName_HasValidationError()
    {
        var result = _validator.TestValidate(new CreateProjectCommand("", null));

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithValidData_HasNoValidationError()
    {
        var result = _validator.TestValidate(new CreateProjectCommand("Efor Takip Platformu", "Açıklama"));

        result.ShouldNotHaveAnyValidationErrors();
    }
}
