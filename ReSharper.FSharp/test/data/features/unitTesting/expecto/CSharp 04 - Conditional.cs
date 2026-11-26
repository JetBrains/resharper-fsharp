using Expecto;
using Expecto.CSharp;

public class Samples
{
    static bool Condition;

    [Tests]
    public static Test Tests =
        Runner.TestList("All", new Test[] {
            Condition ? Runner.TestCase("conditional", () => { }) : Runner.TestCase("other", () => { }),
            Runner.TestCase("standard", () => { }),
        });
}
