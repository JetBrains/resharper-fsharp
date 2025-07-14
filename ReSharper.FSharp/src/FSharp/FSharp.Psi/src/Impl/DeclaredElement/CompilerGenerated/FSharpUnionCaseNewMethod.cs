using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  public class FSharpUnionCaseNewMethod([NotNull] IUnionCase unionCase)
    : FSharpGeneratedMethodBase, IFSharpGeneratedFromUnionCase
  {
    [NotNull] internal IUnionCase UnionCase { get; } = unionCase;

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

    public override IList<IParameter> Parameters
    {
      get
      {
        var fields = UnionCase.CaseFields;
        var result = new IParameter[fields.Count];
        for (var i = 0; i < fields.Count; i++)
          result[i] = new FSharpGeneratedParameter(this, fields[i], true);

        return result;
      }
    }

    public override AccessRights GetAccessRights() =>
      ContainingType.GetRepresentationAccessRights();

    public override bool IsValid() =>
      UnionCase.IsValid();

    public override bool Equals(object obj) =>
      obj is FSharpUnionCaseNewMethod other && Equals(OriginElement, other.OriginElement);

    public override int GetHashCode() =>
      UnionCase.GetHashCode();
  }
}
