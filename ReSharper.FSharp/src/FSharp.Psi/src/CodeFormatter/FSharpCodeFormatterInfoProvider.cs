using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
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
        ("If_ThenExpr", ElementType.IF_THEN_ELSE_EXPR, IfThenElseExpr.THEN_EXPR),
        ("Elif_ThenExpr", ElementType.ELIF_EXPR, ElifExpr.THEN_EXPR),
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
          .Name("If_ElseExprIndent")
          .Where(
            Parent().HasType(ElementType.IF_THEN_ELSE_EXPR),
            Node()
              .HasRole(IfThenElseExpr.ELSE_CLAUSE)
              .Satisfies((node, context) =>
                node.GetPreviousMeaningfulSibling().IsFirstOnLine(context.CodeFormatter) &&
                !(node is IElifExpr)))
          .Return(IndentType.External)
          .Build();

        Describe<IndentingRule>()
          .Name("Elif_ElseExprIndent")
          .Where(
            Parent().HasType(ElementType.ELIF_EXPR),
            Node()
              .HasRole(ElifExpr.ELSE_CLAUSE)
              .Satisfies((node, context) =>
                node.GetPreviousMeaningfulSibling().IsFirstOnLine(context.CodeFormatter)
                && !(node is IElifExpr)))
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
  }
}
