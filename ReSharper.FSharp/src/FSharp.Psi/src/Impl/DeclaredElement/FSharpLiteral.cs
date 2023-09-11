using System.Collections.Generic;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpLiteral : FSharpTypeMember<TopPatternDeclarationBase>, IField, ITopLevelPatternDeclaredElement
  {
    public FSharpLiteral([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    [CanBeNull] public FSharpMemberOrFunctionOrValue Mfv => Symbol as FSharpMemberOrFunctionOrValue;

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.CONSTANT;

    private IList<FSharpAttribute> Attributes =>
      Mfv?.Attributes ?? EmptyList<FSharpAttribute>.Instance;

    public override IList<IAttributeInstance> GetAttributeInstances(AttributesSource attributesSource) =>
      Attributes.ToAttributeInstances(Module);

    public override IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, AttributesSource attributesSource) =>
      Attributes.GetAttributes(clrName).ToAttributeInstances(Module);

    public override bool HasAttributeInstance(IClrTypeName clrName, AttributesSource attributesSource) =>
      Attributes.HasAttributeInstance(clrName.FullName);

    public override bool IsStatic => true;

    public IType Type => GetType(Mfv?.FullType);

    public ConstantValue ConstantValue =>
      Mfv is { } mfv
        ? ConstantValue.Create(mfv.LiteralValue.Value, Type)
        : ConstantValue.BAD_VALUE;

    public bool IsField => false;
    public bool IsConstant => true;
    public bool IsEnumMember => false;
    public bool IsRequired => false;
    public ReferenceKind ReferenceKind => ReferenceKind.VALUE;
    public int? FixedBufferSize => null;
  }
}
