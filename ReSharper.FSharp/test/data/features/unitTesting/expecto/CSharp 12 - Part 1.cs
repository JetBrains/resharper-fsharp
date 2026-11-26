using Expecto;
using Expecto.CSharp;

public static partial class Samples
{
    [Tests]
    public static Test Tests =
        Runner.TestList("All", new Test[] {
            Shared,
        });
}
