using Expecto;
using Expecto.CSharp;

public class Samples
{
    private const string ConstantName = "constant";

    [Tests]
    public static Test Tests =
        Runner.TestList("All", new Test[] {
            Runner.TestCase(@"verbatim", () => { }),
            Runner.TestCase(ConstantName, () => { }),
            Runner.TestCase("con" + "cat", () => { }),
            Runner.TestCase(name: "named argument", test: () => { }),
            Runner.TestCase(System.Environment.MachineName, () => { }),
        });
}
