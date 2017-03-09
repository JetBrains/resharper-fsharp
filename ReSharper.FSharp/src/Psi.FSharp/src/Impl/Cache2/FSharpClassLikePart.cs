using System.Collections.Generic;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  public abstract class FSharpClassLikePart<TDeclaration> : FSharpTypePart<TDeclaration>,
    ClassLikeTypeElement.IClassLikePart where TDeclaration : class, ITypeDeclaration
  {
    protected FSharpClassLikePart(TDeclaration declaration, string shortName, MemberDecoration memberDecoration,
      int typeParameters = 0) : base(declaration, shortName, memberDecoration, typeParameters)
    {
    }

    protected FSharpClassLikePart(IReader reader) : base(reader)
    {
    }

    public virtual IEnumerable<ITypeMember> GetTypeMembers()
    {
      return GetDeclaration()?.MemberDeclarations.Select(d => d.DeclaredElement).WhereNotNull() ??
             EmptyList<ITypeMember>.InstanceList;
    }

    public virtual IEnumerable<IDeclaredType> GetSuperTypes()
    {
      return (GetDeclaration() as IFSharpTypeDeclaration)?.SuperTypes ??
             EmptyList<IDeclaredType>.Instance;
    }

    public virtual IDeclaredType GetBaseClassType()
    {
      return (GetDeclaration() as IFSharpTypeDeclaration)?.BaseClassType ??
             GetPsiModule().GetPredefinedType().Object;
    }
  }
}