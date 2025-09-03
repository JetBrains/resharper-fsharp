using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.Calculated.Interface;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DocComments;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Services.Formatter;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CodeStyle;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Format;
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

    public override bool NeedsAdditionalFormatStage => true;
    public override bool ForceRemoveTrailingSpaces => true;

    protected readonly NodeTypeSet AccessModifiers =
      new(FSharpTokenType.PUBLIC, FSharpTokenType.INTERNAL, FSharpTokenType.PRIVATE);

    protected readonly NodeTypeSet Comments =
      new(FSharpTokenType.LINE_COMMENT, FSharpTokenType.BLOCK_COMMENT);

    protected override void Initialize()
    {
      base.Initialize();

      Indenting();
      Aligning();
      Formatting();
      Wrapping();
      BlankLines();
      Braces();
    }

    public override bool KeepAlignmentWhenFirstOnLine() => true;

    private void Wrapping()
    {
      Describe<WrapRule>()
        .Name($"FieldPatKeepTogether")
        .Where(Node().In(ElementType.FIELD_PAT))
        .Return(WrapType.KeepTogether)
        .Build();

      var pattern = Node().In(ElementType.PAREN_PAT)
        .Satisfies((node, _) => ((IParenPat)node.Node).Pattern is ITuplePat).Or().In(ElementType.PAREN_EXPR)
        .Satisfies((node, _) => ((IParenExpr)node.Node).InnerExpression is ITupleExpr);
      Describe<WrapRule>()
        .Name($"ChopArguments")
        .Where(pattern)
        .Switch(key => key.WrapArguments,
          When(WrapStyleSimple.CHOP_IF_LONG).Return(WrapType.Chop | WrapType.StartAtExternal))
        .Build();

      var blank = pattern.BuildBlank();
      Describe<FormattingRule>()
        .Name($"ChopArguments")
        .Where(
          Parent().Is(blank),
          Left().In(FSharpTokenType.LPAREN))
        .Switch(key => key.PreferLineBreakAfterMultilineLparen,
          When(true)
            .Switch(key => key.WrapArguments,
              When(WrapStyleSimple.CHOP_IF_LONG)
                .Return(IntervalFormatType.InsertNewLineConditionally),
              When(WrapStyleSimple.WRAP_IF_LONG)
                .Return(IntervalFormatType.ExcellentPlaceToWrap)))
        .Build();
      
      Describe<FormattingRule>()
        .Name($"ChopArguments")
        .Where(GrandParent().Is(blank), Left().In(FSharpTokenType.COMMA))
        .Switch(key => key.WrapArguments, When(WrapStyleSimple.CHOP_IF_LONG).Return(IntervalFormatType.InsertNewLineConditionally))
        .Build();
      
      // WrapArguments

      Describe<WrapRule>()
        .Name("MultilineLambdaBody")
        .Where(
          Parent().In(ElementType.LAMBDA_EXPR),
          Node().In(ElementBitsets.F_SHARP_EXPRESSION_BIT_SET)
        )
        .Return(WrapType.KeepTogether | WrapType.LineBreakBeforeIfMultiline)
        .Build();

      // todo: unify with the lambda rule?
      Describe<WrapRule>()
        .Name("BindingBody")
        .Where(
          Parent().In(ElementBitsets.BINDING_BIT_SET.Union(ElementType.DO_STATEMENT, ElementType.DO_EXPR)),
          Node().In(ElementBitsets.F_SHARP_EXPRESSION_BIT_SET.Union(ElementType.CHAMELEON_EXPRESSION)).Satisfies((node,
            _) => node.Node switch
          {
            IChameleonExpression { Expression: ILambdaExpr } or ILambdaExpr => false,
            _ => true
          })
        )
        .Return(WrapType.KeepTogether | WrapType.LineBreakBeforeIfMultiline)
        .Build();

      // Describe<FormattingRule>()
      //   .Name("LambdaInBinding")
      //   .StageIdPredicate(FormattingStageAcceptanceType.AdditionalFormatStage)
      //   .Where(
      //     Parent().In(ElementBitsets.BINDING_BIT_SET),
      //     Node().In(ElementType.LAMBDA_EXPR).Satisfies((node, _) => node.ContainsLineBreak()))
      //   .Return(IntervalFormatType.NoLineBreaks)
      //   .Build();

      Describe<WrapRule>()
        .Name("MultilineMatchExpr")
        .Where(
          Node().In(ElementType.MATCH_EXPR, ElementType.MATCH_LAMBDA_EXPR)
        )
        .Return(WrapType.Chop | WrapType.StartAtExternal)
        .Build();
      
      Describe<FormattingRule>()
        .Name("MultilineMatchExpr")
        .Group(WrapRuleGroup)
        .Where(
          Parent().In(ElementType.MATCH_EXPR, ElementType.MATCH_LAMBDA_EXPR),
          Right().In(ElementType.MATCH_CLAUSE)
        )
        .Return(IntervalFormatType.InsertNewLineConditionally)
        .Build();
    }

    public override ProjectFileType MainProjectFileType => FSharpProjectFileType.Instance;

    private void Braces()
    {
      var bracesRule = Describe<BracesRule>()
        .LPar(FSharpTokenType.LBRACE)
        .RPar(FSharpTokenType.RBRACE)
        .FormatBeforeParent(true)
        .DisableParentAlignment(false)
        .ProhibitBlankLinesNearBracesInBsdStyle(false)
        .AvoidZeroExternalIndent(true)
        .RestrictedReformatOption(key => key.RestrictedFormat)
        .AlwaysAlignContent(true)
        .OnlyBreakBeforeRBraceIfBreakAfterLBrace(true)
        .OnlyWrapBeforeRBraceIfLBraceIsIncludedInTask(true)
        .MaxBlankLinesInsideSetting(it => it.KeepMaxBlankLineAroundModuleMembers) // todo: use separate settings
        .FormatBeforeLBrace(false);

      bracesRule.Clone().Name("RecordReprWithModifierBraces")
        .Where(Parent().In(ElementType.RECORD_REPRESENTATION).Satisfies((node, _) => ((IRecordRepresentation)node.Node).AccessModifier == null))
        .FormatBeforeLBrace(false)
        .BraceSetting(it => it.TypeDeclarationBraces)
        .Priority(2)
        .Build();

      var nonReprBraceRule =
        bracesRule
          .Clone()
          .FormatBeforeLBrace(false, formatBeforeLBraceUnlessSingleLine: true)
          .BraceSetting(it => it.TypeDeclarationBraces)
          .Priority(2);

      nonReprBraceRule
        .Clone()
        .Name("RecordReprWithoutModifierBraces")
        .Where(Parent().In(ElementType.RECORD_REPRESENTATION).Satisfies((node, _) =>
          ((IRecordRepresentation)node.Node).AccessModifier != null)
        )
        .Build();

      nonReprBraceRule
        .Clone()
        .Name("RecordExprBraces")
        .Where(Parent().In(ElementType.RECORD_EXPR))
        .FormatBeforeLBrace(false)
        .Build();

      nonReprBraceRule
        .Clone()
        .Name("AnonRecordBraces")
        .Where(Parent().In(ElementType.ANON_RECORD_EXPR))
        .LPar(FSharpTokenType.LBRACE_BAR)
        .RPar(FSharpTokenType.BAR_RBRACE)
        .Build();

      nonReprBraceRule
        .Clone()
        .Name("ObjExprBraces")
        .Where(Parent().In(ElementType.OBJ_EXPR))
        .NeverAlignContent(true)
        .Build();

      nonReprBraceRule
        .Clone()
        .Name("ListBraces")
        .Where(Parent().In(ElementType.LIST_EXPR, ElementType.LIST_PAT))
        .LPar(FSharpTokenType.LBRACK)
        .RPar(FSharpTokenType.RBRACK)
        .AlwaysAddSpacesInsidePico(false)
        .SpacesInsideParsSetting(key => key.SpaceAroundDelimiter)
        .FormatBeforeLBrace(false)
        .Build();
      
      nonReprBraceRule
        .Clone()
        .Name("ArrayBraces")
        .Where(Parent().In(ElementType.ARRAY_EXPR, ElementType.ARRAY_PAT))
        .LPar(FSharpTokenType.LBRACK_BAR)
        .RPar(FSharpTokenType.BAR_RBRACK)
        .AlwaysAddSpacesInsidePico(false)
        .SpacesInsideParsSetting(key => key.SpaceAroundDelimiter)
        // .FormatBeforeLBrace(false)
        .Build();
    }

    private void Indenting()
    {
      // continuous indent is wrapping in the middle of a line
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

      Describe<IndentingRule>()
        .Name("Object type repr indenting ")
        .Where(Left().In(FSharpTokenType.CLASS, FSharpTokenType.INTERFACE, FSharpTokenType.STRUCT),
          Right().In(FSharpTokenType.END), Parent().In(ElementBitsets.OBJECT_MODEL_TYPE_REPRESENTATION_BIT_SET))
        .Return(IndentType.Internal)
        .Build();

      // var pattern = Node().In(ElementType.PAREN_PAT)
      //   .Satisfies((node, context) => ((IParenPat)node.Node).Pattern is ITuplePat).Or().In(ElementType.PAREN_EXPR)
      //   .Satisfies((node, context) => ((IParenExpr)node.Node).InnerExpression is ITupleExpr);
      //
      // var blank = pattern.BuildBlank();

      // Describe<IndentingRule>()
      //   .Name("Yo")
      //   .Where(
      //     Parent().Is(blank),
      //     Left().In(FSharpTokenType.LPAREN),
      //     Right().In(FSharpTokenType.RPAREN))
      //   .Return(IndentType.Internal | IndentType.NonSticky)
      //   .Build();

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
        ("MatchExpr_Expr", ElementType.MATCH_EXPR, MatchExpr.EXPR),
        ("MatchExpr_With", ElementType.MATCH_EXPR, MatchExpr.WITH),
      };

      simpleIndentingNodes
        .ToList()
        .ForEach(DescribeSimpleIndentingRule);

      Describe<IndentingRule>()
        .Name("LambdaExprBodyIndent")
        .Where(
          Node().HasRole(LambdaExpr.EXPR).Or().In(FSharpTokenType.LINE_COMMENT, FSharpTokenType.BLOCK_COMMENT).Before(Node().HasRole(LambdaExpr.EXPR))) // todo: add set
        .StartAlternating()

        .Where(
          Parent().HasType(ElementType.LAMBDA_EXPR).Satisfies((node, _) => node.HasNewLineBefore()))
        .Return(IndentType.External | IndentType.NonSticky)
        .Build()

        .Where(
          Parent().HasType(ElementType.LAMBDA_EXPR).Satisfies((node, _) => !node.HasNewLineBefore()))
        .Return(IndentType.External)
        .Build();

      DescribeHeaderRule("LambdaExpr", ElementType.LAMBDA_EXPR, FSharpTokenType.FUN, FSharpTokenType.RARROW);
      DescribeHeaderRule("ObjExpr", ElementType.OBJ_EXPR, FSharpTokenType.NEW, FSharpTokenType.WITH);

      var continuousIndentNodes =
        new NodeTypeSet(
            ElementType.UNIT_EXPR,
            ElementType.REFERENCE_EXPR,
            ElementType.PREFIX_APP_EXPR,

            ElementType.ARRAY_PAT,
            ElementType.CHAR_RANGE_PAT,
            ElementType.IS_INST_PAT,
            ElementType.LIST_PAT,
            ElementType.TYPED_PAT,
            ElementType.UNIT_PAT,
            ElementType.PARAMETERS_OWNER_PAT,

            ElementType.ARRAY_TYPE_USAGE,
            ElementType.FUNCTION_TYPE_USAGE,
            ElementType.NAMED_TYPE_USAGE,

            ElementType.LOCAL_BINDING,
            ElementType.TOP_BINDING,

            ElementType.EXPRESSION_REFERENCE_NAME,
            ElementType.TYPE_REFERENCE_NAME,

            ElementType.UNION_CASE_FIELD_DECLARATION,

            ElementType.NESTED_MODULE_DECLARATION,
            ElementType.F_SHARP_TYPE_DECLARATION,
            ElementType.TYPE_EXTENSION_DECLARATION,
            ElementType.OPEN_STATEMENT,
            ElementType.EXCEPTION_DECLARATION,
            ElementType.UNION_CASE_DECLARATION,
            ElementType.ENUM_CASE_DECLARATION,
            ElementType.INTERFACE_IMPLEMENTATION,
            ElementType.MODULE_ABBREVIATION_DECLARATION)
          .Union(ElementBitsets.TYPE_BODY_MEMBER_DECLARATION_BIT_SET)
          .Except(ElementType.LET_BINDINGS_DECLARATION);

      Describe<ContinuousIndentRule>()
        .Name("ContinuousIndent")
        .Where(Node().In(continuousIndentNodes.Except(ElementType.MEMBER_DECLARATION)).Satisfies((node, _) => !(IsNestedRefOrAppExpr(node.NodeOrNull))))
        .AddException(Node().In(ElementType.ATTRIBUTE_LIST, DocCommentBlockNodeType.Instance))
        .AddException(Node().In(ElementType.COMPUTATION_EXPR).Satisfies((node, _) =>
          !node.HasNewLineBefore()))
        .Build();

      Describe<ContinuousIndentRule>()
        .Name("ContinuousIndentDouble").DefaultMultiplier(1) // use 2 // use multiplier setting instead of default multiplier
        .Where(Node().In(ElementType.MEMBER_DECLARATION))
        .AddException(Node().In(ElementType.ATTRIBUTE_LIST, DocCommentBlockNodeType.Instance))
        .AddException(Node().In(ElementType.CHAMELEON_EXPRESSION))
        .Build();

      Describe<IndentingRule>()
        .Name("MemberDeclBody")
        .Where(
          Node().In(ElementType.CHAMELEON_EXPRESSION),
          Parent().In(ElementType.MEMBER_DECLARATION))
        .Return(IndentType.External)
        .Build();

      Describe<IndentingRule>()
        .Name("ObjExprIndent")
        .Where(
          Parent().In(ElementType.OBJ_EXPR),
          Left().In(FSharpTokenType.LBRACE),
          Right().In(FSharpTokenType.RBRACE))
        .Return(IndentType.Internal)
        .Build();

      Describe<FSharpContinuousIndentRule>()
        .Name("FSharpContinuousIndentRule")
        .Where(Node().In(ElementType.MATCH_CLAUSE))
        .AddException(
          Parent().In(ElementType.MATCH_CLAUSE).Satisfies(IsLastNodeOfItsType),
          Node().In(ElementBitsets.F_SHARP_EXPRESSION_BIT_SET).Satisfies((node, context) =>
            AreAligned(node, node.Parent, context.CodeFormatter)))
        .AddException(
          Parent().In(ElementType.MATCH_CLAUSE),
          Node().In(ElementType.OR_PAT))
        .Build();

      // External: starts/ends at first/last node in interval
      // Internal: skips first/last node in interval

      var memberOrRepr =
        ElementBitsets.TYPE_BODY_MEMBER_DECLARATION_BIT_SET.Union(ElementBitsets.TYPE_REPRESENTATION_BIT_SET);

      FixPartialSelectionMemberIndentingRule(ElementType.NESTED_MODULE_DECLARATION, ElementBitsets.MODULE_MEMBER_BIT_SET);
      FixPartialSelectionMemberIndentingRule(ElementType.F_SHARP_TYPE_DECLARATION, memberOrRepr);
      FixPartialSelectionMemberIndentingRule(ElementType.TYPE_EXTENSION_DECLARATION, memberOrRepr);
      FixPartialSelectionMemberIndentingRule(ElementType.OBJ_EXPR, ElementBitsets.TYPE_BODY_MEMBER_DECLARATION_BIT_SET);
      FixPartialSelectionMemberIndentingRule(ElementType.INTERFACE_IMPLEMENTATION, memberOrRepr);
      FixPartialSelectionMemberIndentingRule(ElementType.CLASS_REPRESENTATION, memberOrRepr);
      FixPartialSelectionMemberIndentingRule(ElementType.INTERFACE_REPRESENTATION, memberOrRepr);
      FixPartialSelectionMemberIndentingRule(ElementType.STRUCT_REPRESENTATION, memberOrRepr);
      FixPartialSelectionMemberIndentingRule(ElementType.ENUM_REPRESENTATION, new NodeTypeSet(ElementType.ENUM_CASE_DECLARATION));
      FixPartialSelectionMemberIndentingRule(ElementType.UNION_REPRESENTATION, new NodeTypeSet(ElementType.UNION_CASE_DECLARATION));

      Describe<IndentingRule>()
        .Name("ObjExprInterfaceImpl")
        .Where(
          Parent().In(ElementType.OBJ_EXPR),
          Node().In(ElementType.INTERFACE_IMPLEMENTATION))
        .Calculate((node, context) =>
        {
          if (node == null || context == null)
            return new ConstantOptionNode(new IndentOptionValue(IndentType.StartAtExternal | IndentType.EndAtExternal));

          var treeNode = (VirtNode)node;
          var objExpr = ObjExprNavigator.GetByInterfaceImplementation((IInterfaceImplementation)treeNode.Node);
          var newKeyword = objExpr?.NewKeyword;
          if (newKeyword == null) // todo: formatter: check this
            return new ConstantOptionNode(new IndentOptionValue(IndentType.StartAtExternal | IndentType.EndAtExternal));

          var newKeywordVirtNode = new VirtNode(context, newKeyword);
          var newKeywordIndent = newKeywordVirtNode.CalcNodeIndent(context.TabWidth);

          return
            new ConstantOptionNode(
              new IndentOptionValue(
                IndentType.AbsoluteIndent | IndentType.StartAtExternal | IndentType.EndAtExternal |
                IndentType.NonSticky | IndentType.NonAdjustable, 0, newKeywordIndent));
        })
        .Build();

      Describe<IndentingRule>()
        .Name("SimpleTypeRepr_Accessibility")
        .Where(
          Parent().In(ElementBitsets.ENUM_LIKE_TYPE_REPRESENTATION_BIT_SET),
          Left().In(ElementBitsets.ENUM_CASE_LIKE_DECLARATION_BIT_SET).Satisfies((node, _) =>
            AccessModifiers[node.GetPreviousMeaningfulSibling().GetTokenType()]))
        .CloseNodeGetter((node, _) => node.Parent.LastChild)
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

    private void DescribeHeaderRule(string name, NodeType parentNodeType, NodeType left, NodeType right)
    {
      Describe<IndentingRule>()
        .Where(
          Parent().In(parentNodeType),
          Left().In(left),
          Right().In(right))
        .StartAlternating()

        .Name(name + "HeaderAligning")
        .Return(IndentType.NoIndentAtExternal | IndentType.EndAtExternal | IndentType.Alignment)
        .Build()

        .Name(name + "HeaderContIndent")
        .Return(IndentType.StartAfterFirstToken | IndentType.EndAtExternal)
        .Build();
    }

    private static bool IsNestedRefOrAppExpr(ITreeNode node)
    {
      // ReSharper disable once VariableHidesOuterVariable
      bool IsQualifiedExprOrHighPrecedenceAppExpr(ITreeNode node) =>
        node is IQualifiedExpr or IPrefixAppExpr { IsHighPrecedence: true };

      if (node is IPrefixAppExpr { IsHighPrecedence: false } && node.Parent is IPrefixAppExpr) return true;

      if (IsQualifiedExprOrHighPrecedenceAppExpr(node) && (IsQualifiedExprOrHighPrecedenceAppExpr(node.Parent)))
        return true;

      if (node is IReferenceName && node.Parent is IReferenceName) return true;
      if (node is IFunctionTypeUsage && node.Parent is IFunctionTypeUsage) return true;
      if (node is IArrayTypeUsage && node.Parent is IArrayTypeUsage) return true;

      return false;
    }

    public static VirtNode GetLastNodeOfTypeSet(NodeTypeSet nodeTypeSet, VirtNode node)
    {
      var parent = node.Parent;
      if (parent == null) return node.Null;

      VirtNode result = node.Null;

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
          ElementType.SEQUENTIAL_EXPR,
          ElementType.IF_THEN_ELSE_EXPR,

          ElementType.TUPLE_PAT,
          ElementType.PARAMETERS_OWNER_PAT,

          ElementType.ARRAY_TYPE_USAGE,
          ElementType.NAMED_TYPE_USAGE,
          ElementType.TUPLE_TYPE_USAGE,

          ElementType.EXPRESSION_REFERENCE_NAME,
          ElementType.TYPE_REFERENCE_NAME,

          ElementType.RECORD_FIELD_BINDING_LIST,
          ElementType.RECORD_FIELD_DECLARATION_LIST,

          ElementType.UNION_CASE_FIELD_DECLARATION,

          ElementType.ENUM_REPRESENTATION,
          ElementType.UNION_REPRESENTATION);

      Describe<IndentingRule>()
        .Name("SimpleAlignment")
        .Where(Node().In(aligningNodes).Satisfies((node, _) => !aligningNodes[node.Parent.NodeType] || node.Parent.FirstChild != node))
        .Return(IndentType.AlignThrough)
        .Build();

      Describe<IndentingRule>()
        .Name("LetExprAlignment")
        .Where(Node().In(ElementType.LET_OR_USE_EXPR))
        .Return(IndentType.AlignThrough)
        .Build();

      // todo: tuple patterns
      //
      // Describe<IndentingRule>()
      //   .Name("TupleAlignment")
      //   .Where(Node().In(ElementType.TUPLE_EXPR).Satisfies((node, _) => ((ITupleExpr)node.NodeOrNull)?.Expressions.LastOrDefault() is not ILambdaExpr or IMatchLambdaExpr))
      //   .Return(IndentType.AlignThrough)
      //   .Build();

      var expressionsExceptLambda =
        ElementBitsets.F_SHARP_EXPRESSION_BIT_SET.Except(ElementType.LAMBDA_EXPR, ElementType.MATCH_LAMBDA_EXPR);

      Describe<IndentingRule>()
        .Name("TupleAlignment")
        .Where(Parent().In(ElementType.TUPLE_EXPR),
          Node().In(expressionsExceptLambda).Satisfies(IsFirstNodeOfItsType)).CloseNodeGetter((node, _) => GetLastNodeOfTypeSet(expressionsExceptLambda, node))
        .Return(IndentType.AlignThrough)
        .Build();

      Describe<IndentingRule>()
        .Name("Todo123")
        .Where(Parent().In(ElementType.NAMED_UNION_CASE_FIELDS_PAT),
          Node().In(ElementType.FIELD_PAT).Satisfies(IsFirstNodeOfItsType)).CloseNodeGetter(GetLastNodeWithSameType)
        .Return(IndentType.AlignThrough)
        .Build();

      DescribeNestedAlignment("PrefixAppAlignment", ElementType.PREFIX_APP_EXPR);
      DescribeNestedAlignment("RefExprAppAlignment", ElementType.REFERENCE_EXPR);
      DescribeNestedAlignment("FunctionTypeUsageAlignment", ElementType.FUNCTION_TYPE_USAGE);
      DescribeNestedAlignment("ArrayTypeUsageAlignment", ElementType.ARRAY_TYPE_USAGE);

      DescribeChildrenAlignment<IArrayOrListPat>(
        ElementBitsets.ARRAY_OR_LIST_PAT_BIT_SET,
        ElementBitsets.F_SHARP_PATTERN_BIT_SET,
        pat => pat.PatternsEnumerable);

      DescribeChildrenAlignment<IMatchClauseListOwnerExpr>(
        ElementType.MATCH_EXPR,
        ElementType.MATCH_CLAUSE,
        pat => pat.ClausesEnumerable);

      DescribeChildrenAlignment<IAttributeList>(
        ElementType.ATTRIBUTE_LIST,
        ElementType.ATTRIBUTE,
        attrList => attrList.AttributesEnumerable);

      Describe<IndentingRule>().Name("Function deindent alignment")
        .Where(
          Parent().In(ElementType.PAREN_EXPR)
            .Satisfies((node, _) =>
            {
              var innerExpr = ((IParenExpr)node.Node).InnerExpression;
              if (innerExpr is IMatchLambdaExpr or ILambdaExpr) return true;

              if (innerExpr is ITupleExpr tupleExpr &&
                  tupleExpr.ExpressionsEnumerable.LastOrDefault() is IMatchLambdaExpr or ILambdaExpr)
                return true;

              return false;
            }),
          Left().In(FSharpTokenType.LPAREN), Right().In(FSharpTokenType.RPAREN))
        .Return(IndentType.OverrideAlignment | IndentType.ExternalNoIndent | IndentType.Internal)
        .Build();

      Describe<IndentingRule>().Name("EnumCaseLikeDeclarations")
        .Where(Parent().In(ElementBitsets.SIMPLE_TYPE_REPRESENTATION_BIT_SET),
          Left().In(ElementBitsets.ENUM_CASE_LIKE_DECLARATION_BIT_SET).Satisfies(IsFirstNodeOfItsType))
        .CloseNodeGetter((node, _) => node.Parent.LastChild)
        .Return(IndentType.AlignThrough) // through => including the last node (till => without the last one)
        .Build();

      Describe<IndentingRule>()
        .Name("OutdentBinaryOperators")
        .Priority(100100)
        .Where(
          Parent().HasType(ElementType.BINARY_APP_EXPR),
          Node().HasRole(BinaryAppExpr.OP_REF_EXPR).Satisfies((node, context) => !IsPipeOperator(node, context)))
        .Switch(settings => settings.OutdentBinaryOperators,
          When(true).Return(IndentType.Outdent | IndentType.External))
        .Build();

      Describe<IndentingRule>()
        .Name("OutdentPipeOperators")
        .Priority(100100)
        .Where(
          Parent().HasType(ElementType.BINARY_APP_EXPR),
          Node().HasRole(BinaryAppExpr.OP_REF_EXPR).Satisfies(IsPipeOperator))
        .Switch(settings => settings.OutdentBinaryOperators,
          When(true).Switch(settings => settings.NeverOutdentPipeOperators,
            When(false).Return(IndentType.Outdent | IndentType.External)))
        .Build();

      Describe<IndentingRule>()
        .Name("RecordReprAccessibility")
        .Where(Node().In(ElementType.RECORD_REPRESENTATION).Satisfies((node, context) =>
          ((RecordRepresentation)node.Node).AccessModifier != null &&
          new VirtNode(context, ((RecordRepresentation)node.Node).LeftBrace).HasNewLineBefore()))
        .StartAlternating()

        .Return(IndentType.AlignThrough)
        .Build()
        
        .Return(IndentType.StartAfterFirstToken | IndentType.EndAtExternal)
        .Build();
    }

    private void DescribeNestedAlignment(string title, NodeType nodeType) =>
      Describe<IndentingRule>()
        .Name(title)
        .Where(Node().In(nodeType).Satisfies((node, _) => !IsNestedRefOrAppExpr(node.NodeOrNull)))
        .Return(IndentType.AlignThrough)
        .Build();

    private void DescribeChildrenAlignment<TParent>(IBuilderAction<IBlankWithSinglePattern> parentPattern,
      IBuilderAction<IBlankWithSinglePattern> nodeParent, Func<TParent, IEnumerable<ITreeNode>> childrenGetter) =>
      Describe<IndentingRule>()
        .Name("ListLikePatLikeAlignment")
        .Where(parentPattern, nodeParent)
        .CloseNodeGetter((node, context) => new VirtNode(context, childrenGetter((TParent) node.Parent.NodeOrNull).LastOrDefault()))
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

          ElementType.F_SHARP_TYPE_DECLARATION,
          ElementType.PRIMARY_CONSTRUCTOR_DECLARATION,
          ElementType.SECONDARY_CONSTRUCTOR_DECLARATION,

          ElementType.ENUM_CASE_DECLARATION,
          ElementType.UNION_CASE_DECLARATION,
          ElementType.UNION_CASE_FIELD_DECLARATION_LIST,

          ElementType.FUNCTION_TYPE_USAGE,
          ElementType.TUPLE_TYPE_USAGE,

          ElementType.LOCAL_BINDING,
          ElementType.TOP_BINDING,

          ElementType.INTERFACE_IMPLEMENTATION);

      Describe<FormattingRule>()
        .Name("DeclarationsSpaces")
        .Group(SpaceRuleGroup)
        .Where(Parent().In(nodesWithSpaces))
        .Return(IntervalFormatType.Space)
        .Build();

      Describe<FormattingRule>()
        .Name("MultilineMatchExpr")
        .Group(SpaceRuleGroup)
        .Priority(100)
        .Where(
          Parent().In(ElementType.MATCH_EXPR, ElementType.MATCH_LAMBDA_EXPR),
          Right().In(ElementType.MATCH_CLAUSE)
        )
        .Return(IntervalFormatType.InsertSpace)
        .Build();


      Describe<FormattingRule>()
        .Name("GoodPlacesToWrap")
        .Group(WrapRuleGroup | LineBreaksRuleGroup)
        .Where(Parent().In(ElementType.PREFIX_APP_EXPR))
        .Return(IntervalFormatType.GoodPlaceToWrap)
        .Build();

      var membersBitset =
        ElementBitsets.MODULE_MEMBER_BIT_SET.Union(ElementBitsets.TYPE_BODY_MEMBER_DECLARATION_BIT_SET);

      Describe<FormattingRule>()
        .Name("LineBreaksBetweenMembers")
        .Group(LineBreaksRuleGroup)
        .Where(Right().In(membersBitset))
        .Return(IntervalFormatType.NewLine)
        .Build();

      // Describe<FormattingRule>()
      //   .Name("PipeNewLine")
      //   .Group(LineBreaksRuleGroup)
      //   .Where(
      //     Left().In(ElementBitsets.F_SHARP_EXPRESSION_BIT_SET),
      //     Right().In(FSharpTokenType.SYMBOLIC_OP), // todo: check pipe op
      //     If((context, formattingContext) => )
      //   ) 
      //   .Return(IntervalFormatType.NewLine)
      //   .Build();

      Describe<FormattingRule>()
        .Name("LineBreakBeforeBindingInExpr")
        .Group(LineBreaksRuleGroup)
        .Where(
          Parent().In(ElementType.LET_OR_USE_EXPR).Satisfies((node, _) => ((ILetOrUseExpr)node.Node).InKeyword == null),
          Right().In(ElementBitsets.F_SHARP_EXPRESSION_BIT_SET)
        )
        .Return(IntervalFormatType.NewLine)
        .Build();

      // todo: members
      Describe<FormattingRule>()
        .Name("GoodPlacesToWrap")
        .Group(WrapRuleGroup | LineBreaksRuleGroup)
        .Where(Parent().In(ElementBitsets.BINDING_BIT_SET), Left().In(FSharpTokenType.EQUALS))
        .Return(IntervalFormatType.GoodPlaceToWrap)
        .Build();

      Describe<FormattingRule>()
        .Name("ProhibitWrappingBeforeSemicolon")
        .Group(WrapRuleGroup | LineBreaksRuleGroup)
        .Where(Right().In(FSharpTokenType.SEMICOLON, FSharpTokenType.COMMA, FSharpTokenType.THEN))
        .Return(IntervalFormatType.NoWrap)
        .Build();


      Describe<FormattingRule>()
        .Name("PreferWrappingAfterSemicolon")
        .Group(WrapRuleGroup | LineBreaksRuleGroup)
        .Where(Left().In(FSharpTokenType.SEMICOLON, FSharpTokenType.COMMA))
        .Return(IntervalFormatType.ExcellentPlaceToWrap)
        .Build();

      Describe<FormattingRule>()
        .Name("SpaceAfterPunctuation")
        .Group(SpaceRuleGroup)
        .Where(Left().In(FSharpTokenType.COLON, FSharpTokenType.RARROW, ElementType.RETURN_TYPE_INFO))
        .Return(IntervalFormatType.Space)
        .Build();

      Describe<FormattingRule>()
        .Name("SpaceBeforeColon")
        .Group(SpaceRuleGroup)
        .Where(Right().In(ElementType.RETURN_TYPE_INFO, FSharpTokenType.COLON))
        .Switch(it => it.SpaceBeforeColon, SpaceOptionsBuilders)
        .Build();

      Describe<FormattingRule>()
        .Name("SpaceBeforeColon")
        .Group(SpaceRuleGroup)
        .Priority(100)
        .Where(
          Parent().In(ElementBitsets.BINDING_BIT_SET),
          Right().In(ElementType.RETURN_TYPE_INFO).Satisfies((node, _) => (node.Node.Parent as IBinding)?.HasParameters ?? false)
        )
        .Return(IntervalFormatType.Space)
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
        .Where(Right().In(FSharpTokenType.COMMA, FSharpTokenType.SEMICOLON, FSharpTokenType.SEMICOLON_SEMICOLON, FSharpTokenType.RPAREN))
        .Return(IntervalFormatType.Empty)
        .Build();

      Describe<FormattingRule>()
        .Name("NoSpaceBeforeSeparators")
        .Group(SpaceRuleGroup)
        .Where(Left().In(FSharpTokenType.LPAREN))
        .Return(IntervalFormatType.Empty)
        .Build();

      Describe<FormattingRule>()
        .Name("NoSpaceBeforePrimaryCtor")
        .Group(SpaceRuleGroup)
        .Where(
          Parent().In(ElementType.F_SHARP_TYPE_DECLARATION),
          Left().In(FSharpTokenType.IDENTIFIER, ElementType.POSTFIX_TYPE_PARAMETER_DECLARATION_LIST),
          Right().In(ElementType.PRIMARY_CONSTRUCTOR_DECLARATION, ElementType.POSTFIX_TYPE_PARAMETER_DECLARATION_LIST))
        .Return(IntervalFormatType.Empty)
        .Build();

      Describe<FormattingRule>()
        .Name("NoSpaceInsideIndexerLikeList")
        .Group(SpaceRuleGroup)
        .Priority(1000)
        .Where(
          Parent().In(ElementType.LIST_EXPR).Satisfies((node, _) =>
            node.NodeOrNull is IListExpr listExpr &&
            (DotLambdaExprNavigator.GetByExpression(listExpr) != null ||
             PrefixAppExprNavigator.GetByArgumentExpression(listExpr) is { IsIndexerLike: true })))
        .Return(IntervalFormatType.Empty)
        .StartAlternating()
        .Where(Left().In(FSharpTokenType.LBRACK))
        .Build()
        .Where(Right().In(FSharpTokenType.RBRACK))
        .Build();

      Describe<FormattingRule>()
        .Name("NoSpaceBeforeTypeParams")
        .Group(SpaceRuleGroup)
        .Where(
          Parent().In(ElementBitsets.BINDING_BIT_SET),
          Left().In(ElementBitsets.F_SHARP_PATTERN_BIT_SET),
          Right().In(ElementType.POSTFIX_TYPE_PARAMETER_DECLARATION_LIST)
        )
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
          Parent().In(ElementBitsets.ARRAY_OR_LIST_PAT_BIT_SET).Satisfies((node, _) =>
            !(node.NodeOrNull is IArrayOrListPat arrayOrListPat && arrayOrListPat.PatternsEnumerable.IsEmpty())))
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
        .Name("NoSpaceInListPat")
        .Group(SpaceRuleGroup)
        .Where(
          Parent().In(ElementType.LIST_PAT, ElementType.ARRAY_PAT),
          Left().In(FSharpTokenType.LBRACK, FSharpTokenType.LBRACE_BAR),
          Right().In(ElementBitsets.F_SHARP_PATTERN_BIT_SET))
        .Switch(it => it.SpaceAroundDelimiter, SpaceOptionsBuilders)
        .Build();

      Describe<FormattingRule>()
        .Name("NoSpaceInListPat")
        .Group(SpaceRuleGroup)
        .Where(
          Parent().In(ElementType.LIST_PAT, ElementType.ARRAY_PAT),
          Left().In(ElementBitsets.F_SHARP_PATTERN_BIT_SET),
          Right().In(FSharpTokenType.RBRACK, FSharpTokenType.BAR_RBRACK))
        .Switch(it => it.SpaceAroundDelimiter, SpaceOptionsBuilders)
        .Build();

      Describe<FormattingRule>()
        .Name("AttributeBrackets")
        .Group(SpaceRuleGroup | LineBreaksRuleGroup)
        .Where(
          Parent().In(ElementType.ATTRIBUTE_LIST))
        .Return(IntervalFormatType.OnlyEmpty)
        .StartAlternating()
        .Where(
          Left().In(FSharpTokenType.LBRACK_LESS),
          Right().In(ElementType.ATTRIBUTE))
        .Build()
        .Where(
          Left().In(ElementType.ATTRIBUTE),
          Right().In(FSharpTokenType.GREATER_RBRACK))
        .Build();

      Describe<FormattingRule>()
        .Name("ErrorNodes")
        .Group(AllRuleGroup)
        .Priority(1000)
        .Where(
          Right()
            .In(ElementType.FROM_ERROR_EXPR, ElementType.FROM_ERROR_PAT)
            .Or().In(ElementType.CHAMELEON_EXPRESSION).Satisfies((node, _) => node.IsZeroLength)
        )
        .Return(IntervalFormatType.ReallyDoNotChangeAnything)
        .Build()
        .AndViceVersa()
        .Build();

      Describe<FormattingRule>()
        .Group(LineBreaksRuleGroup)
        .Name("LineBreakAfterTypeReprAccessModifier")
        .Where(
          Parent()
            .In(ElementBitsets.SIMPLE_TYPE_REPRESENTATION_BIT_SET)
            .Satisfies((node, _) => ((ISimpleTypeRepresentation) node.Node).AccessModifier != null),
          Right().In(ElementBitsets.ENUM_CASE_LIKE_DECLARATION_BIT_SET).Satisfies(IsFirstNodeOfItsType))
        .Switch(settings => settings.LineBreakAfterTypeReprAccessModifier,
          When(true).Return(IntervalFormatType.NewLine))
        .Build();

      Describe<FormattingRule>()
        .Group(LineBreaksRuleGroup)
        .Name("NewLineBetweenMembers")
        .Return(IntervalFormatType.NewLine)
        .StartAlternating()
        .Where(
          Right().In(ElementBitsets.MODULE_MEMBER_BIT_SET.Union(ElementBitsets.TYPE_BODY_MEMBER_DECLARATION_BIT_SET)))
        .Build()
        .Where(
          Left().In(ElementBitsets.MODULE_MEMBER_BIT_SET.Union(ElementBitsets.TYPE_BODY_MEMBER_DECLARATION_BIT_SET)),
          Right().Not().In(FSharpTokenType.SEMICOLON))
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
            .Satisfies((node, _) => ((IRecordFieldBinding) node.Node).Semicolon != null),
          Right().HasType(ElementType.RECORD_FIELD_BINDING))
        .Return(IntervalFormatType.Space)
        .Build();

      Describe<FormattingRule>()
        .Group(LineBreaksRuleGroup)
        .Name("LineBreaksBetweenRecordBindings")
        .Where(
          Left()
            .HasType(ElementType.RECORD_FIELD_BINDING)
            .Satisfies((node, _) => ((IRecordFieldBinding) node.Node).Semicolon == null),
          Right().HasType(ElementType.RECORD_FIELD_BINDING))
        .Return(IntervalFormatType.NewLine)
        .Build();

      Describe<FormattingRule>()
        .Group(LineBreaksRuleGroup)
        .Name("NewLineAfterPrecedingAttrList")
        .Where(
          Parent().In(ElementBitsets.MODULE_DECLARATION_BIT_SET.Union(ElementBitsets.F_SHARP_TYPE_OR_EXTENSION_DECLARATION_BIT_SET).Union(ElementBitsets.TYPE_BODY_MEMBER_DECLARATION_BIT_SET)),
          Left().In(ElementType.ATTRIBUTE_LIST),
          Right().In(ElementType.ATTRIBUTE_LIST, FSharpTokenType.MODULE, FSharpTokenType.TYPE, FSharpTokenType.AND, FSharpTokenType.OVERRIDE, FSharpTokenType.MEMBER))
        .Return(IntervalFormatType.NewLine)
        .Build();

      Describe<FormattingRule>()
        .Group(SpaceRuleGroup)
        .Name("SpacesAroundAttrList")
        .Where(Left().In(ElementType.ATTRIBUTE_LIST))
        .Return(IntervalFormatType.Space)
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
          .Union(ElementType.F_SHARP_TYPE_DECLARATION)
          .Union(ElementBitsets.MODULE_MEMBER_BIT_SET)
          .Union(ElementBitsets.F_SHARP_TYPE_MEMBER_DECLARATION_BIT_SET)
          .Union(ElementBitsets.TYPE_REPRESENTATION_BIT_SET);

      var noBlankLineAfterNodeTypes =
        new NodeTypeSet(
          FSharpTokenType.EQUALS,
          FSharpTokenType.IDENTIFIER,
          FSharpTokenType.CLASS,
          FSharpTokenType.INTERFACE,
          FSharpTokenType.STRUCT,
          FSharpTokenType.WITH,
          FSharpTokenType.PRIVATE,
          FSharpTokenType.INTERNAL,
          FSharpTokenType.PUBLIC
        );

      var noBlankLineBeforeNodeTypes =
        ElementBitsets.TYPE_REPRESENTATION_BIT_SET
          .Union(
            FSharpTokenType.RBRACE
          );

      // todo: group member kinds, check same group instead
      bool AllowsNoBlankLine(NodeType nodeType, NodeType nextNodeType)
      {
        return nodeType == ElementType.LET_BINDINGS_DECLARATION && nextNodeType == ElementType.EXPRESSION_STATEMENT ||
               nodeType == ElementType.EXPRESSION_STATEMENT&& nextNodeType == ElementType.LET_BINDINGS_DECLARATION ||
               nodeType == ElementType.ABSTRACT_MEMBER_DECLARATION && nextNodeType == ElementType.MEMBER_DECLARATION;
      }

      Describe<BlankLinesAroundNodeRule>()
        .AddNodesToGroupBefore(Node().In(Comments))
        .AddNodesToGroupAfter(Node().In(Comments))
        .AllowedNodesBefore(Node().Satisfies((node, _) => !noBlankLineAfterNodeTypes[node.NodeType]))
        .AllowedNodesAfter(Node().Satisfies((node, _) => !noBlankLineBeforeNodeTypes[node.NodeType]))
        .Priority(1)
        .StartAlternating()

        .Name("BlankLinesAroundDeclarations")
        .Where(Node().In(declarations))
        .MinBlankLines(it => it.BlankLinesAroundMultilineModuleMembers)
        .MinBlankLinesForSingleLine(it => it.BlankLinesAroundSingleLineModuleMember)
        .Build()

        .Name("BlankLinesAroundDifferentModuleMemberKinds")
        .Where(Node().In(declarations))
        .MinBlankLines(it => it.BlankLinesAroundDifferentModuleMemberKinds)
        .AdditionalCheckForBlankLineAfter((node, _) =>
          node.GetNextMeaningfulSibling().NodeType is var nextNodeType &&
          nextNodeType != node.NodeType && 
          !AllowsNoBlankLine(node.NodeType, nextNodeType) && 
          declarations[nextNodeType])
        .AdditionalCheckForBlankLineBefore((node, _) =>
          node.GetPreviousMeaningfulSibling().NodeType is var prevNodeType &&
          !AllowsNoBlankLine(prevNodeType, node.NodeType) &&
          prevNodeType != node.NodeType && declarations[prevNodeType])
        .Build()

        .AllowedNodesBefore(Node().Satisfies((_, _) => true))
        .Name("BlankLinesBeforeFirstTopLevelModuleMember")
        .Where(
          Parent().In(ElementBitsets.TOP_LEVEL_MODULE_LIKE_DECLARATION_BIT_SET),
          Node().In(ElementBitsets.MODULE_MEMBER_BIT_SET)
            .Satisfies(IsFirstNodeOfTypeSet(ElementBitsets.MODULE_MEMBER_BIT_SET, false)))
        .MinBlankLinesBefore(it => it.BlankLinesBeforeFirstModuleMemberInTopLevelModule)
        .Build()

        .AllowedNodesBefore(Node().Satisfies((_, _) => true))
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
          Node().HasRole(parameters.childRole).Or().In(FSharpTokenType.LINE_COMMENT, FSharpTokenType.BLOCK_COMMENT).Before(Node().HasRole(parameters.childRole))) // todo: add set
        .Return(IndentType.External)
        .Build();
    }

    private void DescribeLineBreakInDeclarationWithEquals(string name,
      IBuilderAction<IBlankWithSinglePattern> declarationPattern,
      ChildBuilder<IBlankWithSinglePattern, NodePatternBlank> afterEqualsNodesPattern) =>
      DescribeLineBreakInNode(name, declarationPattern, afterEqualsNodesPattern.Satisfies((node, _) =>
          node.GetPreviousMeaningfulSibling().GetTokenType() == FSharpTokenType.EQUALS),
          // Uncomment this line to take END_OF_LINE/NEXT_LINE brace style into account
          // Don't forget to change
          // .FormatBeforeLBrace(false)
          // to
          // .FormatBeforeLBrace(false, formatBeforeLBraceUnlessSingleLine: true)
          // node.GetPreviousMeaningfulSibling().GetTokenType() == FSharpTokenType.EQUALS && node.FirstChild.NodeType != FSharpTokenType.LBRACE),
        key => key.DeclarationBodyOnTheSameLine, key => key.KeepExistingLineBreakBeforeDeclarationBody);

    private void DescribeLineBreakInNode(string name,
      IBuilderAction<IBlankWithSinglePattern> containingNodesPattern,
      IBuilderAction<IBlankWithSinglePattern> afterEqualsNodesPattern,
      Expression<Func<FSharpFormatSettingsKey, object>> onSameLine,
      Expression<Func<FSharpFormatSettingsKey, object>> keepExistingLineBreak)
    {
      var containingNodes = containingNodesPattern.BuildBlank();
      var equalsBeforeNodes = afterEqualsNodesPattern.BuildBlank();

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

    // Handles the case where the format region doesn't include members at the end,
    // and we can't move and have to align the members being formatted
    private void FixPartialSelectionMemberIndentingRule(NodeType parentType, NodeTypeSet memberTypes)
    {
      Describe<IndentingRule>()
        .Name("Partial selection indenting rule")
        .Where(
          Parent().In(parentType),
          Node() // the node is the starting node for the rule
            .In(memberTypes.Union(Comments))
            .Satisfies((node, _) =>
            {
              // This case should be handled by alignment, not this rule
              // type T() = member this.P = 1
              //            member this.P2 = 1
              if (!node.HasNewLineBefore())
                return false;
              
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

                  if (i.HasNewLineBefore())
                    foundComment = true;
                  
                  continue;
                }

                if (memberTypes[i.NodeType])
                  return i == node && !foundComment || ourNodeIsComment;

                if (!i.IsWhitespace) // looks like it's handling `=`
                {
                  foundComment = false;
                  if (ourNodeIsComment)
                    return false; // if the comment is before `=`/keyword/name, can't be the starting node for this rule 
                }
              }

              return false;
            }))
        .CloseNodeGetter((node, _) => GetLastNodeOfTypeSet(memberTypes, node))
        .Calculate((node, context) => // node is Left()/Node()
        {
          // Formatter engine passes nulls once for caching internal/external intervals as an optimization.
          if (node == null || context == null)
            return new ConstantOptionNode(new IndentOptionValue(IndentType.StartAtExternal | IndentType.EndAtExternal));

          var treeNode = (VirtNode)node;

          var closingNode = GetLastNodeOfTypeSet(memberTypes, treeNode);
          FormatTask lastTask = null;
          for (var i = context.FormatTasks.Length - 1; i >= 0; i--)
          {
            var it = context.FormatTasks[i];
            if (it.Profile != CodeFormatProfile.NO_REINDENT)
            {
              lastTask = it;
              break;
            }
          }

          var offsetLast = lastTask == null ? TreeOffset.Zero : new VirtNode(context, lastTask.LastElement).GetTreeStartOffset();

          if (closingNode.GetTreeStartOffset() > offsetLast)
            return
              new ConstantOptionNode(
                new IndentOptionValue(
                  IndentType.AbsoluteIndent | IndentType.StartAtExternal | IndentType.EndAtExternal |
                  IndentType.NonSticky | IndentType.NonAdjustable,
                  0, closingNode.CalcLineIndent(context.TabWidth, true)));

          // todo: try using the following for nodes without further indent:
          //     IndentType.NoIndentAtExternal
          //     or maybe startAtExt + multiplier 0

          return new ConstantOptionNode(
            new IndentOptionValue(IndentType.NoIndentAtExternal | IndentType.EndAtExternal | IndentType.NonSticky));
        }).Build();
    }

    private static bool IndentElseExpr(VirtNode elseExpr, CodeFormattingContext context) =>
      elseExpr.GetPreviousMeaningfulSibling().IsFirstOnLine() && elseExpr.NodeOrNull is not IElifExpr;

    private static bool AreAligned(VirtNode first, VirtNode second, IWhitespaceChecker checker) =>
      first.Node.CalcLineIndent(checker, true) == second.Node.CalcLineIndent(checker, true);

    private static bool IsPipeOperator(VirtNode node, CodeFormattingContext context) =>
      node.NodeOrNull is IReferenceExpr refExpr && FSharpPredefinedType.PipeOperatorNames.Contains(refExpr.ShortName);
  }
  
  /// <summary>
  /// Usual continuous indent rule:
  ///   Start
  ///     next line
  ///   Exception
  ///   resume continuous indent // returning to the start level
  ///     next line after resume
  ///
  /// This (F#) continuous indent rule
  ///   Start
  ///     next line
  ///   Exception
  ///     resume continuous indent // continuing from last line before exception
  ///     next line after resume
  ///
  /// Example: `|` in or-patterns:
  /// match () with
  /// | a
  /// | b -> ()
  /// </summary>
    public class FSharpContinuousIndentRule : RuleBlankBase,
    IBlankWithPriority, IBlankThatBuilds
  {
    public int Priority { get; set; }
    public List<IBuilderAction<IBlankWithTwoPatterns>[]> Exceptions { get; private set; } = new();
    public IScalarSetting MultiplierSetting { get; set; }

    protected override void ShallowToDeepCopy()
    {
      Exceptions = new(Exceptions);
    }

    public void Build<TContext, TSettingsKey>(FormatterInfoProviderWithFluentApi<TContext, TSettingsKey> provider)
      where TContext : CodeFormattingContext
      where TSettingsKey : FormatSettingsKeyBase
    {
      LinkPatterns();

      Assertion.AssertNotNull(Pattern.BuildPattern().GetNodeTypes(), "Should have node types in pattern, otherwise performance would be too bad");

      provider.Describe<DelayedIndentingRule>()
        .Name(Name)
        .Priority(Priority)
        .CloseNodeGetter((node, _) => node.Parent.LastChild)
        .Switch(MultiplierSetting, ContinuousIndentRule.ContinuousIndentOptions(provider))
        .IfSettingIsNullThenChooseValueFor(1)
        .CheckUntilNodeRightSide(false)
        .Where(
          provider.Node().Satisfies2((it, _) => it.Parent.FirstChild == it),
          provider.Parent().Is(Pattern))
        .WhereToStartCheck(provider.Node().Is(Pattern))
        .CheckUntilNodeGetter((node, _) => node.FirstChild)
        .Build();

      if (Exceptions.Count > 0)
      {
        foreach (var exception in Exceptions)
        {
          var blank = exception.BuildBlankWithTwoPatterns();
          provider.Describe<IndentingRule>().Name("Exception :)").Priority(Priority + 100000)
            .Where(provider.Left().Is(blank.First), provider.Right().Is(blank.Second)/*, provider.Parent().Is(Pattern)*/)
            .Return(IndentType.External | IndentType.NonSticky, -1).Build();
        }
      }
    }
  }
    
  public static class ContinuousIndentRuleEx
  {
    public static TEnvelope AddException<TEnvelope>(this TEnvelope builder, params IBuilderAction<IBlankWithTwoPatterns>[] buiders)
      where TEnvelope: IBuilder<FSharpContinuousIndentRule>
    {
      builder.Obj.Exceptions.Add(buiders);
      return builder;
    }
  }

}
