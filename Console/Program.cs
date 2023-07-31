using Application;
using Autofac;
using Domain.Entities;
using Infrastructure.Interpreter;
using Serilog;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
        .CreateLogger();

        Log.Logger.Information("Starting up!");

        await using var container = BuildDIContainer();
        await using var scope = container.BeginLifetimeScope();

        var interpreter = scope.Resolve<IElevatorInputInterpreter>();

        ReadInputAndWaitForExit(interpreter);
    }

    private static void ReadInputAndWaitForExit(IElevatorInputInterpreter interpreter)
    {
        while (true)
        {
            var line = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (line.Contains("Q", StringComparison.InvariantCultureIgnoreCase))
            {
                Log.Logger.Information("Shutting Down!");
                break;
            }

            try
            {
                interpreter.ReadInput(line);
            }
            catch (ArgumentException argumentException)
            {
                Log.Logger.Error(argumentException, "Bad Argument");
            }
            catch (InvalidOperationException invalidOperationException)
            {
                Log.Logger.Error(invalidOperationException, "Invalid Operation");
            }
            catch (FormatException formatException)
            {
                Log.Logger.Error(formatException, "Bad Format");
            }
            catch (Exception exception)
            {
                Log.Logger.Fatal(exception, "Fatal!");
                throw;
            }
        }
    }

    private static IContainer BuildDIContainer()
    {
        var containerBuilder = new ContainerBuilder();

        containerBuilder.RegisterInstance(Log.Logger);
        containerBuilder.RegisterInstance(new Elevator(1000, 10)); // config?
        containerBuilder.RegisterType<ElevatorInputInterpreter>().As<IElevatorInputInterpreter>().SingleInstance();

        return containerBuilder.Build();
    }
}