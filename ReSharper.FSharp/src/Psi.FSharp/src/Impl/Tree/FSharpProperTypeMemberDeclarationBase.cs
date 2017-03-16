using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  public abstract class FSharpProperTypeMemberDeclarationBase : FSharpTypeMemberDeclarationBase,
    ICachedTypeMemberDeclaration
  {
    [NotNull]
    protected abstract IDeclaredElement CreateDeclaredElement();

    private static readonly Func<FSharpProperTypeMemberDeclarationBase, IDeclaredElement>
      DeclaredElementFactory = declaration => declaration.CreateDeclaredElement();

    public override IDeclaredElement DeclaredElement
    {
      get
      {
        this.AssertIsValid("Asking declared element from invalid declaration");
        var cache = GetPsiServices().Caches.SourceDeclaredElementsCache;
        return cache.GetOrCreateDeclaredElement(this, DeclaredElementFactory);
      }
    }
  }
}