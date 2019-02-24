using System.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class TopActivePatternCaseDeclaration : ICachedTypeMemberDeclaration
  {
    private volatile IDeclaredElement myCachedDeclaredElement;

    protected override void PreInit()
    {
      base.PreInit();
      myCachedDeclaredElement = null;
    }

    IDeclaredElement ICachedTypeMemberDeclaration.CachedDeclaredElement
    {
      get => myCachedDeclaredElement;
      set => myCachedDeclaredElement = value;
    }

    public override string CompiledName => NameIdentifier.GetSourceName();
    public override IFSharpIdentifier NameIdentifier => (IFSharpIdentifier) Identifier;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public override IDeclaredElement DeclaredElement
    {
      get
      {
        this.AssertIsValid("Asking declared element from invalid declaration");
        var cache = GetPsiServices().Caches.SourceDeclaredElementsCache;
        return cache.GetOrCreateDeclaredElement(this, DeclaredElementFactory);
      }
    }

    private IDeclaredElement DeclaredElementFactory(ITopActivePatternCaseDeclaration arg) =>
      new TopActivePatternCase(this);
  }
}