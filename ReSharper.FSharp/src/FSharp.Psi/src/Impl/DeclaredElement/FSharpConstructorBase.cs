using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpConstructorBase<TDeclaration> : FSharpFunctionBase<TDeclaration>, IConstructor
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    protected FSharpConstructorBase([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    public override DeclaredElementType GetElementType() =>
      CLRDeclaredElementType.CONSTRUCTOR;

    public override bool IsStatic => false;
    public override IType ReturnType => Module.GetPredefinedType().Void;

    public abstract bool IsImplicit { get; }

    public bool IsDefault => false;
    public bool IsParameterless => Parameters.IsEmpty();

    public override bool Equals(object obj) => 
      obj is IConstructor && base.Equals(obj);

    public override int GetHashCode() => ShortName.GetHashCode();
  }

  internal class FSharpSecondaryConstructor : FSharpConstructorBase<SecondaryConstructorDeclaration>
  {
    public FSharpSecondaryConstructor([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    public override bool IsImplicit => false;
  }

  internal class FSharpPrimaryConstructor : FSharpConstructorBase<PrimaryConstructorDeclaration>
  {
    public FSharpPrimaryConstructor([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    public override bool IsImplicit => true;
  }
}
