namespace Domain.Enums;

public enum ElevatorEvent
{
    GetOn,
    GotOff,
    ChangeDirections,
    Idling,
    WaitOnFloor,
    StartMovingToFloor,
    FinishedMovingToFloor,
    Blocked
}
