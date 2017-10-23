using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Files;

// ReSharper disable once CheckNamespace
namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  [LanguageDefinition(Name)]
  public class FSharpLanguage : KnownLanguage
  {
    public new const string Name = "F#";

    [UsedImplicitly]
    public static readonly FSharpLanguage Instance;

    public FSharpLanguage() : base(Name)
    {
    }

    protected FSharpLanguage([NotNull] string name) : base(name)
    {
    }

    protected FSharpLanguage([NotNull] string name, [NotNull] string presentableName) : base(name, presentableName)
    {
    }

    public override PsiLanguageCategories SupportedCategories => PsiLanguageCategories.Dominant;
  }
}