using System;
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

    protected readonly NodeTypeSet Comments =
      new NodeTypeSet(FSharpTokenType.LINE_COMMENT, FSharpTokenType.BLOCK_COMMENT);

    protected readonly NodeTypeSet AccessModifiers =
      new NodeTypeSet(FSharpTokenType.PUBLIC, FSharpTokenType.INTERNAL, FSharpTokenType.PRIVATE);

    protected readonly NodeTypeSet ExpressionsWithChameleon =
      ElementBitsets.F_SHARP_EXPRESSION_BIT_SET.Union(ElementType.CHAMELEON_EXPRESSION);

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
      // todo: use parens rules for parens (closing bracket without indent)
      Describe<ContinuousIndentRule>()
        .Name("ContinuousIndent1")
        .Where(Node().In(ElementBitsets.DO_LIKE_EXPR_BIT_SET.Union(ElementType.NESTED_MODULE_DECLARATION,
          ElementType.F_SHARP_TYPE_DECLARATION, ElementType.ENUM_CASE_DECLARATION, ElementType.UNION_CASE_DECLARATION,
          ElementType.ATTRIBUTE_LIST, ElementType.UNIT_EXPR, ElementType.UNIT_PAT, ElementType.LET_BINDINGS_DECLARATION, 
          ElementType.MODULE_ABBREVIATION_DECLARATION)))
        .AddException(Node().In(ElementType.ATTRIBUTE_LIST))
        .Build();

      // todo: union with the previous rule
      Describe<ContinuousIndentRule>()
        .Name("ContinuousIndent2")
        .Where(Node().In(ElementType.DO_STATEMENT).Satisfies(IsExplicitDo))
        .AddException(Node().In(ElementType.ATTRIBUTE_LIST))
        .Build();

      Describe<IndentingRule>()
        .Name("TypeRepr_Accessibility")
        .Where(
          Parent().In(ElementBitsets.SIMPLE_TYPE_REPRESENTATION_BIT_SET),
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
        .Name("PrefixAppExprIndent")
        .Where(
          Parent().HasType(ElementType.PREFIX_APP_EXPR),
          Node()
            .HasRole(PrefixAppExpr.ARG_EXPR)
            .Satisfies((node, context) =>
              !(node is IComputationExpr) ||
              !node.ContainsLineBreak(context.CodeFormatter)))
        .Return(IndentType.External)
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
        .Name("MatchClauseExprIndent")
        .Where(
          Node().HasRole(MatchClause.EXPR),
          Parent()
            .HasType(ElementType.MATCH_CLAUSE)
            .Satisfies((node, context) =>
            {
              if (!(node is IMatchClause matchClause))
                return false;

              var expr = matchClause.Expression;
              return !IsLastNodeOfItsType(node, context) ||
                     !AreAligned(matchClause, expr, context.CodeFormatter);
            }))
        .Return(IndentType.External)
        .Build();

      Describe<IndentingRule>()
        .Name("MatchClauseWhenExprIndent")
        .Where(
          Parent().HasType(ElementType.MATCH_CLAUSE),
          Node().HasRole(MatchClause.WHEN_CLAUSE))
        .Return(IndentType.External, 2)
        .Build();

      Describe<IndentingRule>()
        .Name("DoDeclIndent")
        .Where(
          Parent().HasType(ElementType.DO_STATEMENT).Satisfies(IsExplicitDo),
          Node().HasRole(DoStatement.CHAMELEON_EXPR))
        .Return(IndentType.External)
        .Build();
    }

    private void Aligning()
    {
      var alignmentRulesParameters = new[]
      {
        ("MatchClauses", ElementType.MATCH_EXPR),
        ("UnionRepresentation", ElementType.UNION_REPRESENTATION),
        ("EnumCases", ElementType.ENUM_REPRESENTATION),
        ("SequentialExpr", ElementType.SEQUENTIAL_EXPR), // todo: do stmt
        ("BinaryExpr", ElementType.BINARY_APP_EXPR),
        ("RecordDeclaration", ElementType.RECORD_FIELD_DECLARATION_LIST),
        ("RecordExprBindings", ElementType.RECORD_FIELD_BINDING_LIST),
        ("MemberDeclarationList", ElementType.MEMBER_DECLARATION_LIST),
        ("TypeMemberDeclarationList", ElementType.TYPE_MEMBER_DECLARATION_LIST),
      };

      alignmentRulesParameters
        .ToList()
        .ForEach(DescribeSimpleAlignmentRule);

      Describe<IndentingRule>().Name("EnumCases")
        .Where(Parent().In(ElementType.ENUM_REPRESENTATION, ElementType.UNION_REPRESENTATION),
          Left().In(ElementType.ENUM_CASE_DECLARATION).Satisfies(IsFirstNodeOfItsType))
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

    private void BlankLines()
    {
      // todo: separate setting for enum like cases?
      var declarations =
        ElementBitsets.MODULE_MEMBER_BIT_SET
          .Union(ElementBitsets.ENUM_CASE_LIKE_DECLARATION_BIT_SET)
          .Union(ElementType.F_SHARP_TYPE_DECLARATION);

      Describe<BlankLinesAroundNodeRule>()
        .AddNodesToGroupBefore(Node().In(Comments))
        .AddNodesToGroupAfter(Node().In(Comments))
        .AllowedNodesBefore(Node().Satisfies((node, checker) => node.NodeType != FSharpTokenType.EQUALS))
        .AllowedNodesAfter(Node().Satisfies((node, checker) => true))
        .Priority(1)
        .StartAlternating()
        .Name("BlankLinesAroundModuleMembers")
        .Where(Node().In(declarations))
        .MinBlankLines(it => it.BlankLinesAroundMultilineModuleMembers)
        .MinBlankLinesForSingleLine(it => it.BlankLinesAroundSingleLineModuleMember)
        .Build()
        .Name("BlankLinesAroundDifferentModuleMemberTypes")
        .Where(Node().In(ElementBitsets.MODULE_MEMBER_BIT_SET))
        .MinBlankLines(it => it.BlankLinesAroundDifferentModuleMembers)
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
            .Satisfies(IsFirstNodeOfTypeSet(ElementBitsets.MODULE_MEMBER_BIT_SET)))
        .MinBlankLinesBefore(it => it.BlankLinesBeforeFirstTopLevelModuleMember)
        .Build()
        .Name("BlankLinesBeforeFirstNestedModuleMember")
        .Where(
          Parent().In(ElementType.NESTED_MODULE_DECLARATION),
          Node().In(ElementBitsets.MODULE_MEMBER_BIT_SET)
            .Satisfies(IsFirstNodeOfTypeSet(ElementBitsets.MODULE_MEMBER_BIT_SET)))
        .MinBlankLinesBefore(it => it.BlankLinesBeforeFirstNestedModuleMember)
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

    private void Formatting()
    {
      Describe<FormattingRule>()
        .Name("EnumCaseSpaces")
        .Group(SpaceRuleGroup)
        .Where(Parent().In(ElementType.ENUM_CASE_DECLARATION, ElementType.UNION_CASE_DECLARATION,
          ElementType.UNION_CASE_FIELD_DECLARATION_LIST))
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
        .Name("NoSpaceBeforeComma")
        .Group(SpaceRuleGroup)
        .Where(Right().In(FSharpTokenType.COMMA))
        .Return(IntervalFormatType.Empty)
        .Build();

      Describe<FormattingRule>()
        .Name("NoSpaceInUnit")
        .Group(SpaceRuleGroup)
        .Where(
          Parent().In(ElementType.UNIT_EXPR, ElementType.UNIT_PAT),
          Left().In(FSharpTokenType.LPAREN),
          Right().In(FSharpTokenType.RPAREN))
        .Return(IntervalFormatType.OnlyEmpty)
        .Build();

      Describe<FormattingRule>()
        .Name("KeepUserLineBreaks")
        .FormatHighlighting(FormatterImplHelper.ProhibitHighlighting) // todo: other cases
        .Group(UserLineBreaksRuleGroup)
        .Priority(0)
        .Switch(it => it.KeepUserLinebreaks,
          When(false).Return(IntervalFormatType.RemoveUserNewLines)) // todo: generator mode
        .Build();

      Describe<FormattingRule>()
        .Name("LineBreakBeforeModuleMember")
        .Group(LineBreaksRuleGroup)
        .Where(Right().In(ElementBitsets.MODULE_MEMBER_BIT_SET))
        .Return(IntervalFormatType.NewLine).Build();

      Describe<FormattingRule>()
        .Group(LineBreaksRuleGroup)
        .Name("LineBreakAfterTypeReprAccessModifier")
        .Where(
          Parent().In(ElementBitsets.SIMPLE_TYPE_REPRESENTATION_BIT_SET)
            .Satisfies((node, context) => ((ISimpleTypeRepresentation) node).AccessModifier != null),
          Right().In(ElementBitsets.ENUM_CASE_LIKE_DECLARATION_BIT_SET.Union(ElementType.RECORD_FIELD_DECLARATION)))
        .Switch(settings => settings.LineBreakAfterTypeReprAccessModifier,
          When(true).Return(IntervalFormatType.NewLine))
        .Build();

      // todo: type U = C of int
      // todo: single/nulti line module abbr
      // Describe<FormattingRule>()
      //   .Group(LineBreaksRuleGroup)
      //   .Name("LineBreakAfterEqualsInTypeDecl")
      //   .Where(Parent().In(ElementType.F_SHARP_TYPE_DECLARATION),
      //     Right().In(ElementBitsets.TYPE_REPRESENTATION_BIT_SET).Satisfies((node, context) =>
      //       node.GetPreviousMeaningfulSibling()?.GetTokenType() == FSharpTokenType.EQUALS))
      //   .Switch(settings => settings.LineBreakAfterEqualsInTypeDecl, When(true).Return(IntervalFormatType.NewLine))
      //   .Build();

      DescribeLineBreakInNode("Declarations", Node().In(ElementType.F_SHARP_TYPE_DECLARATION),
        Node().In(ElementBitsets.TYPE_REPRESENTATION_BIT_SET).Satisfies((node, context) =>
          node.GetPreviousMeaningfulSibling()?.GetTokenType() == FSharpTokenType.EQUALS),
        key => key.DeclarationBodyOnTheSameLine, key => key.KeepExistingLineBreakBeforeDeclarationBody);

      DescribeLineBreakInNode("DoLikeClause", Node().In(ElementBitsets.DO_LIKE_EXPR_BIT_SET),
        Node().In(ExpressionsWithChameleon), key => key.DoLikeExprOnTheSameLine, 
        key => key.KeepExistingLineBreakInDoLikeExpr);

      DescribeLineBreakInNode("DoLikeClause", 
        Node().In(ElementType.DO_STATEMENT).Satisfies((node, context) => !((IDoStatement) node).IsImplicit),
        Node().In(ExpressionsWithChameleon), key => key.DoLikeExprOnTheSameLine,
        key => key.KeepExistingLineBreakInDoLikeExpr);

      Describe<FormattingRule>()
        .Group(SpaceRuleGroup)
        .Name("SpaceAfterPrimaryConstructorDecl")
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
      
      
      var invocationRule = GetParsRuleBuilder()
        // .SpacesBeforeLParSetting(x => x.SPACE_BEFORE_METHOD_CALL_PARENTHESES)
        // .SpacesInsideParsSetting(x => x.SPACE_WITHIN_METHOD_CALL_PARENTHESES)
        // .SpacesBeforeEmptyParsSetting(x => x.SPACE_BEFORE_EMPTY_METHOD_CALL_PARENTHESES)
        // .SpacesInsideEmptyParsSetting(x => x.SPACE_WITHIN_EMPTY_METHOD_CALL_PARENTHESES)
        .WrapSetting(x => x.WrapArgumentsStyle)

        // todo: check style guides
        // .MaxElementsOnLine(x => x.MAX_INVOCATION_ARGUMENTS_ON_LINE)

        // .WrapBeforeLParSetting(x => x.WRAP_BEFORE_INVOCATION_LPAR)

        .WrapAfterLParSetting(x => x.WrapAfterInvocationLparen)
        .WrapBeforeRParSetting(x => x.WrapBeforeInvocationRparen)
        .KeepExistingArrangementOfParsSetting(x => x.KeepExistingInvocationParensArrangement)
        .IndentParsSetting(x => x.IndentInvocationPars)
        .ChopAroundPars(true)
        .KeepTogetherPolicy(KeepTogetherPolicy.ExceptTheOnlyElement)
        .ExcellentPlaceToWrapAroundPars(true)
        .StartAlternating();

      invocationRule
        .Name("INVOCATION_EXPRESSION()")
        .Where(
          Node()
            .In(ElementType.PAREN_EXPR)
            .Satisfies((node, checker) => true /*!NameofUtil.CanBeNameofOperatorIdentifier(((IInvocationExpression) node).InvokedExpression)*/) // todo: upper case rule from Fantomas
            // .Or()
            // .In(ElementType.OBJECT_CREATION_EXPRESSION)
            // .Satisfies((node, checker) => ((IObjectCreationExpression) node).TypeUsage != null)
            // .Or()
            // .In(ElementType.CONSTRUCTOR_INITIALIZER, ElementType.EXTENDED_TYPE))
            )
        .CommaAndElementParent(ElementType.TUPLE_EXPR)
        .Elements(ElementBitsets.F_SHARP_EXPRESSION_BIT_SET)
        // .DontChopIf(lpar => lpar.Parent != null && ((ICSharpArgumentsOwner) lpar.Parent).Arguments.Count <= 1)
        .AlignSetting(x => x.AlignFirstArgByParen)
        // .IndentByOneChar(true)
        .Build();
    }

    private IBuilderWithProvider<CodeFormattingContext, FSharpFormatSettingsKey, ParenthesisRuleWithWrapping> GetParsRuleBuilder()
    {
      return Describe<ParenthesisRuleWithWrapping>()
        .LPar(FSharpTokenType.LPAREN)
        .RPar(FSharpTokenType.RPAREN)
        // .ParsSpacesHighlighting(FSharpFormatHighlightingIds.BadParensSpaces)
        // .ParsLineBreaksHighlighting(CSharpFormatHighlightingIds.BadParensLineBreaks)
        .Comma(FSharpTokenType.COMMA)
        // .ListLineBreaksHighlighting(CSharpFormatHighlightingIds.BadListLineBreaks)
        // .WrapBeforeCommaSetting(x => x.WRAP_BEFORE_COMMA)
        .Comments(Comments)
        // .ContIndentMultiplierSetting(x => x.CONTINUOUS_INDENT_MULTIPLIER)
        // .UseContIndentSetting(x => x.USE_CONTINUOUS_INDENT_INSIDE_PARENS)
        .WrapBeforeRParByDefault(false);
    }

    
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
          When(true).Return(IntervalFormatType.DoNotRemoveUserNewLines),
          When(false)
            .Switch(onSameLine,
              When(PlaceOnSameLineAsOwner.NEVER).Return(IntervalFormatType.NewLine),
              When(PlaceOnSameLineAsOwner.ALWAYS).Return(IntervalFormatType.RemoveUserNewLines),
              When(PlaceOnSameLineAsOwner.IF_OWNER_IS_SINGLE_LINE)
                .Return(IntervalFormatType.RemoveUserNewLines | IntervalFormatType.InsertNewLineConditionally)))
        .Build();

      // todo: why does it work after comment and new line?

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

    private void DescribeSimpleAlignmentRule((string name, CompositeNodeType nodeType) parameters)
    {
      Describe<IndentingRule>()
        .Name(parameters.name + "Alignment")
        .Where(Node().HasType(parameters.nodeType))
        .Return(IndentType.AlignThrough)
        .Build();
    }

    private static bool IndentElseExpr(ITreeNode elseExpr, CodeFormattingContext context) =>
      elseExpr.GetPreviousMeaningfulSibling().IsFirstOnLine(context.CodeFormatter) && !(elseExpr is IElifExpr);

    private static bool AreAligned(ITreeNode first, ITreeNode second, IWhitespaceChecker whitespaceChecker) =>
      first.CalcLineIndent(whitespaceChecker) == second.CalcLineIndent(whitespaceChecker);

    private static bool IsPipeOperator(ITreeNode node, CodeFormattingContext context) =>
      node is IReferenceExpr refExpr && FSharpPredefinedType.PipeOperatorNames.Contains(refExpr.ShortName);

    private static bool IsExplicitDo(ITreeNode node, CodeFormattingContext context) =>
      node is IDoStatement { IsImplicit: false };
  }
}
