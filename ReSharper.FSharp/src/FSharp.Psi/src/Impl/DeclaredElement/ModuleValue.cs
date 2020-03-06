using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class ModuleValue : FSharpPropertyBase<TopPatternDeclarationBase>, IMutableModifierOwner
  {
    public ModuleValue([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv) : base(declaration, mfv)
    {
    }

    public bool IsMutable =>
      GetDeclaration() is ITopReferencePat referencePat && referencePat.IsMutable;

    public void SetIsMutable(bool value)
    {
      foreach (var declaration in GetDeclarations())
        if (declaration is ITopReferencePat referencePat)
          referencePat.SetIsMutable(true);
    }

    public bool CanBeMutable =>
      GetDeclaration() is ITopReferencePat referencePat && referencePat.CanBeMutable;
  }
}
