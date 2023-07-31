namespace Application;

public interface IElevatorInputInterpreter : IAsyncDisposable
{
    void ReadInput(string input);
}
