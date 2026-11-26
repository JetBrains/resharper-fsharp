using Expecto;
using Expecto.CSharp;

public static class Lists
{
    public static Test ParserTests =
        Runner.TestList("Parser", new Test[] {
            Runner.TestCase("numbers", () => { }),
            Main.Tests,
        });
}
