using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  public class FSharpObjectExpressionClass([NotNull] Class.IClassPart part)
    : FSharpClass(part), IFSharpRepresentationAccessRightsOwner, ILanguageSpecificDeclaredElement
  {
    protected override bool AcceptsPart(TypePart part) =>
      part is ObjectExpressionTypePart;

    // An easy way to hide the type from global navigation.
    public override bool IsSynthetic() => true;

    public AccessRights RepresentationAccessRights => AccessRights.PUBLIC;
    public bool IsErased => true;

    public override string ToString() => this.TestToString(BuildTypeParameterString());
  }
}
