using System;
using System.Linq;
using System.Text;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
{
  public static class XmlDocTemplateUtil
  {
    public static (string, int caretOffset) GetDocTemplate(IDocCommentBlock docCommentBlock,
      Func<int, string> linePrefix)
    {
      var text = new StringBuilder();
      var line = 1;

      text.Append("summary>");
      text.Append($"{linePrefix(line++)}");

      var caretOffset = text.Length;

      text.Append($"{linePrefix(line++)}</summary>");

      foreach (var parameter in FSharpParameterUtil.GetParametersGroupNames(docCommentBlock.Parent)
                 .SelectMany(t => t)
                 .Where(t => t != SharedImplUtil.MISSING_DECLARATION_NAME))

        text.Append($"{linePrefix(line++)}<param name=\"{parameter}\"></param>");

      return (text.ToString(), caretOffset);
    }
  }
}
