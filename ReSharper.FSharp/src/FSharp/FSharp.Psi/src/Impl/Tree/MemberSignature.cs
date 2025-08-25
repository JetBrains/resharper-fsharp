using System.Collections.Generic;
using FSharp.Compiler.Symbols;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class MemberSignatureStub
  {
    public override IFSharpIdentifier NameIdentifier => (IFSharpIdentifier) Identifier;
    protected override string DeclaredElementName => NameIdentifier.GetCompiledName(Attributes);

    protected override IDeclaredElement CreateDeclaredElement()
    {
      if (Parent is IMemberConstraint)
        return null;

      if (ReturnTypeInfo is { ReturnType: IFunctionTypeUsage })
        return this.CreateMethod();

      return GetFcsSymbol() is { } fcsSymbol
        ? CreateDeclaredElement(fcsSymbol)
        : null;
    }

    protected override IDeclaredElement CreateDeclaredElement(FSharpSymbol fcsSymbol) =>
      this.CreateMemberDeclaredElement(fcsSymbol);

    public bool IsIndexer => this.IsIndexer();

    public override bool IsStatic => StaticKeyword != null;
    public override bool IsVirtual => MemberKeyword?.GetTokenType() == FSharpTokenType.DEFAULT;
    public override bool IsOverride => this.IsOverride();

    public IFSharpParameterDeclaration GetParameterDeclaration(FSharpParameterIndex index) =>
      TypeUsage.GetParameterDeclaration(index);

    public IList<IList<IFSharpParameterDeclaration>> GetParameterDeclarations() =>
      TypeUsage.GetParameterDeclarations();
  }

  internal class MemberSignature : MemberSignatureStub
  {
    public override ITypeUsage SetTypeUsage(ITypeUsage typeUsage)
    {
      if (TypeUsage != null)
        return base.SetTypeUsage(typeUsage);

      var colon = ModificationUtil.AddChildAfter(Identifier, FSharpTokenType.COLON.CreateTreeElement());
      return ModificationUtil.AddChildAfter(colon, typeUsage);
    }
  }
}
