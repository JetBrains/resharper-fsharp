using System.Linq;
using System.Text;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
{
  public static class XmlDocTemplateUtil
  {
    public static (string, int caretOffset) GetDocTemplate(IDocCommentBlock docCommentBlock, string linePrefix,
      string lineEnding)
    {
      var text = new StringBuilder();

      text.Append($"{linePrefix}<summary>{lineEnding}");
      text.Append(linePrefix + lineEnding);

      var caretOffset = text.Length;

      text.Append($"{linePrefix}</summary>");

      foreach (var parameter in FSharpParameterUtil.GetParametersGroupNames(docCommentBlock.Parent)
                 .SelectMany(t => t)
                 .Select(t => t.name)
                 .Where(t => t != SharedImplUtil.MISSING_DECLARATION_NAME))
        text.Append($"{lineEnding}{linePrefix}<param name=\"{parameter}\"></param>");

      return (text.ToString(), caretOffset);
    }
  }
}
