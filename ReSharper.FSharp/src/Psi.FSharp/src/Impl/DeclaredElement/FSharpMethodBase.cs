using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util.Extension;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement
{
  internal abstract class FSharpMethodBase<TDeclaration> : FSharpFunctionBase<TDeclaration>, IMethod
    where TDeclaration : FSharpDeclarationBase, IFSharpDeclaration, IAccessRightsOwnerDeclaration,
    IModifiersOwnerDeclaration
  {
    protected FSharpMethodBase([NotNull] ITypeMemberDeclaration declaration,
      [NotNull] FSharpMemberOrFunctionOrValue mfv, [CanBeNull] IFSharpTypeDeclaration typeDeclaration)
      : base(declaration, mfv, typeDeclaration)
    {
      ShortName = mfv.GetMemberCompiledName();
    }

    public override DeclaredElementType GetElementType()
    {
      return CLRDeclaredElementType.METHOD;
    }

    public override string ShortName { get; }

    public bool IsExtensionMethod => FSharpSymbol.Attributes.Any(a =>
      a.AttributeType.QualifiedName.SubstringBefore(",", StringComparison.Ordinal)
        .Equals("System.Runtime.CompilerServices.ExtensionAttribute", StringComparison.Ordinal));

    public bool IsAsync => false;
    public bool IsVarArg => false;
    public bool IsXamlImplicitMethod => false;
  }
}