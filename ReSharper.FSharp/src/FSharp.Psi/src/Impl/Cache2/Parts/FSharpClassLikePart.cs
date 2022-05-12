using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal abstract class FSharpClassLikePart<T> : FSharpTypeParametersOwnerPart<T>, IFSharpClassLikePart
    where T : class, IFSharpTypeOldDeclaration
  {
    private bool? myHasPublicDefaultCtor;

    protected FSharpClassLikePart([NotNull] T declaration, MemberDecoration memberDecoration,
      IList<ITypeParameterDeclaration> typeParameters, [NotNull] ICacheBuilder cacheBuilder)
      : base(declaration, memberDecoration, typeParameters, cacheBuilder)
    {
    }

    protected FSharpClassLikePart(IReader reader) : base(reader)
    {
    }

    public virtual IEnumerable<ITypeMember> GetTypeMembers()
    {
      var declaration = GetDeclaration();
      if (declaration == null)
        return EmptyList<ITypeMember>.Instance;

      var result = new LocalList<ITypeMember>();
      foreach (var memberDeclaration in declaration.MemberDeclarations)
      {
        var declaredElement = memberDeclaration.DeclaredElement;
        if (declaredElement != null)
          result.Add(declaredElement);
      }

      return result.ResultingList();
    }

    public abstract IEnumerable<IDeclaredType> GetSuperTypes();
    public virtual IEnumerable<ITypeElement> GetSuperTypeElements() => GetSuperTypes().AsIList().ToTypeElements();

    public virtual MemberPresenceFlag GetMemberPresenceFlag() =>
      MemberPresenceFlag.SIGN_OR_EQUALITY_OP | MemberPresenceFlag.EXPLICIT_OP |
      MemberPresenceFlag.MAY_EQUALS_OVERRIDE |

      // RIDER-10263
      (HasPublicDefaultCtor ? MemberPresenceFlag.PUBLIC_DEFAULT_CTOR : MemberPresenceFlag.NONE);

    public virtual IDeclaredType GetBaseClassType() =>
      ExtendsListShortNames.IsEmpty()
        ? null
        : GetDeclaration()?.BaseClassType ?? GetPsiModule().GetPredefinedType().Object;

    public bool HasPublicDefaultCtor
    {
      get
      {
        lock (this)
        {
          if (myHasPublicDefaultCtor != null)
            return myHasPublicDefaultCtor.Value;

          myHasPublicDefaultCtor = false;
          var declaration = GetDeclaration();
          if (declaration == null)
            return false;

          foreach (var memberDeclaration in declaration.MemberDeclarations)
          {
            if (memberDeclaration is IConstructorDeclaration)
            {
              // todo: analyze tree and don't get declared elements here
              if (memberDeclaration.DeclaredElement is IConstructor { IsParameterless: true } ctor &&
                  ctor.GetAccessRights() == AccessRights.PUBLIC)
              {
                myHasPublicDefaultCtor = true;
                return true;
              }
            }
          }

          myHasPublicDefaultCtor = false;
          return false;
        }
      }
    }
  }
}
