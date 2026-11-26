using Expecto;
using Expecto.CSharp;

public static class Main
{
    [Tests]
    public static Test Tests =
        Runner.TestList("All", new Test[] {
            Lists.ParserTests,
        });
}
