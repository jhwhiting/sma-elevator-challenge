# sma-elevator-challenge
The Elevator Coding Challenge for SMA

## Methodology
Domain Driven Design (Eric Evans)
TDD (somewhat, this was really handy for finding the edge cases, and I'm sure I didn't find them all!)

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
