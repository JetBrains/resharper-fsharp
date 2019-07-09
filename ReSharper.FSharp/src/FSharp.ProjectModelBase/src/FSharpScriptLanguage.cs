using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

// ReSharper disable once CheckNamespace
namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  [LanguageDefinition(Name)]
  public class FSharpScriptLanguage : FSharpLanguage
  {
    public new const string Name = "F# Script";

    [CanBeNull, UsedImplicitly]
    public new static FSharpScriptLanguage Instance { get; private set; }

    public FSharpScriptLanguage() : base(Name)
    {
    }
  }
}
