using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Common.Checker;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal abstract class FSharpFileBase : FileElementBase, IFSharpFileCheckInfoOwner
  {
    // ReSharper disable once NotNullMemberIsNotInitialized
    public FSharpCheckerService CheckerService { get; set; }

    // ReSharper disable once NotNullMemberIsNotInitialized
    public IFSharpResolvedSymbolsCache ResolvedSymbolsCache { get; set; }

    private readonly CachedPsiValue<FSharpOption<FSharpParseFileResults>> myParseResults =
      new FileCachedPsiValue<FSharpOption<FSharpParseFileResults>>();

    public FSharpOption<FSharpParseFileResults> ParseResults
    {
      get => myParseResults.GetValue(this, fsFile => CheckerService.ParseFile(SourceFile));
      set => myParseResults.SetValue(this, value);
    }

    public override PsiLanguageType Language => FSharpLanguage.Instance;

    public FSharpOption<FSharpParseAndCheckResults> GetParseAndCheckResults(bool allowStaleResults) =>
      CheckerService.ParseAndCheckFile(SourceFile, allowStaleResults);

    public IReadOnlyList<FSharpResolvedSymbolUse> GetAllResolvedSymbols(FSharpCheckFileResults checkResults = null) =>
      ResolvedSymbolsCache.GetAllResolvedSymbols(SourceFile);

    public IReadOnlyList<FSharpResolvedSymbolUse> GetAllDeclaredSymbols(FSharpCheckFileResults checkResults = null) =>
      ResolvedSymbolsCache.GetAllDeclaredSymbols(SourceFile);

    public FSharpSymbolUse GetSymbolUse(int offset) =>
      ResolvedSymbolsCache.GetSymbolUse(SourceFile, offset);

    public FSharpSymbol GetSymbolDeclaration(int offset) =>
      ResolvedSymbolsCache.GetSymbolDeclaration(SourceFile, offset);

    public virtual void Accept(TreeNodeVisitor visitor) => visitor.VisitNode(this);

    public virtual void Accept<TContext>(TreeNodeVisitor<TContext> visitor, TContext context) =>
      visitor.VisitNode(this, context);

    public virtual TReturn Accept<TContext, TReturn>(TreeNodeVisitor<TContext, TReturn> visitor, TContext context) =>
      visitor.VisitNode(this, context);

    [NotNull] public IPsiSourceFile SourceFile => GetSourceFile().NotNull("sourceFile != null");
  }
}
