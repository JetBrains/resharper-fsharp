using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  public class FSharpUnionCaseIsCaseProperty : FSharpGeneratedPropertyBase, IFSharpGeneratedFromUnionCase
  {
    [NotNull] internal IFSharpUnionCase UnionCase { get; }

    internal FSharpUnionCaseIsCaseProperty([NotNull] IFSharpUnionCase unionCase) =>
      UnionCase = unionCase;

    public override ITypeElement GetContainingType() => UnionCase.ContainingType;
    public IClrDeclaredElement OriginElement => UnionCase;

    public IDeclaredElementPointer<IFSharpGeneratedFromOtherElement> CreatePointer() =>
      new FSharpUnionCaseIsCasePropertyPointer(this);

    public override string ShortName =>
      OriginElement is IFSharpUnionCase unionCase
        ? "Is" + unionCase.ShortName
        : SharedImplUtil.MISSING_DECLARATION_NAME;

    public override IType Type => PredefinedType.Bool;

    public override AccessRights GetAccessRights() =>
      ContainingType.GetRepresentationAccessRights();

    public override string SourceName => ShortName;

    public override bool IsValid() =>
      UnionCase.IsValid();

    public override bool Equals(object obj) =>
      obj is FSharpUnionCaseIsCaseProperty other && Equals(OriginElement, other.OriginElement);

    public override int GetHashCode() =>
      UnionCase.GetHashCode();
  }
}
