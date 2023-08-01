# sma-elevator-challenge
The Elevator Coding Challenge for SMA

## Framework
Net 7.0

## To Run
`dotnet run` in the `Console` directory.
`dotnet test` in the `Tests` directory.

## Methodology
Domain Driven Design (Eric Evans)

TDD (this was really handy for finding some of the edge cases)

Domain: the layer that holds the business rules and behaviors. It is supposed to be as pure as possible and not reference other layers.

Application: interfaces for external integrations and behaviors for things that aren't part of the Domain.

Infrastructure: concrete implementations.

Entity: in DDD this means something with an identity and lifecycle.
Value Object: in DDD this means something that is descriptive, but doesn't have an identity. Think of a business card, for instance.

I've defined the behaviors of the Elevator in the Domain, and I've exposed a couple events that we can pretend could be Event Source Streams, Domain Events, or some other integration. For this application they are sending Value Objects that are logged by Infrastructure.

## Packages
`Serilog` for console and file logging.
`Autofac` for dependency injection.

## Signaling

I went back and worth with a couple different approaches to try and implement the asynchronous button request and response behavior. I tried a `Channel` but I had to store the button requests later. So, I switched the signal to a `Semaphore`. An internal data-structure like a `Trie` contains the floors and possible button combinations. When a button press is received we release the semaphore and scan the `Trie` in comparison to the current position.

I'm not sure if this is the best approach, but it got the job done.

## Testing

I used xUnit for testing and a X Should Do X with the standard Arrange/Act/Assert pattern.

### Sample Output
[19:24:56 INF] Starting up!

[19:24:56 INF] ElevatorSensorData { MovementDirection = Up, MovementState = Stopped, ElevatorEvent = Idling, CurrentFloor = 1, NextFloor = 1, CurrentWeight = 0, MaxWeight = 1000, WeightLimitReached = False }

5

[19:24:58 INF] ButtonPress { Floor = 5, MovementDirection = Inside }

[19:24:58 INF] ElevatorSensorData { MovementDirection = Up, MovementState = Moving, ElevatorEvent = StartMovingToFloor, CurrentFloor = 1, NextFloor = 2, CurrentWeight = 0, MaxWeight = 1000, WeightLimitReached = False }

[19:25:01 INF] ElevatorSensorData { MovementDirection = Up, MovementState = Moving, ElevatorEvent = FinishedMovingToFloor, CurrentFloor = 2, NextFloor = 2, CurrentWeight = 0, MaxWeight = 1000, WeightLimitReached = False }

[19:25:01 INF] ElevatorSensorData { MovementDirection = Up, MovementState = Moving, ElevatorEvent = StartMovingToFloor, CurrentFloor = 2, NextFloor = 3, CurrentWeight = 0, MaxWeight = 1000, WeightLimitReached = False }

[19:25:04 INF] ElevatorSensorData { MovementDirection = Up, MovementState = Moving, ElevatorEvent = FinishedMovingToFloor, CurrentFloor = 3, NextFloor = 3, CurrentWeight = 0, MaxWeight = 1000, WeightLimitReached = False }

[19:25:04 INF] ElevatorSensorData { MovementDirection = Up, MovementState = Moving, ElevatorEvent = StartMovingToFloor, CurrentFloor = 3, NextFloor = 4, CurrentWeight = 0, MaxWeight = 1000, WeightLimitReached = False }

1U

[19:25:05 INF] ButtonPress { Floor = 1, MovementDirection = Up }

[19:25:07 INF] ElevatorSensorData { MovementDirection = Up, MovementState = Moving, ElevatorEvent = FinishedMovingToFloor, CurrentFloor = 4, NextFloor = 4, CurrentWeight = 0, MaxWeight = 1000, WeightLimitReached = False }

[19:25:07 INF] ElevatorSensorData { MovementDirection = Up, MovementState = Moving, ElevatorEvent = StartMovingToFloor, CurrentFloor = 4, NextFloor = 5, CurrentWeight = 0, MaxWeight = 1000, WeightLimitReached = False }

[19:25:10 INF] ElevatorSensorData { MovementDirection = Up, MovementState = Moving, ElevatorEvent = FinishedMovingToFloor, CurrentFloor = 5, NextFloor = 5, CurrentWeight = 0, MaxWeight = 1000, WeightLimitReached = False }

[19:25:10 INF] ElevatorSensorData { MovementDirection = Up, MovementState = Stopped, ElevatorEvent = WaitOnFloor, CurrentFloor = 5, NextFloor = 5, CurrentWeight = 0, MaxWeight = 1000, WeightLimitReached = False }

[19:25:11 INF] ElevatorSensorData { MovementDirection = Down, MovementState = Stopped, ElevatorEvent = ChangeDirections, CurrentFloor = 5, NextFloor = 5, CurrentWeight = 0, MaxWeight = 1000, WeightLimitReached = False }

[19:25:11 INF] ElevatorSensorData { MovementDirection = Down, MovementState = Moving, ElevatorEvent = StartMovingToFloor, CurrentFloor = 5, NextFloor = 4, CurrentWeight = 0, MaxWeight = 1000, WeightLimitReached = False }

[19:25:14 INF] ElevatorSensorData { MovementDirection = Down, MovementState = Moving, ElevatorEvent = FinishedMovingToFloor, CurrentFloor = 4, NextFloor = 4, CurrentWeight = 0, MaxWeight = 1000, WeightLimitReached = False }

[19:25:14 INF] ElevatorSensorData { MovementDirection = Down, MovementState = Moving, ElevatorEvent = StartMovingToFloor, CurrentFloor = 4, NextFloor = 3, CurrentWeight = 0, MaxWeight = 1000, WeightLimitReached = False }

[19:25:17 INF] ElevatorSensorData { MovementDirection = Down, MovementState = Moving, ElevatorEvent = FinishedMovingToFloor, CurrentFloor = 3, NextFloor = 3, CurrentWeight = 0, MaxWeight = 1000, WeightLimitReached = False }

[19:25:17 INF] ElevatorSensorData { MovementDirection = Down, MovementState = Moving, ElevatorEvent = StartMovingToFloor, CurrentFloor = 3, NextFloor = 2, CurrentWeight = 0, MaxWeight = 1000, WeightLimitReached = False }

[19:25:20 INF] ElevatorSensorData { MovementDirection = Down, MovementState = Moving, ElevatorEvent = FinishedMovingToFloor, CurrentFloor = 2, NextFloor = 2, CurrentWeight = 0, MaxWeight = 1000, WeightLimitReached = False }

[19:25:20 INF] ElevatorSensorData { MovementDirection = Down, MovementState = Moving, ElevatorEvent = StartMovingToFloor, CurrentFloor = 2, NextFloor = 1, CurrentWeight = 0, MaxWeight = 1000, WeightLimitReached = False }

[19:25:23 INF] ElevatorSensorData { MovementDirection = Down, MovementState = Moving, ElevatorEvent = FinishedMovingToFloor, CurrentFloor = 1, NextFloor = 1, CurrentWeight = 0, MaxWeight = 1000, WeightLimitReached = False }

[19:25:23 INF] ElevatorSensorData { MovementDirection = Down, MovementState = Stopped, ElevatorEvent = WaitOnFloor, CurrentFloor = 1, NextFloor = 1, CurrentWeight = 0, MaxWeight = 1000, WeightLimitReached = False }

[19:25:24 INF] ElevatorSensorData { MovementDirection = Up, MovementState = Stopped, ElevatorEvent = ChangeDirections, CurrentFloor = 1, NextFloor = 1, CurrentWeight = 0, MaxWeight = 1000, WeightLimitReached = False }

[19:25:24 INF] ElevatorSensorData { MovementDirection = Up, MovementState = Stopped, ElevatorEvent = Idling, CurrentFloor = 1, NextFloor = 1, CurrentWeight = 0, MaxWeight = 1000, WeightLimitReached = False }

[19:25:27 INF] ElevatorSensorData { MovementDirection = Up, MovementState = Stopped, ElevatorEvent = Idling, CurrentFloor = 1, NextFloor = 1, CurrentWeight = 0, MaxWeight = 1000, WeightLimitReached = False }

q

[19:25:30 INF] Shutting Down!
