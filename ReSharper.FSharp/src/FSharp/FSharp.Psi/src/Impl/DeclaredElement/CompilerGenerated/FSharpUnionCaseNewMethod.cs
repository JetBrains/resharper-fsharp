using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  public class FSharpUnionCaseNewMethod([NotNull] IFSharpUnionCase unionCase)
    : FSharpGeneratedMethodBase, IFSharpGeneratedFromUnionCase, IFSharpParameterOwner
  {
    [NotNull] internal IFSharpUnionCase UnionCase { get; } = unionCase;

    public override ITypeElement GetContainingType() => UnionCase.ContainingType;
    public IClrDeclaredElement OriginElement => UnionCase;

    public IDeclaredElementPointer<IFSharpGeneratedFromOtherElement> CreatePointer() =>
      new FSharpUnionCaseNewMethodPointer(this);

    public override string ShortName => "New" + UnionCase.ShortName;

    public override IType ReturnType =>
      GetContainingType() is { } typeElement
        ? TypeFactory.CreateType(typeElement)
        : TypeFactory.CreateUnknownType(Module);

    public override bool IsStatic => true;

    public override IList<IParameter> Parameters =>
      ParameterGroup.Cast<IParameter>().ToList();

    public override AccessRights GetAccessRights() =>
      ContainingType.GetRepresentationAccessRights();

    public override string SourceName => UnionCase.ShortName;

    public override bool IsValid() =>
      UnionCase.IsValid();

    public override bool Equals(object obj) =>
      obj is FSharpUnionCaseNewMethod other && Equals(OriginElement, other.OriginElement);

    public override int GetHashCode() =>
      UnionCase.GetHashCode();

    public IList<IList<IFSharpParameter>> FSharpParameterGroups => [ParameterGroup];

    public IFSharpParameter GetParameter(FSharpParameterIndex index) =>
      this.GetFSharpParameter(index);

    private IList<IFSharpParameter> ParameterGroup =>
      UnionCase.CaseFields.Select( (f, i) => (IFSharpParameter)new FSharpGeneratedParameterFromUnionCaseField(this, f, i)).ToList();
  }
}
