using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  public abstract class FSharpExpressionBase : FSharpCompositeElement, IFSharpExpression, IArgument
  {
    private readonly CachedPsiValue<DeclaredElementInstance<IParameter>> myMatchingParameter = new FileCachedPsiValue<DeclaredElementInstance<IParameter>>();

    public virtual ConstantValue ConstantValue => ConstantValue.BAD_VALUE;
    public virtual bool IsConstantValue() => false;

    public ExpressionAccessType GetAccessType() => ExpressionAccessType.None;

    public virtual IType Type() => TypeFactory.CreateUnknownType(GetPsiModule());
    public IExpressionType GetExpressionType() => Type();
    public IType GetImplicitlyConvertedTo() => Type();

    IInvocationInfo IArgumentInfo.Invocation => null;

    private IFSharpMethodInvocationUtil InvocationUtil =>
      this.GetSolution().GetComponent<LanguageManager>()
        .TryGetService<IFSharpMethodInvocationUtil>(FSharpLanguage.Instance);

    DeclaredElementInstance<IParameter> IArgumentInfo.MatchingParameter =>
      myMatchingParameter.GetValue(this, () =>
        InvocationUtil?.GetMatchingParameter(this) is var parameter && parameter != null
          ? new DeclaredElementInstance<IParameter>(parameter)
          : null);

    IExpression IArgument.Expression =>
      InvocationUtil?.GetNamedArg(this) is var parameter && parameter != null
        ? this is IBinaryAppExpr binaryAppExpr ? binaryAppExpr.RightArgument : null
        : this;

    bool IArgumentInfo.IsExtensionInvocationQualifier => false;
    IPsiModule IArgumentInfo.PsiModule => GetPsiModule();
    DocumentRange IArgumentInfo.GetDocumentRange() => GetNavigationRange();
  }
}
