using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Common.Checker;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Tree
{
  internal abstract class FSharpFileBase : FileElementBase, IFSharpFileCheckInfoOwner
  {
    public FSharpCheckerService CheckerService { get; set; }
    public FSharpProjectOptions ProjectOptions { get; set; }
    public TokenBuffer ActualTokenBuffer { get; set; }
    public override PsiLanguageType Language => FSharpLanguage.Instance;
    public bool ReferencesResolved { get; set; }
    public bool IsChecked { get; private set; }

    public FSharpOption<FSharpParseFileResults> GetParseResults(bool keepResults = false,
      Action interruptChecker = null)
    {
      return CheckerService.GetOrCreateParseResults(GetSourceFile()); // todo: interrupt
    }

    public FSharpCheckFileResults GetCheckResults(bool forceRecheck = false, [CanBeNull] Action interruptChecker = null)
    {
      var parseResults = CheckerService.GetOrCreateParseResults(GetSourceFile())?.Value;
      if (parseResults == null)
        return null;

      var checkResults = CheckerService.CheckFile(this, parseResults, OptionModule.OfObj(interruptChecker))?.Value;
      IsChecked = true;
      return checkResults;
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