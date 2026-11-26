using System.Linq;
using Expecto;
using Expecto.CSharp;

public class Samples
{
    [Tests]
    public static Test Tests =
        Runner.TestList("All", new[] {
            Runner.TestCase("first", () => { }),
            Runner.TestCase("second", () => { }),
        }.Skip(1));
}
