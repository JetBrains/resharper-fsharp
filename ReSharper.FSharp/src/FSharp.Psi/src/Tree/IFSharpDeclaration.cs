using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public enum ChangeNameKind
  {
    /// Remove compiled name if present and update all references.
    UseSingleName,

    /// Change the name seen by F# source only and update F# references.
    SourceName,

    /// Change the name seen by other languages and update non-F# references.
    CompiledName
  }

  public partial interface IFSharpDeclaration : INameIdentifierOwner, IDeclaration
  {
    /// May take long time due to waiting for FCS. Symbol is cached in declaration.
    [CanBeNull]
    FSharpSymbol GetFSharpSymbol();

    /// Name used in F# source code.
    [NotNull] string SourceName { get; }

    /// Name used in compiled assemblies.
    [NotNull] string CompiledName { get; }

    void SetName(string name, ChangeNameKind changeNameKind);

    TreeTextRange GetNameIdentifierRange();
  }

  public interface IFSharpTypeParametersOwnerDeclaration : IFSharpDeclaration
  {
    TreeNodeCollection<ITypeParameterOfTypeDeclaration> TypeParameters { get; }
  }
}
