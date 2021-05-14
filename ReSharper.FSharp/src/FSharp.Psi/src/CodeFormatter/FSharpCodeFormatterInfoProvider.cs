using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Calculated.Interface;
using JetBrains.Application.Threading;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Services.Formatter;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Impl.CodeStyle;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.CodeFormatter
{
  [Language(typeof(FSharpLanguage))]
  public class FSharpFormatterInfoProvider :
    FormatterInfoProviderWithFluentApi<CodeFormattingContext, FSharpFormatSettingsKey>
  {
    public FSharpFormatterInfoProvider(ISettingsSchema settingsSchema,
      ICalculatedSettingsSchema calculatedSettingsSchema, IThreading threading, Lifetime lifetime)
      : base(settingsSchema, calculatedSettingsSchema, threading, lifetime)
    {
    }

    protected readonly NodeTypeSet AccessModifiers =
      new NodeTypeSet(FSharpTokenType.PUBLIC, FSharpTokenType.INTERNAL, FSharpTokenType.PRIVATE);

    protected readonly NodeTypeSet Comments =
      new NodeTypeSet(FSharpTokenType.LINE_COMMENT, FSharpTokenType.BLOCK_COMMENT);

    protected override void Initialize()
    {
      base.Initialize();

      Indenting();
      Aligning();
      Formatting();
      BlankLines();
    }

    public override ProjectFileType MainProjectFileType => FSharpProjectFileType.Instance;

    private void Indenting()
    {
      // todo: use continuous indent
      Describe<IndentingRule>()
        .Name("ModuleLikeHeaderIndent")
        .Where(
          Parent().In(ElementBitsets.TOP_LEVEL_MODULE_LIKE_DECLARATION_BIT_SET),
          Node().In(
            AccessModifiers.Union(
              FSharpTokenType.REC, FSharpTokenType.DOT,
              FSharpTokenType.IDENTIFIER, FSharpTokenType.GLOBAL,
              ElementType.TYPE_REFERENCE_NAME, ElementType.EXPRESSION_REFERENCE_NAME)))
        .Return(IndentType.External)
        .Build();

      var simpleIndentingNodes = new[]
      {
        ("ForExpr", ElementType.FOR_EXPR, ForExpr.DO_EXPR),
        ("ForEachExpr", ElementType.FOR_EACH_EXPR, ForEachExpr.DO_EXPR),
        ("WhileExpr", ElementType.WHILE_EXPR, WhileExpr.DO_EXPR),
        ("DoExpr", ElementType.DO_EXPR, DoExpr.EXPR),
        ("AssertExpr", ElementType.ASSERT_EXPR, AssertExpr.EXPR),
        ("LazyExpr", ElementType.LAZY_EXPR, LazyExpr.EXPR),
        ("ComputationExpr", ElementType.COMPUTATION_EXPR, ComputationExpr.EXPR),
        ("SetExpr", ElementType.SET_EXPR, SetExpr.RIGHT_EXPR),
        ("TryFinally_TryExpr", ElementType.TRY_FINALLY_EXPR, TryFinallyExpr.TRY_EXPR),
        ("TryFinally_FinallyExpr", ElementType.TRY_FINALLY_EXPR, TryFinallyExpr.FINALLY_EXPR),
        ("TryWith_TryExpr", ElementType.TRY_WITH_EXPR, TryWithExpr.TRY_EXPR),
        ("IfThenExpr", ElementType.IF_THEN_ELSE_EXPR, IfThenElseExpr.THEN_EXPR),
        ("ElifThenExpr", ElementType.ELIF_EXPR, ElifExpr.THEN_EXPR),
        ("LambdaExprBody", ElementType.LAMBDA_EXPR, LambdaExpr.EXPR),
        ("MatchExpr_Expr", ElementType.MATCH_EXPR, MatchExpr.EXPR),
        ("MatchExpr_With", ElementType.MATCH_EXPR, MatchExpr.WITH),
      };

      simpleIndentingNodes
        .ToList()
        .ForEach(DescribeSimpleIndentingRule);

      var continuousIndentNodes =
        new NodeTypeSet(
          ElementType.UNIT_EXPR,

          ElementType.ARRAY_PAT,
          ElementType.CHAR_RANGE_PAT,
          ElementType.IS_INST_PAT,
          ElementType.LIST_CONS_PAT,
          ElementType.LIST_PAT,
          ElementType.TYPED_PAT,
          ElementType.UNIT_PAT,

          ElementType.ARRAY_TYPE_USAGE,
          ElementType.FUNCTION_TYPE_USAGE,
          ElementType.NAMED_TYPE_USAGE,

          ElementType.LOCAL_BINDING,
          ElementType.TOP_BINDING,

          ElementType.EXPRESSION_REFERENCE_NAME,
          ElementType.TYPE_REFERENCE_NAME,

          ElementType.MATCH_CLAUSE,
          ElementType.NESTED_MODULE_DECLARATION,
          ElementType.OPEN_STATEMENT,
          ElementType.EXCEPTION_DECLARATION,
          ElementType.UNION_CASE_DECLARATION,
          ElementType.ENUM_CASE_DECLARATION,
          ElementType.F_SHARP_TYPE_DECLARATION,
          ElementType.INTERFACE_IMPLEMENTATION,
          ElementType.MODULE_ABBREVIATION_DECLARATION);

      Describe<ContinuousIndentRule>()
        .Name("ContinuousIndent")
        .Where(Node().In(continuousIndentNodes).Or().In(ElementType.PREFIX_APP_EXPR).Satisfies((node, context) =>
          !(node.Parent is IPrefixAppExpr)))
        .AddException(Node().In(ElementType.ATTRIBUTE_LIST))
        .AddException(Node().In(FSharpTokenType.LINE_COMMENT).Satisfies((node, context) => node is DocComment))
        .AddException(Node().In(ElementType.COMPUTATION_EXPR).Satisfies((node, context) =>
          !node.HasNewLineBefore(context.CodeFormatter)))
        .AddException(
          // todo: add setting
          Parent().In(ElementType.MATCH_CLAUSE).Satisfies(IsLastNodeOfItsType),
          Node().In(ElementBitsets.F_SHARP_EXPRESSION_BIT_SET).Satisfies((node, context) =>
            AreAligned(node, node.Parent, context.CodeFormatter)))
        .Build();

      // External: starts/ends at first/last node in interval
      // Internal: skips first/last node in interval

      Describe<IndentingRule>()
        .Name("NestedModuleMembersIndent")
        .Where(
          Parent().In(ElementType.NESTED_MODULE_DECLARATION),
          Node()
            .In(ElementBitsets.MODULE_MEMBER_BIT_SET.Union(Comments))
            .Satisfies((node, context) =>
              {
                // Find comment preceding a module member only (i.e. don't use comment before `=`)

                var parent = node.Parent;
                if (parent == null) return false;

                var foundComment = false;
                var ourNodeIsComment = false;

                for (var i = parent.FirstChild; i != null; i = i.NextSibling)
                {
                  if (Comments[i.NodeType])
                  {
                    if (i == node)
                    {
                      if (foundComment)
                        return false;

                      ourNodeIsComment = true;
                    }

                    foundComment = true;
                    continue;
                  }

                  if (ElementBitsets.MODULE_MEMBER_BIT_SET[i.NodeType])
                    return i == node && !foundComment || ourNodeIsComment;

                  if (!i.IsWhitespaceToken())
                  {
                    foundComment = false;
                    if (ourNodeIsComment)
                      return false;
                  }
                }

                return false;
              }))
        .CloseNodeGetter((node, context) => GetLastNodeOfTypeSet(ElementBitsets.MODULE_MEMBER_BIT_SET, node))
        .Calculate((node, context) => // node is Left()/Node()
        {
          var treeNode = (ITreeNode) node;
          // Formatter engine passes nulls once for caching internal/external intervals as an optimization.
          if (treeNode == null || context == null)
            return new ConstantOptionNode(new IndentOptionValue(IndentType.StartAtExternal | IndentType.EndAtExternal));

          var closingNode = GetLastNodeOfTypeSet(ElementBitsets.MODULE_MEMBER_BIT_SET, treeNode);
          if (closingNode.GetTreeStartOffset() > context.LastNode.GetTreeStartOffset())
            return
              new ConstantOptionNode(
                new IndentOptionValue(
                  IndentType.AbsoluteIndent | IndentType.StartAtExternal | IndentType.EndAtExternal |
                  IndentType.NonSticky | IndentType.NonAdjustable,
                  0, closingNode.CalcLineIndent(context.CodeFormatter, true)));

          // todo: try using the following for nodes without further indent:
          //     IndentType.NoIndentAtExternal
          //     or maybe startAtExt + multiplier 0

          return new ConstantOptionNode(
            new IndentOptionValue(IndentType.NoIndentAtExternal | IndentType.EndAtExternal | IndentType.NonSticky));
        }).Build();

      Describe<IndentingRule>()
        .Name("SimpleTypeRepr_Accessibility")
        .Where(
          Parent().In(ElementBitsets.ENUM_LIKE_TYPE_REPRESENTATION_BIT_SET),
          Left().In(ElementBitsets.ENUM_CASE_LIKE_DECLARATION_BIT_SET).Satisfies((node, context) =>
            AccessModifiers[node.GetPreviousMeaningfulSibling()?.GetTokenType()]))
        .CloseNodeGetter((node, context) => node.Parent?.LastChild)
        .Return(IndentType.External)
        .Build();

      Describe<IndentingRule>()
        .Name("TryWith_WithClauseIndent")
        .Where(
          Parent().HasType(ElementType.TRY_WITH_EXPR),
          Node().HasRole(TryWithExpr.MATCH_CLAUSE))
        .Switch(
          settings => settings.IndentOnTryWith,
          When(true).Return(IndentType.External),
          When(false).Return(IndentType.None))
        .Build();

      Describe<IndentingRule>()
        .Name("ElseExprIndent")
        .Where(
          Parent().In(ElementType.IF_THEN_ELSE_EXPR, ElementType.ELIF_EXPR),
          Node()
            .HasRole(IfThenElseExpr.ELSE_CLAUSE)
            .Satisfies(IndentElseExpr)
            .Or()
            .HasRole(ElifExpr.ELSE_CLAUSE)
            .Satisfies(IndentElseExpr))
        .Return(IndentType.External)
        .Build();

      Describe<IndentingRule>()
        .Name("MatchClauseWhenExprIndent")
        .Where(
          Parent().HasType(ElementType.MATCH_CLAUSE),
          Node().HasRole(MatchClause.WHEN_CLAUSE))
        .Return(IndentType.External)
        .Build();

      Describe<IndentingRule>()
        .Name("DoDeclIndent")
        .Where(
          Parent()
            .HasType(ElementType.DO_STATEMENT),
          Node().HasRole(DoStatement.CHAMELEON_EXPR))
        .Return(IndentType.External)
        .Build();
    }

    public static ITreeNode GetLastNodeOfTypeSet(NodeTypeSet nodeTypeSet, ITreeNode node)
    {
      var parent = node.Parent;
      if (parent == null) return null;

      ITreeNode result = null;

      for (var i = parent.FirstChild; i != null; i = i.NextSibling)
        if (nodeTypeSet[i.NodeType])
          result = i;

      return result;
    }

    private void Aligning()
    {
      var aligningNodes =
        new NodeTypeSet(
          ElementType.BINARY_APP_EXPR,
          ElementType.MATCH_EXPR,
          ElementType.TUPLE_EXPR,
          ElementType.SEQUENTIAL_EXPR,

          ElementType.TUPLE_PAT,

          ElementType.ARRAY_TYPE_USAGE,
          ElementType.NAMED_TYPE_USAGE,
          ElementType.TUPLE_TYPE_USAGE,

          ElementType.EXPRESSION_REFERENCE_NAME,
          ElementType.TYPE_REFERENCE_NAME,

          ElementType.RECORD_FIELD_BINDING_LIST,
          ElementType.RECORD_FIELD_DECLARATION_LIST,

          ElementType.ENUM_REPRESENTATION,
          ElementType.UNION_REPRESENTATION);

      Describe<IndentingRule>()
        .Name("SimpleAlignment")
        .Where(Node().In(aligningNodes))
        .Return(IndentType.AlignThrough)
        .Build();

      DescribeNestedAlignment<IPrefixAppExpr>("PrefixAppAlignment", ElementType.PREFIX_APP_EXPR);
      DescribeNestedAlignment<IFunctionTypeUsage>("FunctionTypeUsageAlignment", ElementType.FUNCTION_TYPE_USAGE);

      DescribeChildrenAlignment<IArrayOrListPat>(
        ElementBitsets.ARRAY_OR_LIST_PAT_BIT_SET,
        ElementBitsets.F_SHARP_PATTERN_BIT_SET,
        pat => pat.PatternsEnumerable);

      DescribeChildrenAlignment<IMatchClauseListOwnerExpr>(
        ElementType.MATCH_EXPR,
        ElementType.MATCH_CLAUSE,
        pat => pat.ClausesEnumerable);

      Describe<IndentingRule>().Name("EnumCaseLikeDeclarations")
        .Where(Parent().In(ElementBitsets.SIMPLE_TYPE_REPRESENTATION_BIT_SET),
          Left().In(ElementBitsets.ENUM_CASE_LIKE_DECLARATION_BIT_SET).Satisfies(IsFirstNodeOfItsType))
        .CloseNodeGetter((node, context) => node.Parent?.LastChild)
        .Return(IndentType.AlignThrough) // through => including the last node (till => without the last one)
        .Build();

      Describe<IndentingRule>()
        .Name("OutdentBinaryOperators")
        .Where(
          Parent().HasType(ElementType.BINARY_APP_EXPR),
          Node().HasRole(BinaryAppExpr.OP_REF_EXPR).Satisfies((node, _) => !IsPipeOperator(node, _)))
        .Switch(settings => settings.OutdentBinaryOperators,
          When(true).Return(IndentType.Outdent | IndentType.External))
        .Build();

      Describe<IndentingRule>()
        .Name("OutdentPipeOperators")
        .Where(
          Parent().HasType(ElementType.BINARY_APP_EXPR),
          Node().HasRole(BinaryAppExpr.OP_REF_EXPR).Satisfies(IsPipeOperator))
        .Switch(settings => settings.OutdentBinaryOperators,
          When(true).Switch(settings => settings.NeverOutdentPipeOperators,
            When(false).Return(IndentType.Outdent | IndentType.External)))
        .Build();
    }

    private void DescribeNestedAlignment<T>(string title, NodeType nodeType) =>
      Describe<IndentingRule>()
        .Name(title)
        .Where(Node().In(nodeType).Satisfies((node, context) => !(node.Parent is T)))
        .Return(IndentType.AlignThrough)
        .Build();

    private void DescribeChildrenAlignment<TParent>(IBuilderAction<IBlankWithSinglePattern> parentPattern,
      IBuilderAction<IBlankWithSinglePattern> nodeParent, Func<TParent, IEnumerable<ITreeNode>> childrenGetter) =>
      Describe<IndentingRule>()
        .Name("ListLikePatLikeAlignment")
        .Where(parentPattern, nodeParent)
        .CloseNodeGetter((node, context) => childrenGetter((TParent) node.Parent).LastOrDefault())
        .Return(IndentType.AlignThrough)
        .Build();

    private void DescribeChildrenAlignment<TParent>(NodeTypeSet parent, NodeTypeSet children,
      Func<TParent, IEnumerable<ITreeNode>> childrenGetter) =>
      DescribeChildrenAlignment(
        Parent().In(parent), Node().In(children).Satisfies(IsFirstNodeOfTypeSet(children, false)), childrenGetter);

    private void DescribeChildrenAlignment<TParent>(NodeType parent, NodeType children,
      Func<TParent, IEnumerable<ITreeNode>> childrenGetter) =>
      DescribeChildrenAlignment(
        Parent().In(parent), Node().In(children).Satisfies(IsFirstNodeOfItsType), childrenGetter);

    private void Formatting()
    {
      var nodesWithSpaces =
        new NodeTypeSet(
          ElementType.MATCH_EXPR,
          
          ElementType.CHAR_RANGE_PAT,
          ElementType.IS_INST_PAT,
          ElementType.LIST_CONS_PAT,
          ElementType.TYPED_PAT,

          ElementType.MATCH_CLAUSE,

          ElementType.ENUM_CASE_DECLARATION,
          ElementType.UNION_CASE_DECLARATION,
          ElementType.UNION_CASE_FIELD_DECLARATION_LIST,

          ElementType.FUNCTION_TYPE_USAGE,
          ElementType.TUPLE_TYPE_USAGE);

      Describe<FormattingRule>()
        .Name("DeclarationsSpaces")
        .Group(SpaceRuleGroup)
        .Where(Parent().In(nodesWithSpaces))
        .Return(IntervalFormatType.Space)
        .Build();

      Describe<FormattingRule>()
        .Name("SpaceAfterColon")
        .Group(SpaceRuleGroup)
        .Where(Left().In(FSharpTokenType.COLON))
        .Return(IntervalFormatType.Space)
        .Build();

      Describe<FormattingRule>()
        .Name("SpaceBeforeColon")
        .Group(SpaceRuleGroup)
        .Where(Right().In(FSharpTokenType.COLON))
        .Switch(it => it.SpaceBeforeColon, SpaceOptionsBuilders)
        .Build();

      Describe<FormattingRule>()
        .Name("SpaceAfterComma")
        .Group(SpaceRuleGroup)
        .Where(Left().In(FSharpTokenType.COMMA))
        .Switch(it => it.SpaceAfterComma, SpaceOptionsBuilders)
        .Build();

      Describe<FormattingRule>()
        .Name("NoSpaceBeforeSeparators")
        .Group(SpaceRuleGroup)
        .Where(Right().In(FSharpTokenType.COMMA, FSharpTokenType.SEMICOLON, FSharpTokenType.SEMICOLON_SEMICOLON))
        .Return(IntervalFormatType.Empty)
        .Build();

      DescribeEmptyOnlyFormatting(ElementType.UNIT_EXPR, FSharpTokenType.LPAREN, FSharpTokenType.RPAREN);

      DescribeEmptyOnlyFormatting(ElementType.ARRAY_PAT, FSharpTokenType.LBRACK_BAR, FSharpTokenType.BAR_RBRACK);
      DescribeEmptyOnlyFormatting(ElementType.LIST_PAT, FSharpTokenType.LBRACK, FSharpTokenType.RBRACK);
      DescribeEmptyOnlyFormatting(ElementType.UNIT_PAT, FSharpTokenType.LPAREN, FSharpTokenType.RPAREN);

      Describe<FormattingRule>()
        .Name("SpaceInLists")
        .Group(SpaceRuleGroup)
        .Where(
          Parent().In(ElementBitsets.ARRAY_OR_LIST_PAT_BIT_SET).Satisfies((node, context) =>
            !(node is IArrayOrListPat arrayOrListPat && arrayOrListPat.PatternsEnumerable.IsEmpty())))
        .Return(IntervalFormatType.Space)
        .Build();

      Describe<FormattingRule>()
        .Name("NoSpaceInArrayTypeUsage")
        .Group(SpaceRuleGroup)
        .Where(
          Parent().In(ElementType.ARRAY_TYPE_USAGE),
          Left().In(ElementBitsets.TYPE_USAGE_BIT_SET),
          Right().In(FSharpTokenType.LBRACK))
        .Return(IntervalFormatType.Empty)
        .Build();

      Describe<FormattingRule>()
        .Group(LineBreaksRuleGroup)
        .Name("LineBreakAfterTypeReprAccessModifier")
        .Where(
          Parent()
            .In(ElementBitsets.SIMPLE_TYPE_REPRESENTATION_BIT_SET)
            .Satisfies((node, context) => ((ISimpleTypeRepresentation) node).AccessModifier != null),
          Right().In(ElementBitsets.ENUM_CASE_LIKE_DECLARATION_BIT_SET).Satisfies(IsFirstNodeOfItsType))
        .Switch(settings => settings.LineBreakAfterTypeReprAccessModifier,
          When(true).Return(IntervalFormatType.NewLine))
        .Build();

      DescribeLineBreakInDeclarationWithEquals("TypeDeclaration",
        Node().In(ElementType.F_SHARP_TYPE_DECLARATION),
        Node().In(ElementBitsets.TYPE_REPRESENTATION_BIT_SET));

      DescribeLineBreakInDeclarationWithEquals("ModuleAbbreviation",
        Node().In(ElementType.MODULE_ABBREVIATION_DECLARATION),
        Node().In(ElementType.TYPE_REFERENCE_NAME));

      Describe<FormattingRule>()
        .Group(SpaceRuleGroup)
        .Name("SpaceAfterImplicitConstructorDecl")
        .Where(Left().HasType(ElementType.PRIMARY_CONSTRUCTOR_DECLARATION))
        .Return(IntervalFormatType.Space)
        .Build();

      Describe<FormattingRule>()
        .Group(SpaceRuleGroup)
        .Name("SpacesInMemberConstructorDecl")
        .Where(Parent().HasType(ElementType.SECONDARY_CONSTRUCTOR_DECLARATION))
        .Return(IntervalFormatType.Space)
        .Build();

      Describe<FormattingRule>()
        .Name("SpaceBetweenRecordBindings")
        .Where(
          Left()
            .HasType(ElementType.RECORD_FIELD_BINDING)
            .Satisfies((node, _) => ((IRecordFieldBinding) node).Semicolon != null),
          Right().HasType(ElementType.RECORD_FIELD_BINDING))
        .Return(IntervalFormatType.Space)
        .Build();

      Describe<FormattingRule>()
        .Group(LineBreaksRuleGroup)
        .Name("LineBreaksBetweenRecordBindings")
        .Where(
          Left()
            .HasType(ElementType.RECORD_FIELD_BINDING)
            .Satisfies((node, _) => ((IRecordFieldBinding) node).Semicolon == null),
          Right().HasType(ElementType.RECORD_FIELD_BINDING))
        .Return(IntervalFormatType.NewLine)
        .Build();

      Describe<FormattingRule>()
        .Name("SpacesAroundRecordExprBraces")
        .Where(
          Parent().HasType(ElementType.RECORD_EXPR),
          Left().In(FSharpTokenType.LBRACE, FSharpTokenType.RBRACE),
          Right()
            .In(ElementType.RECORD_FIELD_BINDING_LIST, FSharpTokenType.BLOCK_COMMENT)
            .Or()
            .HasRole(RecordExpr.COPY_INFO))
        .Return(IntervalFormatType.OnlySpace)
        .Build()
        .AndViceVersa()
        .Build();
    }

    private void DescribeEmptyOnlyFormatting(IBuilderAction<IBlankWithSinglePattern> parent, NodeType left, 
      NodeType right) =>
      Describe<FormattingRule>()
        .Name("SpaceInLists")
        .Group(SpaceRuleGroup)
        .Where(parent, Left().In(left), Right().In(right))
        .Return(IntervalFormatType.OnlyEmpty)
        .Build();

    private void DescribeEmptyOnlyFormatting(NodeType parent, NodeType left, NodeType right) =>
      DescribeEmptyOnlyFormatting(Parent().In(parent), left, right);

    private void BlankLines()
    {
      var declarations =
        ElementBitsets.MODULE_MEMBER_BIT_SET
          .Union(ElementBitsets.BINDING_BIT_SET)
          .Union(ElementBitsets.ENUM_CASE_LIKE_DECLARATION_BIT_SET)
          .Union(ElementType.F_SHARP_TYPE_DECLARATION);

      Describe<BlankLinesAroundNodeRule>()
        .AddNodesToGroupBefore(Node().In(Comments))
        .AddNodesToGroupAfter(Node().In(Comments))
        .AllowedNodesBefore(Node().Satisfies((node, checker) => true))
        .AllowedNodesAfter(Node().Satisfies((node, checker) => true))
        .Priority(1)
        .StartAlternating()
        .Name("BlankLinesAroundDeclarations")
        .Where(Node().In(declarations))
        .MinBlankLines(it => it.BlankLinesAroundMultilineModuleMembers)
        .MinBlankLinesForSingleLine(it => it.BlankLinesAroundSingleLineModuleMember)
        .Build()
        .Name("BlankLinesAroundDifferentModuleMemberKinds")
        .Where(Node().In(ElementBitsets.MODULE_MEMBER_BIT_SET))
        .MinBlankLines(it => it.BlankLinesAroundDifferentModuleMemberKinds)
        .AdditionalCheckForBlankLineAfter((node, context) =>
          node.GetNextMeaningfulSibling()?.NodeType is var nodeType &&
          nodeType != node.NodeType && ElementBitsets.MODULE_MEMBER_BIT_SET[nodeType])
        .AdditionalCheckForBlankLineBefore((node, context) =>
          node.GetPreviousMeaningfulSibling()?.NodeType is var nodeType &&
          nodeType != node.NodeType && ElementBitsets.MODULE_MEMBER_BIT_SET[nodeType])
        .Build()
        .Name("BlankLinesBeforeFirstTopLevelModuleMember")
        .Where(
          Parent().In(ElementBitsets.TOP_LEVEL_MODULE_LIKE_DECLARATION_BIT_SET),
          Node().In(ElementBitsets.MODULE_MEMBER_BIT_SET)
            .Satisfies(IsFirstNodeOfTypeSet(ElementBitsets.MODULE_MEMBER_BIT_SET, false)))
        .MinBlankLinesBefore(it => it.BlankLinesBeforeFirstModuleMemberInTopLevelModule)
        .Build()
        .Name("BlankLinesBeforeFirstNestedModuleMember")
        .Where(
          Parent().In(ElementType.NESTED_MODULE_DECLARATION),
          Node().In(ElementBitsets.MODULE_MEMBER_BIT_SET)
            .Satisfies(IsFirstNodeOfTypeSet(ElementBitsets.MODULE_MEMBER_BIT_SET, false)))
        .MinBlankLinesBefore(it => it.BlankLinesBeforeFirstModuleMemberInNestedModule)
        .Build()
        .Name("BlankLinesAroundModules")
        .Where(Node().In(ElementBitsets.TOP_LEVEL_MODULE_LIKE_DECLARATION_BIT_SET))
        .MinBlankLines(it => it.BlankLineAroundTopLevelModules)
        .Build();

      // todo: test sig files
      Describe<BlankLinesRule>()
        .Name("BlankLinesInDecls")
        .Group(MaxLineBreaks)
        .Where(Parent().In(ElementBitsets.MODULE_LIKE_DECLARATION_BIT_SET.Union(ElementBitsets.F_SHARP_FILE_BIT_SET)))
        .SwitchBlankLines(it => it.KeepMaxBlankLineAroundModuleMembers, true, BlankLineLimitKind.LimitMaximumMild)
        .Build();
    }

    private void DescribeSimpleIndentingRule((string name, CompositeNodeType parentType, short childRole) parameters)
    {
      Describe<IndentingRule>()
        .Name(parameters.name + "Indent")
        .Where(
          Parent().HasType(parameters.parentType),
          Node().HasRole(parameters.childRole))
        .Return(IndentType.External)
        .Build();
    }

    private void DescribeLineBreakInDeclarationWithEquals(string name,
      IBuilderAction<IBlankWithSinglePattern> declarationPattern,
      ChildBuilder<IBlankWithSinglePattern, NodePatternBlank> equalsBeforeNodesPattern) =>
      DescribeLineBreakInNode(name, declarationPattern, equalsBeforeNodesPattern.Satisfies((node, context) =>
          node.GetPreviousMeaningfulSibling()?.GetTokenType() == FSharpTokenType.EQUALS),
        key => key.DeclarationBodyOnTheSameLine, key => key.KeepExistingLineBreakBeforeDeclarationBody);

    private void DescribeLineBreakInNode(string name,
      IBuilderAction<IBlankWithSinglePattern> containingNodesPattern,
      IBuilderAction<IBlankWithSinglePattern> equalsBeforeNodesPattern,
      Expression<Func<FSharpFormatSettingsKey, object>> onSameLine,
      Expression<Func<FSharpFormatSettingsKey, object>> keepExistingLineBreak)
    {
      var containingNodes = containingNodesPattern.BuildBlank();
      var equalsBeforeNodes = equalsBeforeNodesPattern.BuildBlank();

      Describe<WrapRule>()
        .Name($"{name}Wrap")
        .Where(Node().Is(containingNodes))
        .Switch(keepExistingLineBreak,
          When(false).Switch(onSameLine,
            When(PlaceOnSameLineAsOwner.IF_OWNER_IS_SINGLE_LINE)
              .Return(WrapType.Chop | WrapType.PseudoStartBeforeExternal)))
        .Build();

      Describe<FormattingRule>()
        .Name($"{name}NewLine")
        .Where(
          Parent().Is(containingNodes),
          Right().Is(equalsBeforeNodes))
        .Group(LineBreaksRuleGroup | WrapRuleGroup)
        .Switch(keepExistingLineBreak,
          When(true)
            .Switch(onSameLine,
              When(PlaceOnSameLineAsOwner.NEVER).Return(IntervalFormatType.NewLine),
              When(PlaceOnSameLineAsOwner.ALWAYS, PlaceOnSameLineAsOwner.IF_OWNER_IS_SINGLE_LINE)
                .Return(IntervalFormatType.DoNotRemoveUserNewLines)),
          When(false)
            .Switch(onSameLine,
              When(PlaceOnSameLineAsOwner.NEVER).Return(IntervalFormatType.NewLine),
              When(PlaceOnSameLineAsOwner.ALWAYS).Return(IntervalFormatType.RemoveUserNewLines),
              When(PlaceOnSameLineAsOwner.IF_OWNER_IS_SINGLE_LINE)
                .Return(IntervalFormatType.RemoveUserNewLines | IntervalFormatType.InsertNewLineConditionally)))
        .Build();

      // initial impl (keeps formatting instead of keeping existing line break only):
      // .Switch(keepExistingLineBreak,
      // When(true).Return(IntervalFormatType.DoNotRemoveUserNewLines),
      // When(false)
      //   .Switch(onSameLine,
      //     When(PlaceOnSameLineAsOwner.NEVER).Return(IntervalFormatType.NewLine),
      //     When(PlaceOnSameLineAsOwner.ALWAYS).Return(IntervalFormatType.RemoveUserNewLines),
      //     When(PlaceOnSameLineAsOwner.IF_OWNER_IS_SINGLE_LINE)
      //       .Return(IntervalFormatType.RemoveUserNewLines | IntervalFormatType.InsertNewLineConditionally)))

      Describe<FormattingRule>()
        .Name($"{name}Space")
        .Where(
          Parent().Is(containingNodes),
          Right().Is(equalsBeforeNodes))
        .Group(SpaceRuleGroup)
        .Return(IntervalFormatType.Space)
        .Priority(3)
        .Build();
    }

    private static bool IndentElseExpr(ITreeNode elseExpr, CodeFormattingContext context) =>
      elseExpr.GetPreviousMeaningfulSibling().IsFirstOnLine(context.CodeFormatter) && !(elseExpr is IElifExpr);

    private static bool AreAligned(ITreeNode first, ITreeNode second, IWhitespaceChecker whitespaceChecker) =>
      first.CalcLineIndent(whitespaceChecker) == second.CalcLineIndent(whitespaceChecker);

    private static bool IsPipeOperator(ITreeNode node, CodeFormattingContext context) =>
      node is IReferenceExpr refExpr && FSharpPredefinedType.PipeOperatorNames.Contains(refExpr.ShortName);
  }
}
