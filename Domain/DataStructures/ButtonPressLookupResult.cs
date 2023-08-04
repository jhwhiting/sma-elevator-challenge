using Domain.ValueObjects;

namespace Domain.DataStructures;

public readonly record struct ButtonPressLookupResult
{
    public required ButtonPress[] CurrentFloorPresses { get; init; }
    public required ButtonPress[] LastFloorPresses { get; init; }
    public required int CurrentFloor { get; init; }
    public required int LastFloor { get; init; }
}