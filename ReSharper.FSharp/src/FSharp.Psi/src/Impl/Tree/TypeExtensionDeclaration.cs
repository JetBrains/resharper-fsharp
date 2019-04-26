using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TypeExtensionDeclaration : IFSharpTypeParametersOwnerDeclaration, IReferenceExpression
  {
    [CanBeNull] private TypeAugmentation myTypeAugmentation;
    public FSharpSymbolReference Reference { get; set; }

    public override IFSharpIdentifier NameIdentifier => LongIdentifier;
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
        lock (this)
        {
          return myTypeAugmentation ?? (myTypeAugmentation = FSharpImplUtil.IsTypePartDeclaration(this));
        }
      }
    }

    public bool IsTypePartDeclaration => TypeAugmentation.IsTypePart;
    public override PartKind TypePartKind => TypeAugmentation.PartKind;

    public bool IsTypeExtensionAllowed =>
      GetContainingNode<IModuleDeclaration>() != null;

    public ITokenNode IdentifierToken => LongIdentifier?.IdentifierToken;

    IReferenceExpression IReferenceExpression.SetName(string name)
    {
      if (IdentifierToken is var id && id != null)
        LowLevelModificationUtil.ReplaceChildRange(id, id, new FSharpIdentifierToken(name));

      return this;
    }
  }
}
