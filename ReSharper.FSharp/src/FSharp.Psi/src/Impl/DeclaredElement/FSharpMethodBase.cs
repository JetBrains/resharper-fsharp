using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Common.Util;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpMethodBase<TDeclaration> : FSharpFunctionBase<TDeclaration>, IMethod
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IAccessRightsOwnerDeclaration,
    IModifiersOwnerDeclaration
  {
    public const string ExtensionAttributeTypeName = "System.Runtime.CompilerServices.ExtensionAttribute";

    protected FSharpMethodBase([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv, [CanBeNull] IFSharpTypeDeclaration typeDeclaration)
      : base(declaration, mfv, typeDeclaration)
    {
      ShortName = mfv.GetMemberCompiledName();
    }

    public override DeclaredElementType GetElementType() => CLRDeclaredElementType.METHOD;

    public override string ShortName { get; }

    public bool IsExtensionMethod => FSharpSymbol.Attributes.HasAttributeInstance(ExtensionAttributeTypeName);

    public bool IsAsync => false;
    public bool IsVarArg => false;
    public bool IsXamlImplicitMethod => false;
  }
}