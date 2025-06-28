using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal class FSharpExtensionMemberThisParameter([NotNull] IMemberSelfId decl)
    : FSharpMethodParameterBase<IMemberSelfId>(decl)
  {
    public override IParametersOwner Owner =>
      MemberDeclarationNavigator.GetBySelfId(GetDeclaration()) is IParameterOwnerMemberDeclaration memberDeclaration
        ? memberDeclaration.DeclaredElement as IParametersOwner
        : null;

    public override string ShortName => "this"; // todo: calc from member self id
    public override int Index => 0;
    public override IType Type => FcsSymbolMappingUtil.GetThisParameterType(Owner);

    public override ParameterKind Kind => ParameterKind.VALUE;
    public override DefaultValue GetDefaultValue() => DefaultValue.BAD_VALUE;

    public override IList<IAttributeInstance> GetAttributeInstances(AttributesSource source) =>
      EmptyList<IAttributeInstance>.Instance;

    public override IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, AttributesSource source) =>
      EmptyList<IAttributeInstance>.Instance;

    public override bool HasAttributeInstance(IClrTypeName clrName, AttributesSource attributesSource) => false;

    public override bool IsParams => false;
    public override bool IsParameterArray => false;
    public override bool IsParameterCollection => false;
    public override bool IsOptional => false;
  }
}
