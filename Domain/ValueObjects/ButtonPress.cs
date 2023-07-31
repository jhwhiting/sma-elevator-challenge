using Domain.Enums;

namespace Domain.ValueObjects;

public record struct ButtonPress : IValueObject
{
    public required int Floor { get; init; }
    public required MovementDirection MovementDirection { get; init; }
}

