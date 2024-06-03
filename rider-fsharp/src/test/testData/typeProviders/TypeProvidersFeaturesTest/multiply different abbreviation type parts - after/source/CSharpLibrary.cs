using static SwaggerProviderLibrary3;

namespace CSharpLibrary;

public class Class
{
    public void Main()
    {
        var client = new Pet<caret>Store.Client();
        var a = client.ApiCoursesGet().Result;
    }
}
