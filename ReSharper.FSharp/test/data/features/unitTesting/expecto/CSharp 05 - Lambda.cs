using System.Linq;
using Expecto;
using Expecto.CSharp;

public class Samples
{
    static int[] Items = { 1 };

    [Tests]
    public static Test Tests =
        Runner.TestList("All", Items.Select(item => Runner.TestCase("case", () => { })).Concat(new[] {
            Runner.TestCase("standard", () => { }),
        }));
}
