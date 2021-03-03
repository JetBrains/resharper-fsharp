using FSharp.Compiler.Symbols;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class MemberSignature
  {
    public override IFSharpIdentifierLikeNode NameIdentifier => (IFSharpIdentifierLikeNode) Identifier;
    protected override string DeclaredElementName => NameIdentifier.GetCompiledName(Attributes);

    protected override IDeclaredElement CreateDeclaredElement()
    {
      if (Type is IFunctionTypeUsage)
        return this.CreateMethod();

      return GetFSharpSymbol() is { } fcsSymbol
        ? CreateDeclaredElement(fcsSymbol)
        : null;
    }

    protected override IDeclaredElement CreateDeclaredElement(FSharpSymbol fcsSymbol) =>
      this.CreateMemberDeclaredElement(fcsSymbol);

    public bool IsIndexer => this.IsIndexer();

    public override bool IsStatic => StaticKeyword != null;
    public override bool IsVirtual => MemberKeyword?.GetTokenType() == FSharpTokenType.DEFAULT;
    public override bool IsOverride => this.IsOverride();
  }
}
