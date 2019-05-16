using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public class FSharpIdentifierToken : FSharpToken, IFSharpIdentifier, IReferenceExpression
  {
    public FSharpSymbolReference Reference { get; protected set; }

    public FSharpIdentifierToken(NodeType nodeType, string text) : base(nodeType, text)
    {
    }

    public FSharpIdentifierToken(string text) : base(FSharpTokenType.IDENTIFIER, text)
    {
    }

    protected override void PreInit()
    {
      base.PreInit();
      Reference = new FSharpSymbolReference(this);
    }

    public override ReferenceCollection GetFirstClassReferences() =>
      Parent is IPreventsChildResolve
        ? ReferenceCollection.Empty
        : new ReferenceCollection(Reference);

    public string Name => GetText().RemoveBackticks();

    public ITokenNode IdentifierToken => this;

    IReferenceExpression IReferenceExpression.SetName(string name)
    {
      var newToken = new FSharpIdentifierToken(name);
      LowLevelModificationUtil.ReplaceChildRange(this, this, newToken);
      return newToken;
    }
  }
}
