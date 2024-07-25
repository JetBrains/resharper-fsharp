using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  public class FSharpExtensionMemberParameter : FSharpMethodParameterBase
  {
    public FSharpExtensionMemberParameter([NotNull] IParametersOwner owner, [NotNull] IType type)
      : base(owner, 0, type)
    {
    }

    public override string ShortName => "this"; // todo: calc from member self id

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
