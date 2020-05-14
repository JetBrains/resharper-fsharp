using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeAnnotations;
using JetBrains.ReSharper.Psi.Impl.Shared.InjectedPsi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.FSharp.Services.Cs.Injected
{
  [SolutionComponent]
  public class FSharpRegexNodeProvider : IInjectionNodeProvider
  {
    private readonly IFSharpMethodInvocationUtil myInvocationUtil;

    public FSharpRegexNodeProvider(ISolution solution)
    {
      myInvocationUtil = solution.GetComponent<LanguageManager>().GetService<IFSharpMethodInvocationUtil>(FSharpLanguage.Instance);
    }

    public bool Check(
      ITreeNode node,
      ILiteralsInjectionDataProvider injectedContext,
      out object data)
    {
      data = null;

      if (!(node.Parent is IFSharpExpression expr))
        return false;

      if (!(expr is IArgument argument))
        return false;

      var argumentsOwner = myInvocationUtil.GetArgumentsOwner(expr);
      if (argumentsOwner == null)
        return false;

      var param = argument.MatchingParameter?.Element;
      if (param?.ShortName != "pattern")
        return false;

      var fullTypeName = param.ContainingParametersOwner?.GetContainingType()?.GetClrName().FullName;

      // todo: check if this is actually from a System assembly
      if (fullTypeName != "System.Text.RegularExpressions.Regex")
        return false;

      var optionsArg = argumentsOwner.Arguments.FirstOrDefault(arg => arg.MatchingParameter?.Element.ShortName == "options");

      // todo: read the options argument if there is one
      data = new RegexOptions();

      return true;
    }

    public string GetPrefix(ITreeNode node, object data)
    {
      return null;
    }

    public string GetPostfix(ITreeNode node, object data)
    {
      return null;
    }

    public PsiLanguageType SupportedOriginalLanguage => FSharpLanguage.Instance;

    public string ProvidedLanguageID => "FsRegex";

    public string Summary => ".NET Regular Expressions in C# entities marked by [RegExpPattern] attribute";

    public string Description =>
      "Injects .NET Regular Expression in C# entities marked by [RegExpPattern] attribute (RegExp constructor, etc.)";

    public string Guid => "a68c2ca6-9622-11ea-bb37-0242ac130002";

    public string[] Words => null;

    public string[] Attributes =>
      new[]
      {
        RegexPatternAnnotationProvider.RegexPatternAttributeShortName
      };
  }
}
