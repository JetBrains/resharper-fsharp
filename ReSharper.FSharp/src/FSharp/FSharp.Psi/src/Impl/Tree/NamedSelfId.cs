using System.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal abstract class SelfIdDeclarationBase : LocalDeclarationBase, ICachedTypeMemberDeclaration
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

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public override IDeclaredElement DeclaredElement
    {
      get
      {
        if (!FSharpParameterUtil.IsMemberParameterDeclaration(this))
          return this;

        this.AssertIsValid("Asking declared element from invalid declaration");
        var cache = GetPsiServices().Caches.SourceDeclaredElementsCache;
        return cache.GetOrCreateDeclaredElement(this, static pat => new FSharpExtensionMemberThisParameter(pat));
      }
    }

  }
  
  internal partial class NamedSelfId
  {
    public override IFSharpIdentifier NameIdentifier => Identifier;
  }
}
