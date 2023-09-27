using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using JetBrains.Util.DataStructures;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class TopActivePatternCase : FSharpCachedTypeMemberBase<TopActivePatternCaseDeclaration>,
    IFSharpDeclaredElement, IActivePatternCase
  {
    public TopActivePatternCase(ITopActivePatternCaseDeclaration declaration) : base(declaration)
    {
    }

    public override DeclaredElementType GetElementType() => FSharpDeclaredElementType.ActivePatternCase;
    public override string ShortName => GetDeclaration()?.CompiledName ?? SharedImplUtil.MISSING_DECLARATION_NAME;

    public int Index => GetDeclaration()?.Index ?? -1;

    public IDeclaredElement ActivePattern =>
      GetDeclaration()?.GetContainingNode<IDeclaration>()?.DeclaredElement;

    public ITypeMember GetContainingTypeMember() =>
      (ITypeMember) GetContainingType();

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(this, obj))
        return true;

      if (!(obj is TopActivePatternCase activePatternCase))
        return false;

      if (Index != activePatternCase.Index)
        return false;

      return Equals(ActivePattern, activePatternCase.ActivePattern);
    }

    public override int GetHashCode() => ShortName.GetHashCode();


    public override IList<IDeclaration> GetDeclarations() =>
      GetDeclarations(ActivePattern?.GetDeclarations());

    public override IList<IDeclaration> GetDeclarationsIn(IPsiSourceFile sourceFile) =>
      GetDeclarations(ActivePattern?.GetDeclarationsIn(sourceFile));

    private IList<IDeclaration> GetDeclarations([CanBeNull] IList<IDeclaration> activePatternDecls) =>
      activePatternDecls?.SelectNotNull(decl =>
        decl is IFSharpDeclaration { NameIdentifier: IActivePatternId activePatternId }
          ? (IDeclaration) activePatternId.GetCase(Index)
          : null).AsList() ?? EmptyList<IDeclaration>.InstanceList;

    public override HybridCollection<IPsiSourceFile> GetSourceFiles() =>
      ActivePattern?.GetSourceFiles() ?? HybridCollection<IPsiSourceFile>.Empty;

    public override bool HasDeclarationsIn(IPsiSourceFile sourceFile) =>
      GetSourceFiles().Contains(sourceFile);
  }
}
