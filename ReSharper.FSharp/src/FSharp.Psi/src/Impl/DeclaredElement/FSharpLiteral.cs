using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpLiteral : FSharpTypeMember<PatternDeclarationBase>, IField
  {
    public FSharpLiteral([NotNull] ITypeMemberDeclaration declaration, FSharpMemberOrFunctionOrValue mfv) :
      base(declaration)
    {
      Type = FSharpTypesUtil.GetType(mfv.FullType, declaration, Module) ??
             TypeFactory.CreateUnknownType(Module);
      ConstantValue = new ConstantValue(mfv.LiteralValue.Value, Type);
    }

    public override DeclaredElementType GetElementType() => CLRDeclaredElementType.CONSTANT;
    public override bool IsStatic => true;
    public override bool IsMember => false;

    public IType Type { get; }
    public ConstantValue ConstantValue { get; }
    public bool IsField => false;
    public bool IsConstant => true;
    public bool IsEnumMember => false;
    public int? FixedBufferSize => null;
  }
}