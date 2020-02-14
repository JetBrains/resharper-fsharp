using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TypeExtensionDeclaration : IFSharpTypeParametersOwnerDeclaration
  {
    [CanBeNull] private TypeAugmentation myTypeAugmentation;
    public FSharpSymbolReference Reference { get; protected set; }

    public override IFSharpIdentifierLikeNode NameIdentifier => Identifier;
    protected override string DeclaredElementName => TypeAugmentation.CompiledName;

    protected override void ClearCachedData()
    {
      base.ClearCachedData();
      myTypeAugmentation = null;
    }

    protected override void PreInit()
    {
      base.PreInit();
      Reference = new TypeExtensionReference(this);
    }

    public override ReferenceCollection GetFirstClassReferences() =>
      IsTypePartDeclaration
        ? ReferenceCollection.Empty
        : new ReferenceCollection(Reference);

    [NotNull]
    private TypeAugmentation TypeAugmentation
    {
      get
      {
        if (myTypeAugmentation != null)
          return myTypeAugmentation;

        lock (this)
          return myTypeAugmentation ??= FSharpImplUtil.GetTypeAugmentationInfo(this);
      }
    }

    public bool IsTypePartDeclaration => TypeAugmentation.IsTypePart;
    public override PartKind TypePartKind => TypeAugmentation.PartKind;

    public bool IsTypeExtensionAllowed =>
      ModuleDeclarationNavigator.GetByMember(TypeDeclarationGroupNavigator.GetByTypeDeclaration(this)) != null;

    public ITokenNode IdentifierToken => Identifier as ITokenNode;

    IFSharpReferenceOwner IFSharpReferenceOwner.SetName(string name) =>
      FSharpImplUtil.SetName(this, name);
  }
}
