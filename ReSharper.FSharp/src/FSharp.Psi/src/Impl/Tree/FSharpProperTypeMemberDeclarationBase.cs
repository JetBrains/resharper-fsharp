using System;
using System.Diagnostics;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public abstract class FSharpProperTypeMemberDeclarationBase : FSharpTypeMemberDeclarationBase,
    ICachedTypeMemberDeclaration
  {
    [CanBeNull]
    protected abstract IDeclaredElement CreateDeclaredElement();

    protected virtual IDeclaredElement CreateDeclaredElement([NotNull] FSharpSymbol fcsSymbol) =>
      CreateDeclaredElement();

    private static readonly Func<FSharpProperTypeMemberDeclarationBase, IDeclaredElement>
      DeclaredElementFactory = declaration => declaration.CreateDeclaredElement();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public override IDeclaredElement DeclaredElement =>
      GetOrCreateDeclaredElement(DeclaredElementFactory);

    [CanBeNull]
    public IDeclaredElement GetOrCreateDeclaredElement([NotNull] FSharpSymbol fcsSymbol) =>
      GetOrCreateDeclaredElement(declaration => declaration.CreateDeclaredElement(fcsSymbol));

    private IDeclaredElement GetOrCreateDeclaredElement(
      Func<FSharpProperTypeMemberDeclarationBase, IDeclaredElement> factory)
    {
      this.AssertIsValid("Asking declared element from invalid declaration");
      var cache = GetPsiServices().Caches.SourceDeclaredElementsCache;
      // todo: calc types on demand in members (move cookie to FSharpTypesUtil)
      using (CompilationContextCookie.GetOrCreate(GetPsiModule().GetContextFromModule()))
        return cache.GetOrCreateDeclaredElement(this, factory);
    }
  }
}
