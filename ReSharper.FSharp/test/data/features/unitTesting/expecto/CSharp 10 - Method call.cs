using Expecto;
using Expecto.CSharp;

public class Samples
{
    static Test Shared() =>
        Runner.TestList("Shared", new Test[] {
            Runner.TestCase("shared test", () => { }),
        });

    [Tests]
    public static Test AllTests =
        Runner.TestList("All", new Test[] {
            Shared(),
            (Runner.TestCase("parenthesized", () => { })),
        });
}
