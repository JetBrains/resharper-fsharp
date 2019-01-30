using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Common.Checker;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;
using Microsoft.FSharp.Core;
using static Microsoft.FSharp.Compiler.PrettyNaming;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal abstract class FSharpFileBase : FileElementBase, IFSharpFileCheckInfoOwner
  {
    private readonly object myGetSymbolsLock = new object();

    private class FileResolvedSymbols
    {
      // These symbols should be invalidated when dependent files change.
      // These symbols should also be used in FSharpMemberBase instances without attaching symbols to R# members.
      // A goal is to move these symbols to a separated cache and use it from IFSharpFile, FSharpMemberBase, FSharpTypePart elements.
      public Dictionary<int, FSharpResolvedSymbolUse> Declarations;
      public Dictionary<int, FSharpResolvedSymbolUse> Uses;
    }

    private readonly CachedPsiValue<FileResolvedSymbols> myResolvedSymbols =
      new FileCachedPsiValue<FileResolvedSymbols>();

    private FileResolvedSymbols ResolvedSymbols =>
      myResolvedSymbols.GetValue(this, () => new FileResolvedSymbols());

    public OneToListMap<string, int> TypeExtensionsOffsets { get; set; }
    private readonly IDictionary<int, ITypeExtension> myTypeExtensionsByOffset =
      new ConcurrentDictionary<int, ITypeExtension>();

    public FSharpCheckerService CheckerService { get; set; }

    private readonly CachedPsiValue<FSharpOption<FSharpParseFileResults>> myParseResults =
      new FileCachedPsiValue<FSharpOption<FSharpParseFileResults>>();

    public FSharpOption<FSharpParseFileResults> ParseResults
    {
      get => myParseResults.GetValue(this, fsFile => CheckerService.ParseFile(fsFile.GetSourceFile()));
      set => myParseResults.SetValue(this, value);
    }

    public override PsiLanguageType Language => FSharpLanguage.Instance;

    public FSharpOption<FSharpParseAndCheckResults> GetParseAndCheckResults(bool allowStaleResults,
      [CanBeNull] Action interruptChecker = null)
    {
      var sourceFile = GetSourceFile();
      Assertion.AssertNotNull(sourceFile, "sourceFile != null");
      return CheckerService.ParseAndCheckFile(sourceFile, allowStaleResults);
    }


    private void UpdateSymbols(FSharpCheckFileResults checkResults = null, Action daemonInterruptChecker = null)
    {
      var resolvedSymbols = ResolvedSymbols;
      if (resolvedSymbols.Declarations == null)
      {
        var interruptChecker = new SeldomInterruptCheckerWithCheckTime(100);
        checkResults = checkResults ?? GetParseAndCheckResults(false)?.Value.CheckResults;
        if (checkResults == null)
          return;

        var document = GetSourceFile()?.Document;
        var buffer = document?.Buffer;

        var symbolUses = checkResults.GetAllUsesOfAllSymbolsInFile().RunAsTask(daemonInterruptChecker);
        if (symbolUses == null || document == null)
          return;

        // add separate APIs to FCS to get resoved symbols and bindings?
        resolvedSymbols.Uses = new Dictionary<int, FSharpResolvedSymbolUse>(symbolUses.Length);
        resolvedSymbols.Declarations = new Dictionary<int, FSharpResolvedSymbolUse>(symbolUses.Length / 4);

        foreach (var symbolUse in symbolUses)
        {
          var symbol = symbolUse.Symbol;
          var range = symbolUse.RangeAlternate;

          var startOffset = document.GetOffset(range.Start);
          var endOffset = document.GetOffset(range.End);
          var mfv = symbol as FSharpMemberOrFunctionOrValue;

          if (symbolUse.IsFromDefinition)
          {
            if (mfv != null)
            {
              // workaround for auto-properties, see visualfsharp#3939
              var mfvLogicalName = mfv.LogicalName;
              if (mfvLogicalName.EndsWith("@", StringComparison.Ordinal))
                continue;

              // visualfsharp#3939
              if (mfvLogicalName == "v" &&
                  resolvedSymbols.Declarations.ContainsKey(startOffset))
                continue;

              if (mfvLogicalName == StandardMemberNames.ClassConstructor)
                continue;

              // visualfsharp#3943, visualfsharp#3933
              if (mfvLogicalName != StandardMemberNames.Constructor &&
                  !(FindTokenAt(new TreeOffset(endOffset - 1)) is FSharpIdentifierToken || mfv.IsActivePattern))
                continue;
            }
            else
            {
              // workaround for compiler generated symbols (e.g. fields auto-properties)
              if (!(FindTokenAt(new TreeOffset(endOffset - 1)) is FSharpIdentifierToken))
                continue;
            }

            var textRange = new TextRange(startOffset, endOffset);
            resolvedSymbols.Declarations[startOffset] = new FSharpResolvedSymbolUse(symbolUse, textRange);
            resolvedSymbols.Uses.Remove(startOffset);
          }
          else
          {
            // workaround for indexer properties, visualfsharp#3933
            if (startOffset == endOffset ||
                mfv != null && mfv.IsProperty && buffer[endOffset - 1] == ']')
              continue;

            var nameRange = FixRange(startOffset, endOffset, mfv?.LogicalName, buffer);

            // workaround for implicit type usages (e.g. in members with optional params), visualfsharp#3933
            if (symbol is FSharpEntity &&
                !(FindTokenAt(new TreeOffset(nameRange.EndOffset - 1)) is FSharpIdentifierToken))
              continue;

            if (!resolvedSymbols.Declarations.ContainsKey(startOffset))
              resolvedSymbols.Uses[nameRange.StartOffset] = new FSharpResolvedSymbolUse(symbolUse, nameRange);
          }

          interruptChecker.CheckForInterrupt();
        }
      }
    }

    private TextRange FixRange(int startOffset, int endOffset, [CanBeNull] string logicalName, IBuffer buffer)
    {
      // todo: remove when visualfsharp#3920 is implemented

      // trim foo.``bar`` to ``bar``
      const int minimumEscapedNameLength = 5;
      if (endOffset >= minimumEscapedNameLength && buffer.Length >= minimumEscapedNameLength &&
          buffer[endOffset - 1] == '`' && buffer[endOffset - 2] == '`')
        for (var i = endOffset - 4; i >= startOffset; i--)
          if (buffer[i] == '`' && buffer[i + 1] == '`')
            return new TextRange(i, endOffset);

      if (logicalName != null && IsMangledOpName(logicalName))
      {
        var token = FindTokenAt(new TreeOffset(endOffset - 1));
        if (token != null)
        {
          var sourceName = DecompileOpName.Invoke(logicalName);
          var sourceLength = sourceName.Length;
          if (sourceLength == endOffset - startOffset)
            return new TextRange(startOffset, endOffset);

          var opText = token.GetTokenType() == FSharpTokenType.SYMBOLIC_OP ? sourceName : logicalName;
          return new TextRange(endOffset - opText.Length, endOffset);
        }
      }

      // trim foo.bar to bar
      for (var i = endOffset - 1; i > startOffset; i--)
        if (buffer[i].Equals('.'))
          return new TextRange(i + 1, endOffset);
      return new TextRange(startOffset, endOffset);
    }

    public FSharpResolvedSymbolUse[] GetAllResolvedSymbols(FSharpCheckFileResults checkResults = null,
      Action interruptChecker = null)
    {
      lock (myGetSymbolsLock)
      {
        var resolvedSymbols = ResolvedSymbols;
        if (resolvedSymbols.Declarations == null)
          UpdateSymbols(checkResults, interruptChecker);
        return resolvedSymbols.Uses?.Values.AsArray() ?? EmptyArray<FSharpResolvedSymbolUse>.Instance;
      }
    }

    public FSharpResolvedSymbolUse[] GetAllDeclaredSymbols(FSharpCheckFileResults checkResults = null,
      Action interruptChecker = null)
    {
      lock (myGetSymbolsLock)
      {
        var resolvedSymbols = ResolvedSymbols;
        if (resolvedSymbols.Declarations == null)
          UpdateSymbols(checkResults);
        return resolvedSymbols.Declarations?.Values.AsArray() ?? EmptyArray<FSharpResolvedSymbolUse>.Instance;
      }
    }

    public FSharpSymbolUse GetSymbolUse(int offset)
    {
      lock (myGetSymbolsLock)
      {
        var resolvedSymbols = ResolvedSymbols;
        if (resolvedSymbols.Declarations == null)
          UpdateSymbols();
        var resolvedSymbol = resolvedSymbols.Uses?.TryGetValue(offset);
        if (resolvedSymbol == null)
          return null;

        return resolvedSymbols.Declarations?.TryGetValue(offset) == null
          ? resolvedSymbol.SymbolUse
          : null;
      }
    }

    public FSharpSymbol GetSymbolDeclaration(int offset)
    {
      lock (myGetSymbolsLock)
      {
        var resolvedSymbols = ResolvedSymbols;
        if (resolvedSymbols.Declarations == null)
          UpdateSymbols();
        return resolvedSymbols.Declarations?.TryGetValue(offset)?.SymbolUse.Symbol;
      }
    }

    public IEnumerable<ITypeExtension> GetTypeExtensions(string shortName) =>
      TypeExtensionsOffsets?.GetValuesSafe(shortName).Select(offset =>
      {
        if (myTypeExtensionsByOffset.TryGetValue(offset, out var typeExtension))
          return typeExtension;

        typeExtension = FindTokenAt(new TreeOffset(offset))?.GetContainingNode<ITypeExtension>();
        myTypeExtensionsByOffset[offset] = typeExtension;
        return typeExtension;
      }).WhereNotNull();

    public virtual void Accept(TreeNodeVisitor visitor) => visitor.VisitNode(this);

    public virtual void Accept<TContext>(TreeNodeVisitor<TContext> visitor, TContext context) =>
      visitor.VisitNode(this, context);

    public virtual TReturn Accept<TContext, TReturn>(TreeNodeVisitor<TContext, TReturn> visitor, TContext context) =>
      visitor.VisitNode(this, context);
  }

  public class FileCachedPsiValue<T> : CachedPsiValue<T>
  {
    protected override int GetTimestamp(ITreeNode element) =>
      element.GetContainingFile()?.ModificationCounter ??
      element.GetPsiServices().Files.PsiCacheTimestamp;
  }
}