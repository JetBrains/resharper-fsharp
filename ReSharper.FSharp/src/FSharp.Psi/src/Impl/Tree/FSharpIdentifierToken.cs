using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public class FSharpIdentifierToken : FSharpToken, IFSharpIdentifierLikeNode, IReferenceOwner, IFSharpIdentifier
  {
    private FSharpSymbolReference myReference;

    public FSharpSymbolReference Reference
    {
      get
      {
        if (myReference == null && !(Parent is IPreventsChildResolve))
        {
          lock (this)
          {
            if (myReference == null)
              myReference = new FSharpSymbolReference(this);
          }
        }

        return myReference;
      }
    }

    public FSharpIdentifierToken(NodeType nodeType, string text) : base(nodeType, text)
    {
    }

    public FSharpIdentifierToken(string text) : base(FSharpTokenType.IDENTIFIER, text)
    {
    }

    public override ReferenceCollection GetFirstClassReferences() =>
      Parent is IPreventsChildResolve
        ? ReferenceCollection.Empty
        : new ReferenceCollection(Reference);

    public string Name => GetText().RemoveBackticks();

    public ITokenNode IdentifierToken => this;

    IReferenceOwner IReferenceOwner.SetName(string name)
    {
      var newToken = new FSharpIdentifierToken(name);
      LowLevelModificationUtil.ReplaceChildRange(this, this, newToken);
      return newToken;
    }

    public FSharpSymbolReference QualifierReference =>
      // todo: ignore inner parens of qualifier
      ReferenceExprNavigator.GetByIdentifier(this)?.Qualifier is ReferenceExpr qualifier
        ? qualifier.Reference is var reference && reference.IsValid() ? reference : null
        : null;
  }
}
