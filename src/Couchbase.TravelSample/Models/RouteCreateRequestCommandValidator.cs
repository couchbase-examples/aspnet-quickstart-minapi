using FluentValidation;

namespace Couchbase.TravelSample.Models;

public class RouteCreateRequestCommandValidator : AbstractValidator<RouteCreateRequestCommand>
{
    public RouteCreateRequestCommandValidator()
    {
        RuleFor(x => x.Airline).NotEmpty();
        RuleFor(x => x.AirlineId).NotEmpty();
        RuleFor(x => x.SourceAirport).NotEmpty();
        RuleFor(x => x.DestinationAirport).NotEmpty();
    }
}