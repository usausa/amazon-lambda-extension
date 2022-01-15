namespace AmazonLambdaExtension.TargetProject;

public class ServiceLocator
{
    public ICalculator ResolveCalculator() => new Calculator();
}

public interface ICalculator
{
    public int Add(int x, int y);
}

public class Calculator : ICalculator
{
    public int Add(int x, int y) => x + y;
}
