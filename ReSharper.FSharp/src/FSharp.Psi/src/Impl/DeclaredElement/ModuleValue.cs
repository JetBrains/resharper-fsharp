using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class ModuleValue : FSharpPropertyBase<TopPatternDeclarationBase>, IMutableModifierOwner
  {
    public ModuleValue([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    public bool IsMutable =>
      GetDeclaration() is IMutableModifierOwner { IsMutable: true };

    public void SetIsMutable(bool value)
    {
      foreach (var declaration in GetDeclarations())
        if (declaration is IMutableModifierOwner mutableModifierOwner)
          mutableModifierOwner.SetIsMutable(true);
    }

    public bool CanBeMutable =>
      GetDeclaration() is IMutableModifierOwner { CanBeMutable: true };

    public override bool IsStatic => true;

    public override bool IsReadable => true;
    public override bool IsWritable => Mfv?.IsMutable ?? false;
  }
}
