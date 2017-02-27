using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal abstract class FSharpFileBase : FileElementBase, IFSharpFileCheckInfoOwner
  {
    private FSharpCheckFileResults CheckResults { get; set; }

    public FSharpParseFileResults ParseResults { get; set; }
    public FSharpCheckerService CheckerService { get; set; }
    public FSharpProjectOptions ProjectOptions { get; set; }
    public override PsiLanguageType Language => FSharpLanguage.Instance;
    public bool ReferencesResolved { get; set; }
    public bool IsChecked => CheckResults != null;

    public FSharpCheckFileResults GetCheckResults([CanBeNull] Action interruptChecker = null)
    {
      if (CheckResults != null) return CheckResults;
      var psiSourceFile = GetSourceFile();
      if (psiSourceFile == null || ParseResults == null) return null;
      Assertion.AssertNotNull(CheckerService, "CheckerService != null");

      return CheckResults = CheckerService.CheckFSharpFile(this, interruptChecker);
    }

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