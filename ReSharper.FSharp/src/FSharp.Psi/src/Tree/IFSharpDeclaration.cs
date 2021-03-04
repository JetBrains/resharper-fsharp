using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

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

  public interface IFSharpDeclaration : INameIdentifierOwner, IDeclaration
  {
    /// May take long time due to waiting for FCS. Symbol is cached in declaration.
    [CanBeNull]
    FSharpSymbol GetFSharpSymbol();

    [CanBeNull]
    FSharpSymbolUse GetFSharpSymbolUse();
    
    /// Name used in F# source code.
    [NotNull] string SourceName { get; }

    /// Name used in compiled assemblies.
    [NotNull] string CompiledName { get; }

    void SetName(string name, ChangeNameKind changeNameKind);

    TreeTextRange GetNameIdentifierRange();
  }

  public interface IFSharpTypeParametersOwnerDeclaration : IFSharpDeclaration
  {
    // todo: check interface implementation
    TreeNodeCollection<ITypeParameterOfTypeDeclaration> TypeParameters { get; }
  }
}
