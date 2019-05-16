using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  // todo: add more specific references, replace current inheritors.
  internal abstract class ReferenceExpressionBase : FSharpCompositeElement, IReferenceExpression
  {
    public FSharpSymbolReference Reference { get; protected set; }
    public abstract ITokenNode IdentifierToken { get; }

    protected override void PreInit()
    {
      base.PreInit();
      Reference = CreateReference();
    }

    protected abstract FSharpSymbolReference CreateReference();

    public override ReferenceCollection GetFirstClassReferences() =>
      new ReferenceCollection(Reference);

    public IReferenceExpression SetName(string name)
    {
      if (IdentifierToken is var id && id != null)
        LowLevelModificationUtil.ReplaceChildRange(id, id, new FSharpIdentifierToken(name));

      return this;
    }
  }
}
