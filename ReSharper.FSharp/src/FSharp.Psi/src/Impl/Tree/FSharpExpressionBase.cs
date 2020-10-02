using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public abstract class FSharpExpressionBase : FSharpCompositeElement, IFSharpExpression, IArgument
  {
    public virtual ConstantValue ConstantValue => ConstantValue.BAD_VALUE;
    public virtual bool IsConstantValue() => false;

    public ExpressionAccessType GetAccessType() => ExpressionAccessType.None;

    public virtual IType Type() => this.GetExpressionTypeFromFcs();

    public IExpressionType GetExpressionType() => Type();
    public IType GetImplicitlyConvertedTo() => Type();

    IInvocationInfo IArgumentInfo.Invocation => null;

    private IFSharpMethodInvocationUtil InvocationUtil =>
      this.GetSolution().GetComponent<LanguageManager>()
        .TryGetService<IFSharpMethodInvocationUtil>(FSharpLanguage.Instance);

    DeclaredElementInstance<IParameter> IArgumentInfo.MatchingParameter =>
      InvocationUtil?.GetMatchingParameter(this) is { } parameter
        ? new DeclaredElementInstance<IParameter>(parameter)
        : null;

    IExpression IArgument.Expression =>
      InvocationUtil?.GetNamedArg(this) is { }
        ? this is IBinaryAppExpr binaryAppExpr ? binaryAppExpr.RightArgument : null
        : this;

    bool IArgumentInfo.IsExtensionInvocationQualifier => false;
    IPsiModule IArgumentInfo.PsiModule => GetPsiModule();
    DocumentRange IArgumentInfo.GetDocumentRange() => GetNavigationRange();
  }
}
