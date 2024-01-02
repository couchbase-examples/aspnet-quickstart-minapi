using FluentValidation;

namespace Couchbase.TravelSample.Models;

public class AirportCreateRequestCommandValidator : AbstractValidator<AirportCreateRequestCommand>
{
    public AirportCreateRequestCommandValidator()
    {
        RuleFor(x => x.Airportname).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.Country).NotEmpty();
        RuleFor(x => x.Faa).NotEmpty();
    }
}
