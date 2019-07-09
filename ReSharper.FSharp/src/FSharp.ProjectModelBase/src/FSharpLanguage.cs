using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

// ReSharper disable once CheckNamespace
namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  [LanguageDefinition(Name)]
  public class FSharpLanguage : KnownLanguage
  {
    public new const string Name = "F#";

    [CanBeNull, UsedImplicitly]
    public static FSharpLanguage Instance { get; private set; }

    public FSharpLanguage() : base(Name)
    {
    }

    protected FSharpLanguage([NotNull] string name) : base(name)
    {
    }

    protected FSharpLanguage([NotNull] string name, [NotNull] string presentableName) : base(name, presentableName)
    {
    }
  }
}
