using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public enum ChangeNameKind
  {
    // Remove compiled name if present and update all references.
    UseSingleName,

    // Change the name seen by F# source only and update F# references.
    SourceName,

    // Change the name seen by other languages and update non-F# references.
    CompiledName
  }

  public partial interface IFSharpDeclaration : IDeclaration
  {
    /// <summary>
    /// May take long time due to waiting for FCS. Symbol is cached in declaration.
    /// </summary>
    [CanBeNull]
    FSharpSymbol GetFSharpSymbol();

    [NotNull]
    string ShortName { get; }

    [NotNull]
    string SourceName { get; }

    void SetName(string name, ChangeNameKind changeNameKind);
  }
}