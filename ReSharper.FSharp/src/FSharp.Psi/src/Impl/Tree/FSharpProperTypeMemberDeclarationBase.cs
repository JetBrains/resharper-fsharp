using System;
using System.Diagnostics;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

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
      this.GetOrCreateDeclaredElement(DeclaredElementFactory);

    [CanBeNull]
    public IDeclaredElement GetOrCreateDeclaredElement([NotNull] FSharpSymbol fcsSymbol) =>
      this.GetOrCreateDeclaredElement(declaration => declaration.CreateDeclaredElement(fcsSymbol));
  }
}
