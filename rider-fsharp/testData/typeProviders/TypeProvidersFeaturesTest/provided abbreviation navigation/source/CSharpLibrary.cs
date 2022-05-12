using static SwaggerProviderLibrary;

namespace CSharpLibrary;

public class Class
{
    public void Main()
    {
        var client = new Pet<caret>Store.Client();
        var a = client.ApiCoursesGet().Result;
    }
}
