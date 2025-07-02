using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  public class FSharpExtensionMemberThisParameter([NotNull] IParametersOwner owner)
    : FSharpMethodParameterBase(owner, FSharpParameterIndex.Zero)
  {
    public override string ShortName => "this"; // todo: calc from member self id

    public override ParameterKind Kind => ParameterKind.VALUE;
    public override DefaultValue GetDefaultValue() => DefaultValue.BAD_VALUE;

    public override IList<IAttributeInstance> GetAttributeInstances(AttributesSource source) =>
      EmptyList<IAttributeInstance>.Instance;

    public override IList<IAttributeInstance> GetAttributeInstances(IClrTypeName clrName, AttributesSource source) =>
      EmptyList<IAttributeInstance>.Instance;

    public override bool HasAttributeInstance(IClrTypeName clrName, AttributesSource attributesSource) => false;

    public override IType Type =>
      Owner is IFSharpMember { Mfv.ApparentEnclosingEntity: { } fcsEntity } &&
      fcsEntity.GetTypeElement(Module) is { } typeElement
        ? TypeFactory.CreateType(typeElement)
        : TypeFactory.CreateUnknownType(Module);

    public override bool IsParams => false;
    public override bool IsParameterArray => false;
    public override bool IsParameterCollection => false;
    public override bool IsOptional => false;
    
    public override bool Equals(object obj) =>
      obj is FSharpExtensionMemberThisParameter fsThisParam && Owner.Equals(fsThisParam.Owner);

    public override int GetHashCode() => Owner.GetHashCode();
  }
}
