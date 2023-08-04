using System.Collections.Concurrent;
using Domain.Enums;
using Domain.ValueObjects;

namespace Domain.DataStructures;

public class ButtonPressLookup // should this be something like a Priority Queue instead?
{
    private readonly ConcurrentDictionary<int, ConcurrentDictionary<ButtonPress, byte>> lookup = new();

    public bool Add(int floor, ButtonPress buttonPress)
    {
        if (lookup.TryGetValue(floor, out var buttonPressSet))
        {
            return buttonPressSet.TryAdd(buttonPress, 0);
        }

        buttonPressSet = new ConcurrentDictionary<ButtonPress, byte>();

        if (buttonPressSet.TryAdd(buttonPress, 0))
        {
            return lookup.TryAdd(floor, buttonPressSet);
        }

        return false;
    }

    public void Remove(int floor, ButtonPress buttonPress)
    {
        if (!lookup.TryGetValue(floor, out var buttonPressSet))
        {
            return;
        }

        buttonPressSet.TryRemove(buttonPress, out _);

        if (buttonPressSet.Count == 0)
        {
            lookup.TryRemove(floor, out _);
        }
    }

    public ButtonPressLookupResult ScanToTop(int currentFloor, int totalFloors)
    {
        var currentFloorButtonPresses = Array.Empty<ButtonPress>();
        var lastFloorButtonPresses = Array.Empty<ButtonPress>();
        var lastFloor = 0;

        for (var floor = 1; floor <= totalFloors; floor++)
        {
            if (floor < currentFloor) // ignore floors below us
            {
                continue;
            }

            if (lookup.TryGetValue(floor, out var buttonPressSet))
            {
                var buttonPresses = buttonPressSet.Keys.ToArray();

                if (floor == currentFloor)
                {
                    currentFloorButtonPresses = buttonPresses;
                }

                lastFloorButtonPresses = buttonPresses;
                lastFloor = floor;
            }
        }

        return new ButtonPressLookupResult
        {
            CurrentFloorPresses = currentFloorButtonPresses,
            LastFloorPresses = lastFloorButtonPresses,
            CurrentFloor = currentFloor,
            LastFloor = lastFloor
        };
    }

    public ButtonPressLookupResult ScanToBottom(int currentFloor)
    {
        var currentFloorButtonPresses = Array.Empty<ButtonPress>();
        var lastFloorButtonPresses = Array.Empty<ButtonPress>();
        var lastFloor = 0;

        for (var floor = currentFloor; floor >= 1; floor--)
        {
            if (lookup.TryGetValue(floor, out var buttonPressSet))
            {
                var buttonPresses = buttonPressSet.Keys.ToArray();

                if (floor == currentFloor)
                {
                    currentFloorButtonPresses = buttonPresses;
                }

                lastFloorButtonPresses = buttonPresses;
                lastFloor = floor;
            }
        }

        return new ButtonPressLookupResult
        {
            CurrentFloorPresses = currentFloorButtonPresses,
            LastFloorPresses = lastFloorButtonPresses,
            CurrentFloor = currentFloor,
            LastFloor = lastFloor
        };
    }

    public bool Any() => lookup.Any(l => l.Value.Any());

    public bool Any(MovementDirection movementDirection) => lookup.Any
    (
        l => l.Value.Any
        (
            bp => bp.Key.MovementDirection == movementDirection || bp.Key.MovementDirection == MovementDirection.Inside
        )
    );
}