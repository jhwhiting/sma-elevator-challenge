using Domain.ValueObjects;

namespace Domain.DataStructures;

public record struct ButtonPressLookupResult
{
    public required ButtonPress[] CurrentFloorPresses { get; init; }
    public required ButtonPress[] LastFloorPresses { get; init; }
    public int LastFloor { get; internal set; }
}