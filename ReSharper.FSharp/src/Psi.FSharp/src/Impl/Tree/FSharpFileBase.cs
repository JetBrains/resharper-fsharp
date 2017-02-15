using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal abstract class FSharpFileBase : FileElementBase, IFSharpFile
  {
    private FSharpCheckFileResults myCheckFileResults;

    public FSharpParseFileResults ParseResults { get; set; }

    public FSharpCheckFileResults CheckResults
    {
      get { return myCheckFileResults ?? (myCheckFileResults = FSharpCheckerUtil.CheckFSharpFile(this)); }
      set { myCheckFileResults = value; }
    }

    public override PsiLanguageType Language => FSharpLanguage.Instance;

    public virtual void Accept(TreeNodeVisitor visitor)
    {
      visitor.VisitNode(this);
    }

    public virtual void Accept<TContext>(TreeNodeVisitor<TContext> visitor, TContext context)
    {
      visitor.VisitNode(this, context);
    }

    public virtual TReturn Accept<TContext, TReturn>(TreeNodeVisitor<TContext, TReturn> visitor, TContext context)
    {
      return visitor.VisitNode(this, context);
    }
  }
}