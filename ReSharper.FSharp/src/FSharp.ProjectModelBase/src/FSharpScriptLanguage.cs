using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

// ReSharper disable once CheckNamespace
namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  [LanguageDefinition(Name)]
  public class FSharpScriptLanguage : FSharpLanguage
  {
    public new const string Name = "F# Script";

    [UsedImplicitly] public new static readonly FSharpScriptLanguage Instance;

    public FSharpScriptLanguage() : base(Name)
    {
    }
  }
}
