using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpConstructorBase<TDeclaration> : FSharpFunctionBase<TDeclaration>, IConstructor
    where TDeclaration : IFSharpDeclaration, IModifiersOwnerDeclaration, ITypeMemberDeclaration
  {
    protected FSharpConstructorBase([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv) : base(declaration, mfv)
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

  internal class FSharpConstructor : FSharpConstructorBase<ConstructorDeclaration>
  {
    public FSharpConstructor([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv) : base(declaration, mfv)
    {
    }

    public override bool IsImplicit => false;
  }

  internal class FSharpImplicitConstructor : FSharpConstructorBase<ImplicitConstructorDeclaration>
  {
    public FSharpImplicitConstructor([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv) : base(declaration, mfv)
    {
    }

    public override bool IsImplicit => true;
  }
}
