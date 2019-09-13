using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  // todo: add more specific references, replace current inheritors.
  internal abstract class ReferenceOwnerBase : FSharpCompositeElement, IFSharpReferenceOwner, IPreventsChildResolve
  {
    private FSharpSymbolReference myReference;

    public FSharpSymbolReference Reference
    {
      get
      {
        if (myReference == null)
        {
          lock (this)
          {
            if (myReference == null)
              myReference = CreateReference();
          }
        }

        return myReference;
      }
    }

    public abstract ITokenNode IdentifierToken { get; }

    protected abstract FSharpSymbolReference CreateReference();

    public override ReferenceCollection GetFirstClassReferences() =>
      new ReferenceCollection(Reference);

    public IFSharpReferenceOwner SetName(string name)
    {
      if (IdentifierToken is var id && id != null)
        LowLevelModificationUtil.ReplaceChildRange(id, id, new FSharpIdentifierToken(name));

      return this;
    }
  }
}
