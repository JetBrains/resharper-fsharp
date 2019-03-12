using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Pointers;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Pointers;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  public class IsUnionCaseProperty : FSharpGeneratedPropertyBase, IFSharpGeneratedFromOtherElement
  {
    [NotNull] internal IUnionCase UnionCase { get; }

    internal IsUnionCaseProperty([NotNull] IUnionCase unionCase) =>
      UnionCase = unionCase;

    public override ITypeElement ContainingType => UnionCase.GetContainingType();
    public IClrDeclaredElement OriginElement => UnionCase;

    public IDeclaredElementPointer<IFSharpGeneratedFromOtherElement> CreatePointer() =>
      new IsUnionCasePropertyPointer(this);

    public override string ShortName =>
      OriginElement is IUnionCase unionCase
        ? "Is" + unionCase.ShortName
        : SharedImplUtil.MISSING_DECLARATION_NAME;

    public override IType Type => PredefinedType.Bool;

    public override AccessRights GetAccessRights() =>
      ContainingType.GetRepresentationAccessRights();
  }
}
