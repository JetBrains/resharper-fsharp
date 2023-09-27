using FSharp.Compiler.Symbols;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class MemberSignature
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
  }
}
