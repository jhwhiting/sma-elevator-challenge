using System.Collections.Concurrent;
using Domain.DataStructures;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.Entities;

public class Elevator : IEntity, IAsyncDisposable
{
    private readonly ButtonPressLookup buttonPressLookup = new();
    private readonly SemaphoreSlim inputSemaphore = new(0);
    private readonly ConcurrentStack<int> occupants = new();
    private readonly ConcurrentDictionary<int, ButtonPress> betweenFloorsRequests = new();

    private readonly Task inputTask;
    private readonly int totalFloors;
    private readonly int maxWeight;

    private bool disposed;

    public Elevator(int maxWeight, int totalFloors)
    {
        ElevatorSensorData = new ElevatorSensorData
        {
            MovementDirection = MovementDirection.Up,
            MovementState = MovementState.Stopped,
            CurrentFloor = 1,
            NextFloor = 1,
            CurrentWeight = 0,
            MaxWeight = maxWeight,
            ElevatorEvent = ElevatorEvent.Idling
        };

        this.maxWeight = maxWeight;
        this.totalFloors = totalFloors;
        this.inputTask = Task.Run(IntepretInput);
    }

    public event EventHandler<ElevatorSensorData> ElevatorSensorDataGenerated = delegate { };

    public event EventHandler<ButtonPress> ButtonPressed = delegate { };

    public ElevatorSensorData ElevatorSensorData { get; private set; }

    private async Task IntepretInput()
    {
        while (!disposed)
        {
            try
            {
                await inputSemaphore.WaitAsync().ConfigureAwait(false);

                while (buttonPressLookup.Any())
                {
                    if (ElevatorSensorData.MovementDirection == MovementDirection.Up)
                    {
                        while (await GoUp().ConfigureAwait(false)) { }
                    }
                    else
                    {
                        while (await GoDown().ConfigureAwait(false)) { }
                    }
                }

                if (inputSemaphore.CurrentCount == 0)
                {
                    UpdateSensor(ElevatorSensorData.CurrentFloor, ElevatorSensorData.CurrentFloor, ElevatorSensorData.MovementDirection, MovementState.Stopped, ElevatorEvent.Idling);
                }

                await Task.Delay(TimeSpan.FromSeconds(3));

                InsertBetweenFloorsRequests(0); // prevent deadlock
            }
            catch (ObjectDisposedException) // stopping
            {
            }
        }
    }

    private async Task<bool> GoUp()
    {
        var currentFloor = ElevatorSensorData.CurrentFloor;
        var buttonPressLookupResult = buttonPressLookup.ScanToTop(currentFloor, totalFloors);

        if (buttonPressLookupResult.LastFloor == 0 && buttonPressLookup.Any(MovementDirection.Down))
        {
            UpdateSensor(currentFloor, currentFloor, MovementDirection.Down, MovementState.Stopped, ElevatorEvent.ChangeDirections);
            return false;
        }

        var getOff = buttonPressLookupResult.CurrentFloorPresses.Any(cfp => cfp.MovementDirection == MovementDirection.Inside);
        if (currentFloor < buttonPressLookupResult.LastFloor) // rise one floor
        {
            var waitOnFloor = DecideToWaitOnFloor(buttonPressLookupResult.CurrentFloorPresses, MovementDirection.Up);
            if (waitOnFloor)
            {
                await WaitOnFloor(currentFloor, MovementDirection.Up, getOff).ConfigureAwait(false);
            }

            await TravelBetweenFloors(currentFloor, currentFloor + 1, MovementDirection.Up).ConfigureAwait(false);
            return true;
        }

        // stop and reseed
        bool waitOnLastFloor = DecideToWaitOnLastFloor(buttonPressLookupResult);
        if (waitOnLastFloor)
        {
            await WaitOnLastFloor(currentFloor, MovementDirection.Up, getOff).ConfigureAwait(false);
            if (buttonPressLookupResult.LastFloorPresses.Any(bp => bp.MovementDirection == MovementDirection.Down))
            {
                UpdateSensor(currentFloor, currentFloor, MovementDirection.Down, MovementState.Stopped, ElevatorEvent.ChangeDirections);
            }
        }
        else if (buttonPressLookupResult.LastFloor == currentFloor) // can't open, go back down
        {
            UpdateSensor(currentFloor, currentFloor, MovementDirection.Down, MovementState.Stopped, ElevatorEvent.Blocked);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
        else
        {
            UpdateSensor(currentFloor, currentFloor, MovementDirection.Down, MovementState.Stopped, ElevatorEvent.ChangeDirections);
        }

        return false;
    }

    private async Task<bool> GoDown()
    {
        var currentFloor = ElevatorSensorData.CurrentFloor;
        var buttonPressLookupResult = buttonPressLookup.ScanToBottom(currentFloor);

        if (buttonPressLookupResult.LastFloor == 0 && buttonPressLookup.Any(MovementDirection.Up))
        {
            UpdateSensor(currentFloor, currentFloor, MovementDirection.Up, MovementState.Stopped, ElevatorEvent.ChangeDirections);
            return false;
        }

        var getOff = buttonPressLookupResult.CurrentFloorPresses.Any(cfp => cfp.MovementDirection == MovementDirection.Inside);
        if (currentFloor > buttonPressLookupResult.LastFloor) // fall one floor
        {
            var waitOnFloor = DecideToWaitOnFloor(buttonPressLookupResult.CurrentFloorPresses, MovementDirection.Down);
            if (waitOnFloor)
            {
                await WaitOnFloor(currentFloor, MovementDirection.Down, getOff).ConfigureAwait(false);
            }

            await TravelBetweenFloors(currentFloor, currentFloor - 1, MovementDirection.Down).ConfigureAwait(false);
            return true;
        }

        // stop and reseed  
        var waitOnLastFloor = DecideToWaitOnLastFloor(buttonPressLookupResult);
        if (waitOnLastFloor)
        {
            await WaitOnLastFloor(currentFloor, MovementDirection.Down, getOff).ConfigureAwait(false);
            if (buttonPressLookupResult.LastFloorPresses.Any(bp => bp.MovementDirection == MovementDirection.Up))
            {
                UpdateSensor(currentFloor, currentFloor, MovementDirection.Up, MovementState.Stopped, ElevatorEvent.ChangeDirections);
            }
        }
        else if (buttonPressLookupResult.LastFloor == currentFloor) // can't open, go back up
        {
            UpdateSensor(currentFloor, currentFloor, MovementDirection.Up, MovementState.Stopped, ElevatorEvent.Blocked); /// not sure what do here
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
        else
        {
            UpdateSensor(currentFloor, currentFloor, MovementDirection.Up, MovementState.Stopped, ElevatorEvent.ChangeDirections);
        }

        return false;
    }

    private bool DecideToWaitOnLastFloor(ButtonPressLookupResult buttonPressLookupResult)
    {
        return DecideToWaitOnFloor(buttonPressLookupResult.CurrentFloorPresses, MovementDirection.Up) || DecideToWaitOnFloor(buttonPressLookupResult.CurrentFloorPresses, MovementDirection.Down);
    }

    private bool DecideToWaitOnFloor(ButtonPress[] currentFloorPresses, MovementDirection movementDirection)
    {
        var waitQuery = currentFloorPresses.Where(cf => cf.MovementDirection == movementDirection || cf.MovementDirection == MovementDirection.Inside);

        if (occupants.Sum() > maxWeight)
        {
            waitQuery = waitQuery.Where(wq => wq.MovementDirection == MovementDirection.Inside);
        }

        return waitQuery.Any();
    }

    private bool SkipIfMovingAndIsNextFloor(int floor, ButtonPress buttonPress)
    {
        if (ElevatorSensorData.MovementState == MovementState.Moving && ElevatorSensorData.NextFloor == floor)
        {
            betweenFloorsRequests.TryAdd(floor, buttonPress);
            ButtonPressed(this, buttonPress);
            return true;
        }

        return false;
    }

    private void ThrowOnInvalidFloor(int floor, MovementDirection direction)
    {
        if (floor < 1)
        {
            throw new InvalidOperationException("floor must be greater than 0");
        }

        if (floor == 1 && direction == MovementDirection.Down)
        {
            throw new InvalidOperationException("cannot go down from the first floor");
        }

        if (floor > totalFloors)
        {
            throw new InvalidOperationException("floor must be less than or equal to the total number of floors");
        }

        if (floor == totalFloors && direction == MovementDirection.Up)
        {
            throw new InvalidOperationException("cannot go up from the top floor");
        }
    }

    public void CallElevatorOutside(int floor, MovementDirection direction)
    {
        ThrowIfDisposed();
        ThrowOnInvalidFloor(floor, direction);

        var buttonPress = new ButtonPress { Floor = floor, MovementDirection = direction };

        if (SkipIfMovingAndIsNextFloor(floor, buttonPress))
        {
            return;
        }

        if (buttonPressLookup.Add(floor, buttonPress))
        {
            ButtonPressed(this, buttonPress);

            inputSemaphore.Release();
        }
    }

    public void PressFloorButtonInside(int floor)
    {
        ThrowIfDisposed();
        ThrowOnInvalidFloor(floor, MovementDirection.Inside);

        var buttonPress = new ButtonPress { Floor = floor, MovementDirection = MovementDirection.Inside };

        if (SkipIfMovingAndIsNextFloor(floor, buttonPress))
        {
            return;
        }

        if (buttonPressLookup.Add(floor, buttonPress))
        {
            ButtonPressed(this, buttonPress);

            inputSemaphore.Release();
        }
    }

    public void GetOn(int weight)
    {
        if (ElevatorSensorData.MovementState != MovementState.Stopped)
        {
            throw new InvalidOperationException("cannot get on while moving");
        }

        occupants.Push(weight);

        ElevatorSensorData = ElevatorSensorData.FromWeight(occupants.Sum(), ElevatorEvent.GetOn);

        ElevatorSensorDataGenerated(this, ElevatorSensorData);
    }

    public void GetOff()
    {
        if (ElevatorSensorData.MovementState != MovementState.Stopped)
        {
            throw new InvalidOperationException("cannot get off while moving");
        }

        if (occupants.TryPop(out _))
        {
            ElevatorSensorData = ElevatorSensorData.FromWeight(occupants.Sum(), ElevatorEvent.GotOff);

            ElevatorSensorDataGenerated(this, ElevatorSensorData);
        }
    }

    public async Task DrainInput()
    {
        ThrowIfDisposed();

        while (buttonPressLookup.Any() || betweenFloorsRequests.Any())
        {
            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false); // must be a better way to do this
        }

        await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
    }

    private Task WaitOnLastFloor(int currentFloor, MovementDirection movementDirection, bool getOff)
    {
        var insideButtonPress = new ButtonPress { Floor = currentFloor, MovementDirection = MovementDirection.Inside };
        var outsideUpButtonPress = new ButtonPress { Floor = currentFloor, MovementDirection = MovementDirection.Up };
        var outsideDownButtonPress = new ButtonPress { Floor = currentFloor, MovementDirection = MovementDirection.Down };

        buttonPressLookup.Remove(currentFloor, outsideUpButtonPress); // finish state, remove both
        buttonPressLookup.Remove(currentFloor, outsideDownButtonPress);
        buttonPressLookup.Remove(currentFloor, insideButtonPress);

        return WaitOnFloor(currentFloor, movementDirection, getOff);
    }

    private async Task WaitOnFloor(int currentFloor, MovementDirection movementDirection, bool getOff)
    {
        UpdateSensor(currentFloor, currentFloor, movementDirection, MovementState.Stopped, ElevatorEvent.WaitOnFloor);

        if (getOff)
        {
            GetOff();
        }

        var insideButtonPress = new ButtonPress { Floor = currentFloor, MovementDirection = MovementDirection.Inside };
        var outsideButtonPress = new ButtonPress { Floor = currentFloor, MovementDirection = movementDirection };

        buttonPressLookup.Remove(currentFloor, insideButtonPress); // finish state, remove both
        buttonPressLookup.Remove(currentFloor, outsideButtonPress);

        await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false); // configurable?        

        InsertBetweenFloorsRequests(currentFloor);
    }

    private async Task TravelBetweenFloors(int currentFloor, int nextFloor, MovementDirection movementDirection)
    {
        UpdateSensor(currentFloor, nextFloor, movementDirection, MovementState.Moving, ElevatorEvent.StartMovingToFloor);

        await Task.Delay(TimeSpan.FromSeconds(3)); // configurable?

        UpdateSensor(nextFloor, nextFloor, movementDirection, MovementState.Moving, ElevatorEvent.FinishedMovingToFloor); // arrived, possibly keep moving

        InsertBetweenFloorsRequests(nextFloor);
    }

    private void UpdateSensor(int currentFloor, int nextFloor, MovementDirection movementDirection, MovementState movementState, ElevatorEvent elevatorEvent)
    {
        ElevatorSensorData = new ElevatorSensorData
        {
            CurrentFloor = currentFloor,
            NextFloor = nextFloor,
            MovementDirection = movementDirection,
            MovementState = movementState,
            MaxWeight = maxWeight,
            CurrentWeight = occupants.Sum(),
            ElevatorEvent = elevatorEvent
        };

        ElevatorSensorDataGenerated(this, ElevatorSensorData);
    }

    private void InsertBetweenFloorsRequests(int currentFloor)
    {
        for (var floor = 1; floor <= currentFloor - 1; floor++)
        {
            if (betweenFloorsRequests.TryRemove(floor, out var buttonPress))
            {
                buttonPressLookup.Add(floor, buttonPress);
                inputSemaphore.Release();
            }
        }

        for (var floor = currentFloor + 1; floor <= totalFloors; floor++)
        {
            if (betweenFloorsRequests.TryRemove(floor, out var buttonPress))
            {
                buttonPressLookup.Add(floor, buttonPress);
                inputSemaphore.Release();
            }
        }
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(Elevator));
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DrainInput().ConfigureAwait(false);

        disposed = true;
        inputSemaphore.Dispose();
    }
}