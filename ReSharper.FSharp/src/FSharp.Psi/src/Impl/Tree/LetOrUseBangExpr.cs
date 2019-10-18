using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal class LetOrUseBangExpr : LetOrUseBangExprStub
  {
    public override TreeNodeCollection<IBinding> Bindings =>
      new TreeNodeCollection<IBinding>(new[] {Binding});

    public override TreeNodeEnumerable<IBinding> BindingsEnumerable =>
      new TreeNodeEnumerable<IBinding>(this, BINDING);
  }

  internal partial class LetOrUseBangExprStub
  {
    public bool IsUse => LetOrUseToken?.GetTokenType() == FSharpTokenType.USE_BANG;
  }
}
