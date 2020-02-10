using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Impl.CodeStyle;

namespace JetBrains.ReSharper.Plugins.FSharp.Services.Formatter
{
  [Language(typeof(FSharpLanguage))]
  public class FSharpFormatterInfoProvider :
    FormatterInfoProviderWithFluentApi<CodeFormattingContext, FSharpFormatSettingsKey>
  {
    public FSharpFormatterInfoProvider(ISettingsSchema settingsSchema) : base(settingsSchema)
    {
      lock (this)
      {
        Describe<IndentingRule>()
          .Name("NestedModuleIndent")
          .Where(
            Parent().HasType(ElementType.NESTED_MODULE_DECLARATION),
            Node().HasRole(NestedModuleDeclaration.MODULE_MEMBER))
          .Return(IndentType.External)
          .Build();

        Describe<IndentingRule>()
          .Name("TopBindingIndent")
          .Where(
            Parent().HasType(ElementType.TOP_BINDING),
            Node().HasRole(TopBinding.CHAMELEON_EXPR))
          .Return(IndentType.External)
          .Build();

        Describe<IndentingRule>()
          .Name("LocalBindingIndent")
          .Where(
            Parent().HasType(ElementType.LOCAL_BINDING),
            Node().HasRole(LocalBinding.EXPR))
          .Return(IndentType.External)
          .Build();

        Describe<IndentingRule>()
          .Name("ObjectTypeDeclarationIndent")
          .Where(
            Parent().HasType(ElementType.OBJECT_TYPE_DECLARATION),
            Node().HasRole(ObjectTypeDeclaration.TYPE_MEMBER))
          .Return(IndentType.External)
          .Build();

        Describe<IndentingRule>()
          .Name("ClassDeclarationIndent")
          .Where(Parent().HasType(ElementType.CLASS_DECLARATION))
          .Return(IndentType.External)
          .Build();

        Describe<IndentingRule>()
          .Name("DiscriminatedUnionDeclarationIndent")
          .Where(
            Parent().HasType(ElementType.UNION_REPRESENTATION),
            Node().HasRole(UnionRepresentation.UNION_CASE))
          .Return(IndentType.External)
          .Build();

        Describe<IndentingRule>()
          .Name("MemberDeclarationIndent")
          .Where(
            Parent().HasType(ElementType.MEMBER_DECLARATION),
            Node().HasRole(MemberDeclaration.CHAMELEON_EXPR))
          .Return(IndentType.External)
          .Build();

        Describe<IndentingRule>()
          .Name("IfThenElseExprIndent")
          .Where(
            Parent().HasType(ElementType.IF_THEN_ELSE_EXPR),
            Node().HasRole(IfThenElseExpr.THEN_EXPR).Or().HasRole(IfThenElseExpr.ELSE_EXPR))
          .Return(IndentType.External)
          .Build();

        Describe<IndentingRule>()
          .Name("ForExprIndent")
          .Where(
            Parent().HasType(ElementType.FOR_EXPR),
            Node().HasRole(ForExpr.DO_EXPR))
          .Return(IndentType.External)
          .Build();

        Describe<IndentingRule>()
          .Name("ForEachExprIndent")
          .Where(
            Parent().HasType(ElementType.FOR_EACH_EXPR),
            Node().HasRole(ForEachExpr.DO_EXPR))
          .Return(IndentType.External)
          .Build();

        Describe<IndentingRule>()
          .Name("WhileExprIndent")
          .Where(
            Parent().HasType(ElementType.WHILE_EXPR),
            Node().HasRole(WhileExpr.DO_EXPR))
          .Return(IndentType.External)
          .Build();

        Describe<IndentingRule>()
          .Name("TryExprIndent")
          .Where(
            Parent().In(ElementType.TRY_WITH_EXPR, ElementType.TRY_FINALLY_EXPR),
            Node().HasRole(TryWithExpr.TRY_EXPR).Or().HasRole(TryFinallyExpr.TRY_EXPR))
          .Return(IndentType.External)
          .Build();

        Describe<IndentingRule>()
          .Name("TryFinallyExprIndent")
          .Where(
            Parent().HasType(ElementType.TRY_FINALLY_EXPR),
            Node().HasRole(TryFinallyExpr.FINALLY_EXPR))
          .Return(IndentType.External)
          .Build();

        Describe<IndentingRule>()
          .Name("TryWithExprIndent")
          .Where(
            Parent().HasType(ElementType.TRY_WITH_EXPR),
            Node().HasRole(TryWithExpr.CLAUSE))
          .Switch(
            settings => settings.IndentOnTryWith,
            When(true).Return(IndentType.External),
            When(false).Return(IndentType.None))
          .Build();

        Describe<IndentingRule>()
          .Name("DoExprIndent")
          .Where(
            Parent().HasType(ElementType.DO_EXPR),
            Node().HasRole(DoExpr.EXPR))
          .Return(IndentType.External)
          .Build();

        Describe<IndentingRule>()
          .Name("CompExprIndent")
          .Where(
            Parent().HasType(ElementType.COMPUTATION_EXPR),
            Node().HasRole(ComputationExpr.EXPR))
          .Return(IndentType.External)
          .Build();

        Describe<IndentingRule>()
          .Name("LazyExprIndent")
          .Where(
            Parent().HasType(ElementType.LAZY_EXPR),
            Node().HasRole(LazyExpr.EXPR))
          .Return(IndentType.External)
          .Build();

        Describe<IndentingRule>()
          .Name("MatchClauseExprIndent")
          .Where(
            Parent().HasType(ElementType.MATCH_CLAUSE),
            Node().HasRole(MatchClause.EXPR))
          .Return(IndentType.External)
          .Build();

        Describe<IndentingRule>()
          .Name("LetOrUseExprBindingIndent")
          .Where(
            Parent().HasType(ElementType.LET_OR_USE_EXPR),
            Node().HasRole(LetOrUseExpr.BINDING))
          .Return(IndentType.External)
          .Build();

        Describe<IndentingRule>()
          .Name("LetModuleDeclBindingIndent")
          .Where(
            Parent().HasType(ElementType.LET_MODULE_DECL),
            Node().HasRole(LetModuleDecl.BINDING))
          .Return(IndentType.External)
          .Build();

        Describe<IndentingRule>()
          .Name("ChainMethodCallIndent")
          .Where(
            Parent().HasType(ElementType.REFERENCE_EXPR),
            Node().HasType(FSharpTokenType.DOT))
          .Return(IndentType.External)
          .Build();

        Describe<IndentingRule>()
          .Name("SequentialExprAlignment")
          .Where(Node().HasType(ElementType.SEQUENTIAL_EXPR))
          .Return(IndentType.AlignThrough)
          .Build();

        Describe<IndentingRule>()
          .Name("MatchClauseAlignment")
          .Where(Node().In(ElementType.MATCH_EXPR, ElementType.MATCH_LAMBDA_EXPR))
          .Return(IndentType.AlignThrough)
          .Build();
      }
    }

    public override ProjectFileType MainProjectFileType => FSharpProjectFileType.Instance;
  }
}
