using Application;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Serilog;

namespace Infrastructure.Interpreter;

public class ElevatorInputInterpreter : IElevatorInputInterpreter, IDisposable
{
    private readonly Elevator elevator;
    private readonly ILogger logger;

    public ElevatorInputInterpreter(Elevator elevator, ILogger logger)
    {
        this.elevator = elevator;
        this.elevator.ButtonPressed += LogButtonPressed;
        this.elevator.ElevatorSensorDataGenerated += LogElevatorSensorDataGenerated;
        this.logger = logger;
        this.logger.Information(this.elevator.ElevatorSensorData.ToString());
    }

    private void LogButtonPressed(object? sender, ButtonPress e)
    {
        logger.Information(e.ToString());
    }

    private void LogElevatorSensorDataGenerated(object? sender, ElevatorSensorData e)
    {
        logger.Information(e.ToString());
    }

    public void ReadInput(string input)
    {
        input = input.Trim();

        if (input.Length < 1)
        {
            throw new ArgumentException("bad input");
        }

        if (TryMutateOccupant(input))
        {
            return;
        }

        int floor = GetFloor(input);
        var movementDirection = GetDirection(input);

        if (movementDirection.HasValue)
        {
            elevator.CallElevatorOutside(floor, movementDirection.Value);
            return;
        }

        elevator.PressFloorButtonInside(floor);
    }

    private bool TryMutateOccupant(string input)
    {
        var firstChar = input[0];

        if (firstChar == '-')
        {
            elevator.GetOff();

            return true;
        }
        else if (firstChar == '+')
        {
            if (input.Length < 2)
            {
                throw new FormatException("must provide a weight");
            }

            var number = input.Substring(1);

            var weight = int.Parse(number);
            elevator.GetOn(weight);

            return true;
        }

        return false;
    }

    private static int GetFloor(string input)
    {
        var floor = 0;

        if (char.IsLetter(input.Last()))
        {
            if (!int.TryParse(input.Substring(0, input.Length - 1), out floor))
            {
                throw new ArgumentException("bad floor");
            }
        }
        else if (!int.TryParse(input.Substring(0, input.Length), out floor))
        {
            throw new ArgumentException("bad floor");
        }

        return floor;
    }

    private static MovementDirection? GetDirection(string input)
    {
        if (input.Length > 1)
        {
            var direction = input.Substring(input.Length - 1, 1);

            if (direction.Equals("U", StringComparison.InvariantCultureIgnoreCase))
            {
                return MovementDirection.Up;
            }
            else if (direction.Equals("D", StringComparison.InvariantCultureIgnoreCase))
            {
                return MovementDirection.Down;
            }
            else if (!int.TryParse(direction, out _))
            {
                throw new ArgumentException("bad direction");
            }
        }

        return null;
    }

    public void Dispose()
    {
        elevator.ButtonPressed -= LogButtonPressed;
        elevator.ElevatorSensorDataGenerated -= LogElevatorSensorDataGenerated;
    }
}
