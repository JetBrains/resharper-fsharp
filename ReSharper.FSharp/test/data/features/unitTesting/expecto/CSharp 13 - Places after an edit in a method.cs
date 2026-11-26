using Expecto;
using Expecto.CSharp;

public static class Samples
{
    [Tests]
    public static Test All()
    {
        return Runner.TestList("All", new Test[] {
            Runner.TestCase("first", () => { }),
        });
    }
}
