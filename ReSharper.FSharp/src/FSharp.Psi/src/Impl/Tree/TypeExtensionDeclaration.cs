using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

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

    public IFSharpIdentifier FSharpIdentifier => Identifier;

    IFSharpReferenceOwner IFSharpReferenceOwner.SetName(string name) =>
      FSharpImplUtil.SetName(this, name);

    public bool IsQualified => QualifierReferenceName != null;
    public FSharpSymbolReference QualifierReference => QualifierReferenceName?.Reference;

    public IList<string> Names
    {
      get
      {
        var qualifierReferenceName = QualifierReferenceName;
        if (qualifierReferenceName == null)
          return new[] {SourceName};

        var names = qualifierReferenceName.Names.AsList();
        names.Add(SourceName);
        return names;
      }
    }

    public void SetQualifier(IClrDeclaredElement declaredElement) => 
      this.SetQualifier(this.CreateElementFactory().CreateTypeReferenceName, declaredElement);

    public bool IsPrimary =>
      TypeKeyword?.GetTokenType() == FSharpTokenType.TYPE;
  }
}
