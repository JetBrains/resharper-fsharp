using System.Collections.Generic;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;

internal partial class LocalReferencePat
{
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
  public AccessRights GetAccessRights() => ModifiersUtil.GetAccessRights(AccessModifier);
}
