using System.Collections.Generic;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;

internal partial class TopReferencePat
{
  protected override string DeclaredElementName => NameIdentifier.GetCompiledName(Attributes);
  public override TreeTextRange GetNameRange() => NameIdentifier.GetNameRange();

  public override IFSharpIdentifier NameIdentifier => ReferenceName?.Identifier;

  public override TreeTextRange GetNameIdentifierRange() =>
    NameIdentifier.GetNameIdentifierRange();

  public TreeNodeCollection<IAttribute> Attributes =>
    this.GetBindingFromHeadPattern()?.Attributes ??
    TreeNodeCollection<IAttribute>.Empty;

  public override XmlDocBlock XmlDocBlock =>
    this.GetBindingFromHeadPattern()?.FirstChild as XmlDocBlock;

  public bool IsDeclaration => this.IsDeclaration();
  public override AccessRights GetAccessRights() => FSharpModifiersUtil.GetAccessRights(AccessModifier);

  public override IEnumerable<IFSharpPattern> NestedPatterns => [this];

  public bool IsMutable => Binding?.IsMutable ?? false;

  public void SetIsMutable(bool value)
  {
    var binding = Binding;
    Assertion.Assert(binding != null, "GetBinding() != null");
    binding.SetIsMutable(true);
  }

  public override IBindingLikeDeclaration Binding => this.GetBindingFromHeadPattern();
  public FSharpSymbolReference Reference => ReferenceName.Reference;

  IFSharpReferenceOwner IFSharpReferenceOwner.SetName(string name) => FSharpImplUtil.SetName(this, name);
  public FSharpReferenceContext? ReferenceContext => FSharpReferenceContext.Pattern;

  public override ConstantValue ConstantValue => this.GetConstantValue();
  
  public IFSharpParameter FSharpParameter => this.TryGetDeclaredFSharpParameter();
}
