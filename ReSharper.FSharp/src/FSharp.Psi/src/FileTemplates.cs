using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.ProjectModel.Resources;
using JetBrains.ReSharper.Feature.Services.LiveTemplates.Templates;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  [ShellComponent]
  public class FSharpDefaultFileTemplates : IHaveDefaultSettingsStream
  {
    private const string XmlPath = "JetBrains.ReSharper.Plugins.FSharp.Psi.Features.Templates.FileTemplates.xml";

    public string Name => "Default F# file templates";

    [UsedImplicitly]
    public static readonly TemplateImage FSharp = new TemplateImage("FSharp", ProjectModelThemedIcons.Fsharp.Id);

    public Stream GetDefaultSettingsStream(Lifetime lifetime)
    {
      var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(XmlPath);
      Assertion.AssertNotNull(stream, "stream != null");
      lifetime.AddDispose(stream);
      return stream;
    }
  }
}
