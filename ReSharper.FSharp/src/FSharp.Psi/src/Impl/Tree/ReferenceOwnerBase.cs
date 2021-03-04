using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal abstract class ReferenceOwnerBase : FSharpCompositeElement, IFSharpReferenceOwner
  {
    private FSharpSymbolReference myReference;

    protected override void PreInit()
    {
      base.PreInit();
      myReference = null;
    }

    public FSharpSymbolReference Reference
    {
      get
      {
        if (myReference == null)
          lock (this)
            myReference ??= CreateReference();

        return myReference;
      }
    }

    public abstract IFSharpIdentifier FSharpIdentifier { get; }

    protected abstract FSharpSymbolReference CreateReference();

    public override ReferenceCollection GetFirstClassReferences() =>
      new ReferenceCollection(Reference);

    public IFSharpReferenceOwner SetName(string name) =>
      FSharpImplUtil.SetName(this, name);
  }
}
