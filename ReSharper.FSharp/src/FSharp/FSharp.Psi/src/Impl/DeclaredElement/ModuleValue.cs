using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class ModuleValue : FSharpPropertyBase<TopPatternDeclarationBase>, IFSharpMutableModifierOwner,
    ITopLevelPatternDeclaredElement
  {
    public ModuleValue([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    public bool IsMutable =>
      GetDeclaration() is IFSharpMutableModifierOwner { IsMutable: true };

    public void SetIsMutable(bool value)
    {
      foreach (var declaration in GetDeclarations())
        if (declaration is IFSharpMutableModifierOwner mutableModifierOwner)
          mutableModifierOwner.SetIsMutable(true);
    }

    public bool CanBeMutable =>
      GetDeclaration() is IFSharpMutableModifierOwner { CanBeMutable: true };

    public override bool IsStatic => true;

    public override bool IsReadable => true;
    public override bool IsWritable => Mfv?.IsMutable ?? false;
  }
}
