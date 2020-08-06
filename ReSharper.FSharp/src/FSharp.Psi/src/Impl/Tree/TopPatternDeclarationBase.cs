using System;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal abstract class TopPatternDeclarationBase : FSharpProperTypeMemberDeclarationBase, IFunctionDeclaration
  {
    IFunction IFunctionDeclaration.DeclaredElement => base.DeclaredElement as IFunction;

    protected override IDeclaredElement CreateDeclaredElement()
    {
      var typeDeclaration = GetContainingNode<ITypeDeclaration>();
      if (typeDeclaration == null)
        return null;

      if (TryCreateDeclaredElementFast(typeDeclaration) is var declaredElement && declaredElement != null)
        return declaredElement;
      
      return GetFSharpSymbol() is { } fcsSymbol
        ? CreateDeclaredElement(fcsSymbol)
        : null;
    }

    protected override IDeclaredElement CreateDeclaredElement(FSharpSymbol fcsSymbol)
    {
      if (!(fcsSymbol is FSharpMemberOrFunctionOrValue mfv))
        return null;

      var typeDeclaration = GetContainingNode<ITypeDeclaration>();
      if (typeDeclaration == null)
        return null;

      if (typeDeclaration is IFSharpTypeDeclaration)
      {
        if ((!mfv.CurriedParameterGroups.IsEmpty() || !mfv.GenericParameters.IsEmpty()) && !mfv.IsMutable)
          return new FSharpTypePrivateMethod(this);

        if (mfv.LiteralValue != null)
          return new FSharpLiteral(this);

        return new FSharpTypePrivateField(this);
      }

      if (mfv.LiteralValue != null)
        return new FSharpLiteral(this);

      if (!mfv.IsValCompiledAsMethod())
        return new ModuleValue(this);

      return !mfv.IsInstanceMember && mfv.CompiledName.StartsWith("op_", StringComparison.Ordinal)
        ? (IDeclaredElement) new FSharpSignOperator<TopPatternDeclarationBase>(this)
        : new ModuleFunction(this);
    }

    [CanBeNull]
    private IDeclaredElement TryCreateDeclaredElementFast(ITypeDeclaration typeDeclaration)
    {
      var binding = TopBindingNavigator.GetByHeadPattern((IFSharpPattern) this);
      if (binding == null)
        return null;

      if (binding.IsMutable)
        return CreateValue(typeDeclaration);

      if (this is IParametersOwnerPat)
      {
        if (this.TryCreateOperator() is var opDeclaredElement && opDeclaredElement != null)
          return opDeclaredElement;

        return typeDeclaration is IFSharpTypeDeclaration
          ? (IDeclaredElement) new FSharpTypePrivateMethod(this)
          : new ModuleFunction(this);
      }

      var chameleonExpr = binding.ChameleonExpression;

      // No expression in signatures or parse error.
      if (chameleonExpr == null)
        return null;

      if (TryCreateLiteral(binding, chameleonExpr) is var literal && literal != null)
        return literal;

      if (chameleonExpr.IsSimpleValueExpression())
        return CreateValue(typeDeclaration);

      return null;
    }

    [CanBeNull]
    private IDeclaredElement TryCreateLiteral([NotNull] IBinding binding, [NotNull] IChameleonExpression chameleonExpr)
    {
      if (!binding.AllAttributes.HasAttribute("Literal"))
        return null;

      return chameleonExpr.IsLiteralExpression()
        ? new FSharpLiteral(this)
        : null;
    }

    [NotNull] private IDeclaredElement CreateValue(ITypeDeclaration typeDeclaration) =>
      typeDeclaration is IFSharpTypeDeclaration
        ? (IDeclaredElement) new FSharpTypePrivateField(this)
        : new ModuleValue(this);

    public virtual IType GetPatternType() => TypeFactory.CreateUnknownType(GetPsiModule());
  }
}
