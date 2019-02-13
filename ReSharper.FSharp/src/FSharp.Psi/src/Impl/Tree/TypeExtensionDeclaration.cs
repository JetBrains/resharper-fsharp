using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Common.Util;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TypeExtensionDeclaration
  {
    [CanBeNull] private TypeAugmentation myTypeAugmentation;

    public override IFSharpIdentifier NameIdentifier => (IFSharpIdentifier) Identifier;
    protected override string DeclaredElementName => TypeAugmentation.CompiledName;

    protected override void ClearCachedData()
    {
      base.ClearCachedData();
      myTypeAugmentation = null;
    }

    [NotNull]
    private TypeAugmentation TypeAugmentation
    {
      get
      {
        lock (this)
        {
          return myTypeAugmentation ?? (myTypeAugmentation = FSharpImplUtil.IsTypePartDeclaration(this));
        }
      }
    }

    public bool IsTypePartDeclaration => TypeAugmentation.IsTypePart;
    public override PartKind TypePartKind => TypeAugmentation.PartKind;

    public bool IsTypeExtensionAllowed =>
      GetContainingNode<IModuleLikeDeclaration>()?.IsModule ?? false;
  }
}
