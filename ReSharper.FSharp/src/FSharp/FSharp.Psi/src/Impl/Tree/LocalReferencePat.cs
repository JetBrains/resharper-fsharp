using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;

internal partial class LocalReferencePat : ICachedTypeMemberDeclaration
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
      return cache.GetOrCreateDeclaredElement(this, static pat => new FSharpMethodParameter(pat));
    }
  }


  public override IFSharpIdentifier NameIdentifier => ReferenceName?.Identifier;

  public bool IsDeclaration => this.IsDeclaration();

  public override IEnumerable<IFSharpPattern> NestedPatterns => [this];

  public override TreeTextRange GetNameIdentifierRange() =>
    NameIdentifier.GetNameIdentifierRange();

  public bool IsMutable => Binding?.IsMutable ?? false;

  public void SetIsMutable(bool value)
  {
    var binding = Binding;
    Assertion.Assert(binding != null, "GetBinding() != null");
    binding.SetIsMutable(true);
  }

  public bool CanBeMutable => Binding != null;

  public IBindingLikeDeclaration Binding => this.GetBindingFromHeadPattern();
  public FSharpSymbolReference Reference => ReferenceName?.Reference;
  public override ConstantValue ConstantValue => this.GetConstantValue();
  public AccessRights GetAccessRights() => FSharpModifiersUtil.GetAccessRights(AccessModifier);
}
