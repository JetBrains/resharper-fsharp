using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
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

    public override string ShortName =>
      GetContainingType()?.ShortName ??
      SharedImplUtil.MISSING_DECLARATION_NAME;

    public override bool IsStatic => false;
    public override IType ReturnType => Module.GetPredefinedType().Void;

    public abstract bool IsImplicit { get; }

    public bool IsDefault => false;
    public bool IsParameterless => Parameters.IsEmpty();
  }

  internal class FSharpConstructor : FSharpConstructorBase<MemberConstructorDeclaration>
  {
    public FSharpConstructor([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    public override bool IsImplicit => false;
  }

  internal class FSharpImplicitConstructor : FSharpConstructorBase<PrimaryConstructorDeclaration>
  {
    public FSharpImplicitConstructor([NotNull] ITypeMemberDeclaration declaration) : base(declaration)
    {
    }

    public override bool IsImplicit => true;
  }
}
