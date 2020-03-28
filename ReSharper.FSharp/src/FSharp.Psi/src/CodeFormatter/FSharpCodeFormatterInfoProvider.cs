using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Impl.CodeStyle;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
{
  [Language(typeof(FSharpLanguage))]
  public class FSharpFormatterInfoProvider :
    FormatterInfoProviderWithFluentApi<CodeFormattingContext, FSharpFormatSettingsKey>
  {
    public FSharpFormatterInfoProvider(ISettingsSchema settingsSchema) : base(settingsSchema)
    {
      var bindingAndModuleDeclIndentingRulesParameters = new[]
      {
        ("NestedModuleDeclaration", ElementType.NESTED_MODULE_DECLARATION, NestedModuleDeclaration.MODULE_MEMBER),
        ("TopBinding", ElementType.TOP_BINDING, TopBinding.CHAMELEON_EXPR),
        ("LocalBinding", ElementType.LOCAL_BINDING, LocalBinding.EXPR),
      };

      var synExprIndentingRulesParameters = new[]
      {
        ("ForExpr", ElementType.FOR_EXPR, ForExpr.DO_EXPR),
        ("ForEachExpr", ElementType.FOR_EACH_EXPR, ForEachExpr.DO_EXPR),
        ("WhileExpr", ElementType.WHILE_EXPR, WhileExpr.DO_EXPR),
        ("DoExpr", ElementType.DO_EXPR, DoExpr.EXPR),
        ("AssertExpr", ElementType.ASSERT_EXPR, AssertExpr.EXPR),
        ("LazyExpr", ElementType.LAZY_EXPR, LazyExpr.EXPR),
        ("ComputationExpr", ElementType.COMPUTATION_EXPR, ComputationExpr.EXPR),
        ("SetExpr", ElementType.SET_EXPR, SetExpr.RIGHT_EXPR),
        ("TryFinally_Try", ElementType.TRY_FINALLY_EXPR, TryFinallyExpr.TRY_EXPR),
        ("TryFinally_Finally", ElementType.TRY_FINALLY_EXPR, TryFinallyExpr.FINALLY_EXPR),
        ("TryWith_Try", ElementType.TRY_WITH_EXPR, TryWithExpr.TRY_EXPR),
        ("IfThenExpr", ElementType.IF_THEN_ELSE_EXPR, IfThenElseExpr.THEN_EXPR),
        ("ElifThenExpr", ElementType.ELIF_EXPR, ElifExpr.THEN_EXPR),
        ("MatchClauseExpr", ElementType.MATCH_CLAUSE, MatchClause.EXPR),
        ("LambdaExprBody", ElementType.LAMBDA_EXPR, LambdaExpr.EXPR),
      };

      lock (this)
      {
        bindingAndModuleDeclIndentingRulesParameters
          .Union(synExprIndentingRulesParameters)
          .ToList()
          .ForEach(DescribeSimpleIndentingRule);

        Describe<IndentingRule>()
          .Name("TryWith_WithIndent")
          .Where(
            Parent().HasType(ElementType.TRY_WITH_EXPR),
            Node().HasRole(TryWithExpr.CLAUSE))
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
                !(node is IComputationLikeExpr) ||
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
      }
    }

    public override ProjectFileType MainProjectFileType => FSharpProjectFileType.Instance;

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

    private static bool IndentElseExpr(ITreeNode elseExpr, CodeFormattingContext context) =>
      elseExpr.GetPreviousMeaningfulSibling().IsFirstOnLine(context.CodeFormatter) && !(elseExpr is IElifExpr);
  }
}
