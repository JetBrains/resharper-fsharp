using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts
{
  internal abstract class FSharpClassLikePart<T> : FSharpTypeParametersOwnerPart<T>,
    ClassLikeTypeElement.IClassLikePart where T : class, IFSharpTypeDeclaration
  {
    private bool? myHasPublicDefaultCtor;
    
    protected FSharpClassLikePart([NotNull] T declaration, MemberDecoration memberDecoration,
      TreeNodeCollection<ITypeParameterOfTypeDeclaration> typeParameters, [NotNull] ICacheBuilder cacheBuilder)
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

      var fsFile = (IFSharpFile) declaration.GetContainingFile().NotNull();
      foreach (var typeExtension in fsFile.GetTypeExtensions(ShortName))
      {
        foreach (var memberDeclaration in typeExtension.TypeMembers)
        {
          var declaredElement = memberDeclaration.DeclaredElement;
          if (declaredElement != null)
            result.Add(declaredElement);
        }
      }

      return result.ResultingList();
    }

    public virtual IEnumerable<IDeclaredType> GetSuperTypes() =>
      GetDeclaration()?.SuperTypes ?? EmptyList<IDeclaredType>.Instance;

    public virtual IDeclaredType GetBaseClassType() =>
      GetDeclaration()?.BaseClassType ?? GetPsiModule().GetPredefinedType().Object;

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
            if (memberDeclaration is IConstructorDeclaration || memberDeclaration is IImplicitConstructorDeclaration)
            {
              // todo: analyze tree and don't get declared elements here
              if (memberDeclaration.DeclaredElement is IConstructor ctor &&
                  ctor.IsParameterless && ctor.GetAccessRights() == AccessRights.PUBLIC)
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