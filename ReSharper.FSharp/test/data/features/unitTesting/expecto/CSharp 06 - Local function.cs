using Expecto;
using Expecto.CSharp;

public class Samples
{
    [Tests]
    public static Test Tests
    {
        get
        {
            Test Make() => Runner.TestCase("local", () => { });
            return Runner.TestList("All", new[] { Make() });
        }
    }
}
