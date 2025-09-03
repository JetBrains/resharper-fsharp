using System;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class ValFieldDeclaration
  {
    protected override string DeclaredElementName => NameIdentifier.GetSourceName();
    public override IFSharpIdentifier NameIdentifier => (IFSharpIdentifier) Identifier;

    protected override IDeclaredElement CreateDeclaredElement() => new FSharpValField(this);

    public override AccessRights GetAccessRights() => FSharpModifiersUtil.GetAccessRights(AccessModifier);

    public bool IsMutable => MutableKeyword != null;

    public void SetIsMutable(bool value)
    {
      if (value == IsMutable)
        return;

      if (value)
        ValKeyword.AddTokenAfter(FSharpTokenType.MUTABLE);
      else
        throw new NotImplementedException();
    }
  }
}
