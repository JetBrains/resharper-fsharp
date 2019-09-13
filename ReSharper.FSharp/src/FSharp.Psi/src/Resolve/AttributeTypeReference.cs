using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.Util.Extension;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  public class AttributeTypeReference : TypeReference
  {
    public AttributeTypeReference([NotNull] IFSharpReferenceOwner owner) : base(owner)
    {
    }

    protected override string GetNewReferenceName(string name) =>
      name.SubstringBeforeLast(AttributeInstanceExtensions.ATTRIBUTE_SUFFIX);
  }
}
