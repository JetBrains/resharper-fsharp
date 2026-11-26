using System.Collections.Generic;
using Expecto;
using Expecto.CSharp;

public class Samples
{
    [Tests]
    public static Test Tests =
        Runner.TestList("All", new List<Test> {
            Runner.TestCase("in list", () => { }),
            Runner.TestList("Collection expression", [
                Runner.TestCase("in collection expression", () => { }),
            ]),
            (Test) Runner.TestCase("cast", () => { }),
        });

    [Tests]
    public static Test Property
    {
        get { return Runner.TestList("Property", new[] { Runner.TestCase("in getter", () => { }) }); }
    }

    [Tests]
    public static Test Method() => Runner.TestList("Method", new[] { Runner.TestCase("in method", () => { }) });
}
