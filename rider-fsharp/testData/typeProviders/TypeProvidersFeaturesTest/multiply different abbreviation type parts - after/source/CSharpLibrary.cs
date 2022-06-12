using static SwaggerProviderLibrary3;

namespace CSharpLibrary;

public class Class
{
    public void Main()
    {
        var client = new PetStore.Cli<caret>ent();
        var a = client.ApiCoursesGet().Result;
    }
}
