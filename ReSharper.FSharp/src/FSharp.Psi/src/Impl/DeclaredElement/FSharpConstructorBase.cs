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
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IAccessRightsOwnerDeclaration,
    IModifiersOwnerDeclaration
  {
    protected FSharpConstructorBase([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv, IFSharpTypeDeclaration typeDeclaration)
      : base(declaration, mfv, typeDeclaration)
    {
    }

    public override DeclaredElementType GetElementType()
    {
      return CLRDeclaredElementType.CONSTRUCTOR;
    }

    public override string ShortName => GetContainingType()?.ShortName ?? SharedImplUtil.MISSING_DECLARATION_NAME;
    public bool IsDefault => false;
    public override bool IsStatic => false;
    public bool IsParameterless => Parameters.IsEmpty();
    public abstract bool IsImplicit { get; }
  }
}