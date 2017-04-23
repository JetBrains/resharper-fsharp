using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.ReSharper.Daemon.UsageChecking;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Declarations;
using JetBrains.ReSharper.Psi.FSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.FSharp.Tree;
using JetBrains.ReSharper.Psi.FSharp.Util;
using JetBrains.ReSharper.Psi.Tree;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Daemon.FSharp.Stages
{
  [DaemonStage(StagesBefore = new[] {typeof(TypeCheckErrorsStage)}, StagesAfter = new[] {typeof(CollectUsagesStage)})]
  public class ResolvedIdentifiersHighlightStage : FSharpDaemonStageBase
  {
    protected override IDaemonStageProcess CreateProcess(IFSharpFile fsFile, IDaemonProcess process)
    {
      return new ResolvedIdentifiersHighlightProcess(fsFile, process);
    }

    public class ResolvedIdentifiersHighlightProcess : FSharpDaemonStageProcessBase
    {
      private readonly IFSharpFile myFsFile;

      public ResolvedIdentifiersHighlightProcess([NotNull] IFSharpFile fsFile, [NotNull] IDaemonProcess process)
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
          myHighlightings = new List<HighlightingInfo>(fsFile.TokenBuffer.Buffer.Length);
        }

        public override void VisitFSharpImplFile(IFSharpImplFile implFile)
        {
          foreach (var module in implFile.Declarations) // todo: implement for signature files
          {
            module.Accept(this);
            myInterruptChecker.CheckForInterrupt();
          }
          VisitFSharpFile(implFile);
        }

        public override void VisitFSharpFile(IFSharpFile file)
        {
          foreach (var token in file.Tokens().OfType<FSharpIdentifierToken>())
          {
            var symbol = token.FSharpSymbol;
            if (symbol != null)
              myHighlightings.Add(CreateHighlighting(token, symbol.GetHighlightingAttributeId()));
            myInterruptChecker.CheckForInterrupt();
          }
          myCommitter(new DaemonStageResult(myHighlightings));
        }

        public override void VisitLet(ILet letBinding)
        {
          HighlightIdentifier(letBinding.Identifier, HighlightingAttributeIds.LOCAL_VARIABLE_IDENTIFIER_ATTRIBUTE);
          base.VisitLet(letBinding);
        }

        public override void VisitTopLevelModuleOrNamespaceDeclaration(ITopLevelModuleOrNamespaceDeclaration module)
        {
          foreach (var member in module.MembersEnumerable)
          {
            member.Accept(this);
            myInterruptChecker.CheckForInterrupt();
          }

          var ids = module.LongIdentifier.Identifiers;
          if (ids.IsEmpty) return;

          foreach (var nsToken in module.LongIdentifier.Qualifiers)
          {
            if (nsToken != null)
              myHighlightings.Add(CreateHighlighting(nsToken, HighlightingAttributeIds.NAMESPACE_IDENTIFIER_ATTRIBUTE));
          }

          var highlightingAttrId = module.IsModule
            ? HighlightingAttributeIds.TYPE_STATIC_CLASS_ATTRIBUTE
            : HighlightingAttributeIds.NAMESPACE_IDENTIFIER_ATTRIBUTE;

          var nameToken = ids.Last();
          if (nameToken != null)
            myHighlightings.Add(CreateHighlighting(nameToken, highlightingAttrId));
        }

        public override void VisitMemberDeclaration(IMemberDeclaration member)
        {
          var highlightingAttrId = member.ParametersEnumerable.IsEmpty()
            ? HighlightingAttributeIds.FIELD_IDENTIFIER_ATTRIBUTE
            : HighlightingAttributeIds.METHOD_IDENTIFIER_ATTRIBUTE;

          HighlightIdentifier(member.Identifier, highlightingAttrId);
        }

        public override void VisitFSharpUnionCaseDeclaration(IFSharpUnionCaseDeclaration caseDeclaration)
        {
          HighlightIdentifier(caseDeclaration.Identifier, HighlightingAttributeIds.TYPE_CLASS_ATTRIBUTE);

          foreach (var fieldDeclaration in caseDeclaration.FieldsEnumerable)
            HighlightIdentifier(fieldDeclaration.Identifier, HighlightingAttributeIds.FIELD_IDENTIFIER_ATTRIBUTE);
        }

        public override void VisitNestedModuleDeclaration(INestedModuleDeclaration module)
        {
          HighlightIdentifier(module.Identifier, HighlightingAttributeIds.TYPE_STATIC_CLASS_ATTRIBUTE);

          foreach (var member in module.MembersEnumerable)
          {
            member.Accept(this);
            myInterruptChecker.CheckForInterrupt();
          }
        }

        public override void VisitFSharpExceptionDeclaration(IFSharpExceptionDeclaration exn)
        {
          HighlightIdentifier(exn.Identifier, HighlightingAttributeIds.TYPE_CLASS_ATTRIBUTE);
        }

        public override void VisitFSharpObjectModelTypeDeclaration(IFSharpObjectModelTypeDeclaration type)
        {
          HighlightIdentifier(type.Identifier, GetHighlightingAttributeId(type.TypePartKind));

          foreach (var member in type.TypeMembersEnumerable)
            member.Accept(this);
        }

        [NotNull]
        private string GetHighlightingAttributeId(FSharpPartKind kind)
        {
          switch (kind)
          {
            case FSharpPartKind.Class:
              return HighlightingAttributeIds.TYPE_CLASS_ATTRIBUTE;
            case FSharpPartKind.Interface:
              return HighlightingAttributeIds.TYPE_INTERFACE_ATTRIBUTE;
            case FSharpPartKind.Struct:
              return HighlightingAttributeIds.TYPE_STRUCT_ATTRIBUTE;
            default:
              throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
          }
        }

        public override void VisitFSharpSimpleTypeDeclaration(IFSharpSimpleTypeDeclaration type)
        {
          HighlightIdentifier(type.Identifier, HighlightingAttributeIds.TYPE_CLASS_ATTRIBUTE);

          foreach (var member in type.MemberDeclarations)
            (member as IFSharpDeclaration)?.Accept(this);
        }

        public override void VisitFSharpEnumDeclaration(IFSharpEnumDeclaration enumDeclaration)
        {
          foreach (var member in enumDeclaration.EnumMembersEnumerable)
            HighlightIdentifier(member.Identifier, HighlightingAttributeIds.FIELD_IDENTIFIER_ATTRIBUTE);

          base.VisitFSharpEnumDeclaration(enumDeclaration);
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
            myHighlightings.Add(CreateHighlighting(ids[i], HighlightingAttributeIds.NAMESPACE_IDENTIFIER_ATTRIBUTE));

          var highlightingAttrId = entity.IsNamespace
            ? HighlightingAttributeIds.NAMESPACE_IDENTIFIER_ATTRIBUTE
            : HighlightingAttributeIds.TYPE_STATIC_CLASS_ATTRIBUTE;
          myHighlightings.Add(CreateHighlighting(ids[idsCount - 1], highlightingAttrId));
        }

        public override void VisitFSharpRecordDeclaration(IFSharpRecordDeclaration record)
        {
          foreach (var field in record.FieldsEnumerable)
            HighlightIdentifier(field.Identifier, HighlightingAttributeIds.FIELD_IDENTIFIER_ATTRIBUTE);

          base.VisitFSharpRecordDeclaration(record);
        }

        private void HighlightIdentifier([CanBeNull] IFSharpIdentifier ident, string highlightingAttributeId)
        {
          var idToken = ident?.IdentifierToken;
          if (idToken != null)
            myHighlightings.Add(CreateHighlighting(idToken, highlightingAttributeId));
        }
      }
    }
  }
}