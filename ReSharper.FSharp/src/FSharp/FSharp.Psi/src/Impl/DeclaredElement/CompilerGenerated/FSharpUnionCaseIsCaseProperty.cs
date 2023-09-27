using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  public class FSharpUnionCaseIsCaseProperty : FSharpGeneratedPropertyBase, IFSharpGeneratedFromUnionCase
  {
    [NotNull] internal IUnionCase UnionCase { get; }

    internal FSharpUnionCaseIsCaseProperty([NotNull] IUnionCase unionCase) =>
      UnionCase = unionCase;

    public override ITypeElement GetContainingType() => UnionCase.GetContainingType();
    public IClrDeclaredElement OriginElement => UnionCase;
    public bool IsReadOnly => false;

    public IDeclaredElementPointer<IFSharpGeneratedFromOtherElement> CreatePointer() =>
      new FSharpUnionCaseIsCasePropertyPointer(this);

    public override string ShortName =>
      OriginElement is IUnionCase unionCase
        ? "Is" + unionCase.ShortName
        : SharedImplUtil.MISSING_DECLARATION_NAME;

    public override IType Type => PredefinedType.Bool;

    public override AccessRights GetAccessRights() =>
      ContainingType.GetRepresentationAccessRights();

    public override bool IsValid() =>
      OriginElement.IsValid();

    public override bool Equals(object obj) =>
      obj is FSharpUnionCaseIsCaseProperty other && Equals(OriginElement, other.OriginElement);

    public override int GetHashCode() =>
      OriginElement.GetHashCode();
  }
}
