using System.Collections.Generic;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal abstract class FSharpClassLikePart<T> : FSharpTypeParametersOwnerPart<T>,
    ClassLikeTypeElement.IClassLikePart where T : class, IFSharpTypeDeclaration, ITypeDeclaration
  {
    protected FSharpClassLikePart(T declaration, MemberDecoration memberDecoration,
      TreeNodeCollection<ITypeParameterOfTypeDeclaration> typeParameters, ICacheBuilder cacheBuilder)
      : base(declaration, memberDecoration, typeParameters, cacheBuilder)
    {
    }

    protected FSharpClassLikePart(IReader reader) : base(reader)
    {
    }

    public virtual IEnumerable<ITypeMember> GetTypeMembers()
    {
      // todo: ask members from FCS and check `IsCompilerGenerated`
      return GetDeclaration()?.MemberDeclarations.Select(d => d.DeclaredElement).WhereNotNull() ??
             EmptyList<ITypeMember>.InstanceList;
    }

    public virtual IEnumerable<IDeclaredType> GetSuperTypes()
    {
      // todo: override in class and ask FCS without getting declaration
      return GetDeclaration()?.SuperTypes ?? EmptyList<IDeclaredType>.Instance;
    }

    public virtual IDeclaredType GetBaseClassType()
    {
      // todo: override in class and ask FCS without getting declaration
      return GetDeclaration()?.BaseClassType ?? GetPsiModule().GetPredefinedType().Object;
    }
  }
}