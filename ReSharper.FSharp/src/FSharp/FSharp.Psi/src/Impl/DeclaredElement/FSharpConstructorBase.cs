using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpConstructorBase<TDeclaration>([NotNull] ITypeMemberDeclaration declaration)
    : FSharpFunctionBase<TDeclaration>(declaration), IConstructor
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.CONSTRUCTOR;

    public override bool IsStatic => false;
    public override IType ReturnType => Module.GetPredefinedType().Void;

    public abstract bool IsImplicit { get; }
    public bool IsValueTypeZeroInit => false;

    public bool IsDefault => false;
    public bool IsParameterless => Parameters.IsEmpty();

    public override bool Equals(object obj) =>
      obj is IConstructor && base.Equals(obj);

    public override int GetHashCode() => ShortName.GetHashCode();
  }

  internal class FSharpSecondaryConstructor([NotNull] ITypeMemberDeclaration declaration)
    : FSharpConstructorBase<IConstructorSignatureOrDeclaration>(declaration)
  {
    public override bool IsImplicit => false;
  }

  internal class FSharpPrimaryConstructor([NotNull] ITypeMemberDeclaration declaration)
    : FSharpConstructorBase<PrimaryConstructorDeclaration>(declaration)
  {
    public override bool IsImplicit => true;
  }
}
