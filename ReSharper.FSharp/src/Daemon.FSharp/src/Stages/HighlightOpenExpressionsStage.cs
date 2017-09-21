using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Plugins.FSharp.Daemon.Cs.Stages
{
  [DaemonStage(StagesBefore = new[] {typeof(TypeCheckErrorsStage)}, StagesAfter = new[] {typeof(CollectUsagesStage)})]
  public class HighlightOpenExpressionsStage : FSharpDaemonStageBase
  {
    private const string NsHighlightingAttr = HighlightingAttributeIds.NAMESPACE_IDENTIFIER_ATTRIBUTE;
    private const string ModuleHighlightingAttr = HighlightingAttributeIds.TYPE_STATIC_CLASS_ATTRIBUTE;

    protected override IDaemonStageProcess CreateProcess(IFSharpFile fsFile, IDaemonProcess process)
    {
      return new HighlightOpenExpressionsStageProcess(fsFile, process);
    }

    public class HighlightOpenExpressionsStageProcess : FSharpDaemonStageProcessBase
    {
      private readonly IFSharpFile myFsFile;

      public HighlightOpenExpressionsStageProcess([NotNull] IFSharpFile fsFile, [NotNull] IDaemonProcess process)
        : base(process)
      {
        myFsFile = fsFile;
      }

      public override void Execute(Action<DaemonStageResult> committer)
      {
        myFsFile.Accept(new ResolvedIdentifiersHighlighter(myFsFile, committer, SeldomInterruptChecker));
      }

      private class ResolvedIdentifiersHighlighter : TreeNodeVisitor
      {
        private readonly IFSharpFile myFsFile;
        private readonly Action<DaemonStageResult> myCommitter;
        private readonly SeldomInterruptCheckerWithCheckTime myInterruptChecker;
        private readonly List<HighlightingInfo> myHighlightings;

        public ResolvedIdentifiersHighlighter(IFSharpFile fsFile, Action<DaemonStageResult> committer,
          SeldomInterruptCheckerWithCheckTime interruptChecker)
        {
          myFsFile = fsFile;
          myCommitter = committer;
          myInterruptChecker = interruptChecker;
          myHighlightings =
            new List<HighlightingInfo>(fsFile.TokenBuffer.Buffer.Length / 10); // todo: to set resolved process?
        }


        public override void VisitFSharpFile(IFSharpFile file)
        {
          foreach (var module in file.Declarations)
          {
            module.Accept(this);
            myInterruptChecker.CheckForInterrupt();
          }
          myCommitter(new DaemonStageResult(myHighlightings));
        }


        public override void VisitTopLevelModuleOrNamespaceDeclaration(ITopLevelModuleOrNamespaceDeclaration topDecl)
        {
          foreach (var member in topDecl.MembersEnumerable)
          {
            member.Accept(this);
            myInterruptChecker.CheckForInterrupt();
          }

          var ids = topDecl.LongIdentifier.Identifiers;
          if (ids.IsEmpty) return;


          foreach (var token in topDecl.LongIdentifier.Qualifiers)
          {
            var nsToken = token as FSharpIdentifierToken;
            var symbol = nsToken?.FSharpSymbol;
            if (symbol == null)
              continue;

            myHighlightings.Add(CreateHighlighting(nsToken, symbol.GetHighlightingAttributeId()));
          }

          if (topDecl.IsModule)
            return;

          var nameToken = ids.Last();
          if (nameToken != null)
            myHighlightings.Add(CreateHighlighting(nameToken, NsHighlightingAttr));
        }

        public override void VisitNestedModuleDeclaration(INestedModuleDeclaration module)
        {
          foreach (var member in module.MembersEnumerable)
          {
            member.Accept(this);
            myInterruptChecker.CheckForInterrupt();
          }
        }

        public override void VisitOpen(IOpen open)
        {
          var ids = open.LongIdentifier.Identifiers;
          var names = ids.Select(token => token.GetText()).ToArray();
          for (var idsCount = ids.Count; idsCount > 0; idsCount--)
          {
            myInterruptChecker.CheckForInterrupt();
            var entity = ResolveOpenStatement(ids, idsCount, names);
            if (entity == null) continue;

            HighlightResolvedOpenStatement(entity, idsCount, ids);
            return;
          }
        }

        [CanBeNull]
        private FSharpEntity ResolveOpenStatement(TreeNodeCollection<ITokenNode> ids, int idsCount,
          [NotNull] IEnumerable<string> names)
        {
          var lastIdEndOffset = ids[idsCount - 1].GetTreeEndOffset().Offset;
          var namesList = ListModule.OfSeq(names.Take(idsCount));
          return FSharpSymbolsUtil.TryFindFSharpSymbol(myFsFile, namesList, lastIdEndOffset) as FSharpEntity;
        }

        private void HighlightResolvedOpenStatement([NotNull] FSharpEntity entity, int idsCount,
          TreeNodeCollection<ITokenNode> ids)
        {
          // todo: highlight modules as static classes
          for (var i = 0; i < idsCount - 1; i++)
            myHighlightings.Add(CreateHighlighting(ids[i], NsHighlightingAttr));

          var highlightingAttrId = entity.IsNamespace ? NsHighlightingAttr : ModuleHighlightingAttr;
          myHighlightings.Add(CreateHighlighting(ids[idsCount - 1], highlightingAttrId));
        }
      }
    }
  }
}