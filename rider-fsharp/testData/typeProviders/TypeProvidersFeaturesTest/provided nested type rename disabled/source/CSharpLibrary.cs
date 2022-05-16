using static SwaggerProviderLibrary;

namespace CSharpLibrary;

public class Class
{
    public void Main()
    {
        var client2 = new PetStore.Cli<caret>ent();
        var a = client.ApiCoursesGet().Result;
    }
}
