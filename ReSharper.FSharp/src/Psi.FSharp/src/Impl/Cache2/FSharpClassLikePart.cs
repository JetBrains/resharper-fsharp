using System.Collections.Generic;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  public abstract class FSharpClassLikePart<TDeclaration> : FSharpTypePart<TDeclaration>,
    ClassLikeTypeElement.IClassLikePart where TDeclaration : class, ITypeDeclaration
  {
    protected FSharpClassLikePart(IReader reader) : base(reader)
    {
    }

    protected FSharpClassLikePart(TDeclaration declaration, string shortName, int typeParameters)
      : base(declaration, shortName, typeParameters)
    {
    }

    public IEnumerable<ITypeMember> GetTypeMembers()
    {
      return GetDeclaration()?.MemberDeclarations.Select(d => d.DeclaredElement).WhereNotNull() ??
             EmptyList<ITypeMember>.InstanceList;
    }

    public IEnumerable<IDeclaredType> GetSuperTypes()
    {
      return EmptyList<IDeclaredType>.Instance;

//      var decl = GetDeclaration() as IFSharpTypeDeclaration;
//      return decl != null ? decl.SuperTypes : EmptyList<IDeclaredType>.Instance;
    }
  }
}