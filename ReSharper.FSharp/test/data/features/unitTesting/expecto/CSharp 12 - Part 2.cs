using Expecto;
using Expecto.CSharp;

public static partial class Samples
{
    static Test Shared =
        Runner.TestList("Shared", new Test[] {
            Runner.TestCase("x", () => { }),
        });
}
