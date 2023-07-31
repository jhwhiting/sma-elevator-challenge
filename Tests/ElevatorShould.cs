using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Infrastructure.Debug;
using Infrastructure.Interpreter;
using Serilog;
using Serilog.Core;

namespace Tests;

public class ElevatorShould
{
    private ILogger logger = Logger.None;

    [Fact]
    public async Task No_Op()
    {
        // Arrange
        await using (var elevator = new Elevator(int.MaxValue, 9))
        {
            var elevatorInputInterpreter = new ElevatorInputInterpreter(elevator, logger);

            // Act
            elevatorInputInterpreter.ReadInput("1U");
            elevatorInputInterpreter.ReadInput("1");
            elevatorInputInterpreter.ReadInput("1");
            await elevator.DrainInput().ConfigureAwait(false);

            // Assert
            Assert.True(elevator.ElevatorSensorData.CurrentFloor == 1);
            Assert.True(elevator.ElevatorSensorData.MovementDirection == MovementDirection.Up);
            Assert.True(elevator.ElevatorSensorData.MovementState == MovementState.Stopped);
        }
    }

    [Fact]
    public async Task Not_Stop_When_Overloaded_Unless_Pressed_Inside()
    {
        // Arrange
        await using (var elevator = new Elevator(200, 9))
        {
            using var elevatorDebugWriter = new ElevatorDebugWriter(elevator);
            var elevatorInputInterpreter = new ElevatorInputInterpreter(elevator, logger);

            // open floor 1
            // add 100 lbs
            // rise to floor 2
            // add 300 lbs
            // rise to floor 3 -- DO NOT OPEN
            // rise to floor 4 -- open and let one off
            // fall to floor 1 -- open and let one off
            // rise to floor 3 -- now we can open

            // Act        
            elevatorInputInterpreter.ReadInput("1U");
            elevatorInputInterpreter.ReadInput("+100");
            await elevator.DrainInput().ConfigureAwait(false);
            elevatorInputInterpreter.ReadInput("2U");
            await elevator.DrainInput().ConfigureAwait(false);
            elevatorInputInterpreter.ReadInput("+300");
            await elevator.DrainInput().ConfigureAwait(false);
            elevatorInputInterpreter.ReadInput("3U");
            elevatorInputInterpreter.ReadInput("1");
            elevatorInputInterpreter.ReadInput("4");
            await elevator.DrainInput().ConfigureAwait(false);

            // Assert
            Assert.True(elevator.ElevatorSensorData.CurrentFloor == 3);
            Assert.True(elevator.ElevatorSensorData.MovementDirection == MovementDirection.Up);
            Assert.True(elevator.ElevatorSensorData.MovementState == MovementState.Stopped);
            Assert.True(elevator.ElevatorSensorData.ElevatorEvent == ElevatorEvent.Idling);
            Assert.True(elevator.ElevatorSensorData.CurrentWeight == 0);
        }
    }

    [Fact]
    public async Task Recover_From_Block()
    {
        // Arrange
        await using (var elevator = new Elevator(200, 9))
        {
            using var elevatorDebugWriter = new ElevatorDebugWriter(elevator);
            var elevatorInputInterpreter = new ElevatorInputInterpreter(elevator, logger);

            // Act        
            elevatorInputInterpreter.ReadInput("+1000");
            elevatorInputInterpreter.ReadInput("2U");
            await Task.Delay(TimeSpan.FromSeconds(10)).ConfigureAwait(false); ; // blocked on 2 floor
            elevatorInputInterpreter.ReadInput("1"); // press to get out!!!
            await elevator.DrainInput().ConfigureAwait(false);
            // should go back to 2nd floor afterward

            // Assert
            Assert.True(elevator.ElevatorSensorData.CurrentFloor == 2);
            Assert.True(elevator.ElevatorSensorData.MovementDirection == MovementDirection.Up);
            Assert.True(elevator.ElevatorSensorData.MovementState == MovementState.Stopped);
            Assert.True(elevator.ElevatorSensorData.ElevatorEvent == ElevatorEvent.Idling);
            Assert.True(elevator.ElevatorSensorData.CurrentWeight == 0);
        }
    }

    [Fact]
    public async Task Rise_One_Floor()
    {
        // Arrange
        await using (var elevator = new Elevator(int.MaxValue, 9))
        {
            var elevatorInputInterpreter = new ElevatorInputInterpreter(elevator, logger);

            // Act
            elevatorInputInterpreter.ReadInput("2U");
            await Task.Delay(1500).ConfigureAwait(false);
            elevatorInputInterpreter.ReadInput("2");
            elevatorInputInterpreter.ReadInput("2");
            elevatorInputInterpreter.ReadInput("2");
            await elevator.DrainInput().ConfigureAwait(false);

            // Assert
            Assert.True(elevator.ElevatorSensorData.CurrentFloor == 2);
            Assert.True(elevator.ElevatorSensorData.MovementDirection == MovementDirection.Up);
            Assert.True(elevator.ElevatorSensorData.MovementState == MovementState.Stopped);
        }
    }

    [Fact]
    public async Task Rise_One_Then_Two_Then_One_Then_Go_Back_Down()
    {
        // Arrange
        await using (var elevator = new Elevator(int.MaxValue, 9))
        {
            var elevatorInputInterpreter = new ElevatorInputInterpreter(elevator, logger);

            // Act
            elevatorInputInterpreter.ReadInput("2U");
            elevatorInputInterpreter.ReadInput("4");
            elevatorInputInterpreter.ReadInput("5D");

            await elevator.DrainInput().ConfigureAwait(false);

            // Assert
            Assert.True(elevator.ElevatorSensorData.CurrentFloor == 5);
            Assert.True(elevator.ElevatorSensorData.MovementDirection == MovementDirection.Down);
            Assert.True(elevator.ElevatorSensorData.MovementState == MovementState.Stopped);
        }
    }

    [Fact]
    public async Task Rise_Three_Floors_And_Fall_Two()
    {
        // Arrange
        var elevator = new Elevator(int.MaxValue, 9);
        var elevatorInputInterpreter = new ElevatorInputInterpreter(elevator, logger);

        // Act
        elevatorInputInterpreter.ReadInput("4");
        elevatorInputInterpreter.ReadInput("2D");
        await elevator.DrainInput().ConfigureAwait(false);

        System.Diagnostics.Debug.WriteLine(elevator.ElevatorSensorData);

        // Assert
        Assert.True(elevator.ElevatorSensorData.CurrentFloor == 2);
        Assert.True(elevator.ElevatorSensorData.MovementDirection == MovementDirection.Down);
        Assert.True(elevator.ElevatorSensorData.MovementState == MovementState.Stopped);
    }

    [Fact]
    public async Task Rise_One_Floor_And_Fall_One_Generate_Logs()
    {
        // Arrange
        var sensorDataEntries = new List<ElevatorSensorData>();

        void RecordSensorData(object? sender, ElevatorSensorData sensorData)
        {
            sensorDataEntries.Add(sensorData);
        }

        await using (var elevator = new Elevator(int.MaxValue, 9))
        {
            sensorDataEntries.Add(elevator.ElevatorSensorData);

            elevator.ElevatorSensorDataGenerated += RecordSensorData;

            var elevatorInputInterpreter = new ElevatorInputInterpreter(elevator, logger);

            // Act
            elevatorInputInterpreter.ReadInput("2");
            elevatorInputInterpreter.ReadInput("1");
        }

        // Assert
        Assert.True(sensorDataEntries.Any());
    }

    [Fact]
    public async Task Rise_Three_Floors_And_Stop_At_Each()
    {
        // Arrange
        var sensorDataEntries = new List<ElevatorSensorData>();

        void RecordSensorData(object? sender, ElevatorSensorData sensorData)
        {
            sensorDataEntries.Add(sensorData);
        }

        await using (var elevator = new Elevator(int.MaxValue, 9))
        {
            sensorDataEntries.Add(elevator.ElevatorSensorData);

            elevator.ElevatorSensorDataGenerated += RecordSensorData;

            var elevatorInputInterpreter = new ElevatorInputInterpreter(elevator, logger);

            // Act
            elevatorInputInterpreter.ReadInput("1U");
            elevatorInputInterpreter.ReadInput("2U");
            elevatorInputInterpreter.ReadInput("3U");
        }

        foreach (var sensorData in sensorDataEntries)
        {
            System.Diagnostics.Debug.WriteLine(sensorData);
        }

        // Assert
        Assert.True(sensorDataEntries.Any());
        Assert.True(sensorDataEntries.Count(sensorDataEntry => sensorDataEntry.ElevatorEvent == ElevatorEvent.WaitOnFloor) == 3);
    }
}