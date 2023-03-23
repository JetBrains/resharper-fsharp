using JetBrains.Diagnostics;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class NamedNamespaceDeclaration
  {
    public override string DeclaredName => QualifiedName;
    public string QualifiedName => FSharpImplUtil.GetQualifiedName(QualifierReferenceName, Identifier);
    public override string CompiledName => Identifier.GetCompiledName();

    public override IFSharpIdentifier NameIdentifier => Identifier;

    public DocumentRange GetDeclaredNameDocumentRange()
    {
      var qualification = QualifierReferenceName;
      if (qualification == null)
        return this.GetNameDocumentRange();

      var identifier = NameIdentifier;
      var qualifierRange = qualification.GetDocumentRange();

      return identifier != null
        ? qualifierRange.Join(Identifier.GetDocumentRange())
        : qualifierRange;
    }

    protected override void PreInit()
    {
      base.PreInit();
      CacheDeclaredElement = null;
    }

    public override IDeclaredElement DeclaredElement
    {
      get
      {
        Assertion.Assert(IsValid(), "Getting declared element from invalid declaration");
        Assertion.Assert(CacheDeclaredElement == null || CacheDeclaredElement.IsValid(),
          "myCacheDeclaredElement == null || myCacheDeclaredElement.IsValid()");
        return CacheDeclaredElement;
      }
    }

    public IDeclaredElement CacheDeclaredElement { get; set; }

    INamespace INamespaceDeclaration.DeclaredElement => DeclaredElement as INamespace;

    public void SetQualifiedName(string qualifiedName)
    {
      // todo
    }

    public bool IsRecursive => RecKeyword != null;

    public void SetIsRecursive(bool value)
    {
      if (!value)
        throw new System.NotImplementedException();

      ModuleOrNamespaceKeyword.NotNull().AddTokenAfter(FSharpTokenType.REC);
    }

  }
}
