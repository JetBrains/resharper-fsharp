using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement.CompilerGenerated
{
  public abstract class FSharpGeneratedConstructor(TypePart typePart) : FSharpGeneratedFunctionBase, IConstructor
  {
    [NotNull] protected readonly TypePart TypePart = typePart;

    public override string ShortName => StandardMemberNames.Constructor;

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.CONSTRUCTOR;

    protected override IClrDeclaredElement ContainingElement => TypePart.TypeElement;
    public override ITypeElement GetContainingType() => (ITypeElement) ContainingElement;
    public override ITypeMember GetContainingTypeMember() => (ITypeMember) ContainingElement;

    public override IType ReturnType => PredefinedType.Void;

    public override AccessRights GetAccessRights() =>
      ContainingType.GetRepresentationAccessRights();

    public bool IsDefault => false;
    public bool IsParameterless => false;
    public bool IsImplicit => true;
    public bool IsValueTypeZeroInit => false;
  }
}
