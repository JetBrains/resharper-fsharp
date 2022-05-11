using static SwaggerProviderLibrary;

namespace CSharpLibrary;

public class Class
{
    public void Main()
    {
        var client = new PetStore.Client();
        var a = client.ApiCoursesGet().Result;
    }
}
