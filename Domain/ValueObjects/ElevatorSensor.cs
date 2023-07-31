using Domain.Enums;

namespace Domain.ValueObjects;

public record struct ElevatorSensorData : IValueObject
{
    public required MovementDirection MovementDirection { get; init; }

    public required MovementState MovementState { get; init; }

    public required ElevatorEvent ElevatorEvent { get; init; }

    public required int CurrentFloor { get; init; }

    public required int NextFloor { get; init; }

    public required int CurrentWeight { get; init; }

    public required int MaxWeight { get; init; }

    public bool WeightLimitReached => CurrentWeight >= MaxWeight;

    public ElevatorSensorData FromWeight(int weight, ElevatorEvent elevatorEvent)
    {
        return new ElevatorSensorData
        {
            CurrentFloor = CurrentFloor,
            NextFloor = NextFloor,
            MovementDirection = MovementDirection,
            MovementState = MovementState,
            MaxWeight = MaxWeight,
            CurrentWeight = weight,
            ElevatorEvent = elevatorEvent
        };
    }
}