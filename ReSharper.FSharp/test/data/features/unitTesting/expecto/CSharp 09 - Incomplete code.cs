using Expecto;
using Expecto.CSharp;

public class Samples
{
    [Tests]
    public static Test AllTests =
        Runner.TestList("All", new Test[] {
            Runner.TestCase(, () => { }),
            Runner.TestList(),
            Runner.TestCase("no body"),
            Runner.TestCase("complete", () => { }),
            Runner.TestCase("open", () => {
