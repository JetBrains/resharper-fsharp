using Expecto;
using Expecto.CSharp;

public class Samples
{
    public static Test SharedTests =
        Runner.TestList("Shared", new Test[] {
            Runner.TestCase("shared test", () => { }),
        });

    [Tests]
    public static Test AllTests =
        Runner.TestSequenced(
            Runner.TestList("All", new Test[] {
                SharedTests,
                Runner.TestCase("outer", () => { var inner = Runner.TestCase("inner", () => { }); }),
            }));
}
