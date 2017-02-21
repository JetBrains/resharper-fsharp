using System.Collections.Generic;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal abstract class FSharpTypeElementDeclarationBase : FSharpCachedDeclarationBase, ITypeDeclaration,
    ITypeMemberDeclaration, IFSharpDeclaration
  {
    public FSharpSymbol Symbol { get; set; }

    ITypeMember ITypeMemberDeclaration.DeclaredElement => (ITypeMember) DeclaredElement;
    ITypeElement ITypeDeclaration.DeclaredElement => (ITypeElement) DeclaredElement;

    public ITypeDeclaration GetContainingTypeDeclaration()
    {
      return GetContainingNode<ITypeDeclaration>();
    }

    public IEnumerable<IDeclaredType> SuperTypes
    {
      get
      {
        var entity = Symbol as FSharpEntity;
        return entity != null
          ? FSharpElementsUtil.GetSuperTypes(entity, GetPsiModule())
          : EmptyList<IDeclaredType>.Instance;
      }
    }

    // todo
    public TreeNodeCollection<ITypeDeclaration> NestedTypeDeclarations => TreeNodeCollection<ITypeDeclaration>.Empty;
    public TreeNodeCollection<ITypeMemberDeclaration> MemberDeclarations => TreeNodeCollection<ITypeMemberDeclaration>.Empty;
    public IList<ITypeDeclaration> TypeDeclarations => EmptyList<ITypeDeclaration>.Instance;
    public string CLRName { get; }
  }
}