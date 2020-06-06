using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TypeReferenceName
  {
    public override IFSharpIdentifier FSharpIdentifier => Identifier;
    public string ShortName => FSharpIdentifier?.Name ?? SharedImplUtil.MISSING_DECLARATION_NAME;
    public string QualifiedName => this.GetQualifiedName();
    public IList<string> Names => this.GetNames();

    public bool IsQualified => Qualifier != null;
    public FSharpSymbolReference QualifierReference => Qualifier?.Reference;

    public void SetQualifier(IClrDeclaredElement declaredElement)
    {
      // todo
    }
  }

  internal partial class ExpressionReferenceName
  {
    public override IFSharpIdentifier FSharpIdentifier => Identifier;

    protected override FSharpSymbolReference CreateReference() =>
      new FSharpSymbolReference(this);

    public string ShortName => FSharpIdentifier?.Name ?? SharedImplUtil.MISSING_DECLARATION_NAME;
    public string QualifiedName => this.GetQualifiedName();
    public IList<string> Names => this.GetNames();

    public bool IsQualified => Qualifier != null;
    public FSharpSymbolReference QualifierReference => Qualifier?.Reference;

    public void SetQualifier(IClrDeclaredElement declaredElement)
    {
      // todo
    }
  }
}
