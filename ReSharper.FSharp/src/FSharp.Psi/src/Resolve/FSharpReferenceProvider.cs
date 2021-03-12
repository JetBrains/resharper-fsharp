using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CodeAnnotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Resolve
{
  [ReferenceProviderFactory]
  public class FSharpReferenceProviderFactory : IReferenceProviderFactory
  {
    public FSharpReferenceProvider ReferenceProvider { get; }
    public InvokerParameterNameAnnotationProvider ParameterNameAnnotationProvider { get; }

    public FSharpReferenceProviderFactory(Lifetime lifetime, CodeAnnotationsCache codeAnnotationsCache)
    {
      ReferenceProvider = new FSharpReferenceProvider(this);
      ParameterNameAnnotationProvider = codeAnnotationsCache.GetProvider<InvokerParameterNameAnnotationProvider>();
      Changed = new Signal<IReferenceProviderFactory>(lifetime, nameof(FSharpReferenceProviderFactory));
    }

    public IReferenceFactory CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex wordIndexForChecks) =>
      file is IFSharpFile
        ? ReferenceProvider
        : null;

    public ISignal<IReferenceProviderFactory> Changed { get; }
  }

  public class FSharpReferenceProvider : IReferenceFactory
  {
    private readonly FSharpReferenceProviderFactory myFactory;

    public FSharpReferenceProvider(FSharpReferenceProviderFactory factory) =>
      myFactory = factory;

    public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
    {
      if (element is ILiteralExpr literalExpr && literalExpr.CanBeArgument())
      {
        if (!(literalExpr.ConstantValue.Value is string value))
          return ReferenceCollection.Empty;

        // todo: check for compiler methods only prior to mapping FCS symbols
        // todo: do fast name checks

        var argument = (IArgument) literalExpr;
        var parameter = argument.MatchingParameter;
        if (parameter == null || !myFactory.ParameterNameAnnotationProvider.IsInvokerParameterName(parameter.Element))
          return ReferenceCollection.Empty;

        return ShouldReuseOld(oldReferences)
          ? oldReferences
          : new ReferenceCollection(new ParameterNameReference(literalExpr, value));
      }

      return ReferenceCollection.Empty;
    }

    private static bool ShouldReuseOld(ReferenceCollection oldReferences) =>
      oldReferences.Count == 1 && oldReferences[0] is ParameterNameReference;

    public bool HasReference(ITreeNode element, IReferenceNameContainer names) =>
      element is ILiteralExpr literalExpr && literalExpr.IsStringLiteralExpression() &&
      literalExpr.ConstantValue.Value is string value && names.Contains(value);
  }

  public class ParameterNameReference : CheckedReferenceBase<ILiteralExpr>, IReferenceFromStringLiteral
  {
    private const string OpName = nameof(ParameterNameReference);

    [NotNull] private readonly string myValue;

    public ParameterNameReference([NotNull] ILiteralExpr owner, [NotNull] string value) : base(owner) =>
      myValue = value;

    public override ResolveResultWithInfo ResolveWithoutCache()
    {
      var symbol = myOwner.CheckerService.ResolveNameAtLocation(myOwner, new[] {myValue}, OpName)?.Value?.Symbol;
      if (symbol is FSharpMemberOrFunctionOrValue { IsModuleValueOrMember: false } mfv)
        return mfv.GetLocalValueDeclaredElement(myOwner) is { } element
          ? new ResolveResultWithInfo(new SimpleResolveResult(element), ResolveErrorType.OK)
          : ResolveResultWithInfo.Ignore;

      return ResolveResultWithInfo.Ignore;
    }

    public override string GetName() => myValue;

    public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName) =>
      throw new System.NotImplementedException();

    public override TreeTextRange GetTreeTextRange() =>
      myOwner.Literal.GetTreeTextRange();

    public override IReference BindTo(IDeclaredElement element) =>
      throw new System.NotImplementedException();

    public override IReference BindTo(IDeclaredElement element, ISubstitution substitution) =>
      throw new System.NotImplementedException();

    public override IAccessContext GetAccessContext() => throw new System.NotImplementedException();
    public override ISymbolFilter[] GetSymbolFilters() => throw new System.NotImplementedException();
  }
}
