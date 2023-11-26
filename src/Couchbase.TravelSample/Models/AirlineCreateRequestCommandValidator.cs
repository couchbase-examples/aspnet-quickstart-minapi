using FluentValidation;

namespace Couchbase.TravelSample.Models;

public class AirlineCreateRequestCommandValidator : AbstractValidator<AirlineCreateRequestCommand>
{
    public AirlineCreateRequestCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Callsign).NotEmpty();
        RuleFor(x => x.Country).NotEmpty();
    }
}