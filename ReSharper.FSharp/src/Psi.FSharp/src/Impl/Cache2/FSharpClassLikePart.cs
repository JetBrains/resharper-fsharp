using System.Collections.Generic;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  public abstract class FSharpClassLikePart<TDeclaration> : FSharpTypePart<TDeclaration>,
    ClassLikeTypeElement.IClassLikePart where TDeclaration : class, ITypeDeclaration
  {
    protected FSharpClassLikePart(IReader reader) : base(reader)
    {
    }

    protected FSharpClassLikePart(TDeclaration declaration, string shortName, MemberDecoration memberDecoration,
      int typeParameters = 0) : base(declaration, shortName, memberDecoration, typeParameters)
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
      var entity = (GetDeclaration() as IFSharpTypeDeclaration)?.Symbol as FSharpEntity;
      return entity != null ? FSharpElementsUtil.GetBaseType(entity, GetPsiModule()) : null;
    }
  }
}