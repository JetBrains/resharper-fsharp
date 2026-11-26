using System.Collections.Generic;
using Expecto;
using Expecto.CSharp;

public class Samples
{
    [Tests]
    public static Test FieldTests =
        Runner.TestList("Field", new Test[] {
            Runner.TestCase("standard", () => { }),
            Runner.TestList("Nested", new Test[] {
                Runner.TestCase("nested", () => { }),
            }),
        });

    [Tests]
    public static Test PropertyTests => Runner.TestList("Property", new Test[] { Runner.TestCase("in property", () => { }) });

    [Tests]
    public static Test MethodTests()
    {
        var tests = new List<Test> { Runner.TestCase("in method", () => { }) };
        return Runner.TestList("Method", tests);
    }

    public static Test NoAttributeTests =
        Runner.TestList("No attribute", new Test[] {
            Runner.TestCase("discovered", () => { }),
        });
}
