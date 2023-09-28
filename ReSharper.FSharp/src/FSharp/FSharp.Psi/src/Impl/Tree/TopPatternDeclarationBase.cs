﻿using System;
using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.Symbols;
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

      if (TryCreateDeclaredElementFast(typeDeclaration) is { } declaredElement)
        return declaredElement;

      return GetFcsSymbol() is { } fcsSymbol
        ? CreateDeclaredElement(fcsSymbol)
        : null;
    }

    protected override IDeclaredElement CreateDeclaredElement(FSharpSymbol fcsSymbol) =>
      CreateBindingDeclaredElement(fcsSymbol, this);

    public static IDeclaredElement CreateBindingDeclaredElement([NotNull] FSharpSymbol fcsSymbol,
      [NotNull] TopPatternDeclarationBase declaration)
    {
      if (!(fcsSymbol is FSharpMemberOrFunctionOrValue mfv))
        return null;

      var typeDeclaration = declaration.GetContainingNode<ITypeDeclaration>();
      if (typeDeclaration == null)
        return null;

      if (typeDeclaration is IFSharpTypeDeclaration)
      {
        if ((!mfv.CurriedParameterGroups.IsEmpty() || !mfv.GenericParameters.IsEmpty()) && !mfv.IsMutable)
          return new FSharpTypePrivateMethod(declaration);

        if (mfv.LiteralValue != null)
          return new FSharpLiteral(declaration);

        return new FSharpTypePrivateField(declaration);
      }

      if (mfv.LiteralValue != null)
        return new FSharpLiteral(declaration);

      if (!mfv.IsValCompiledAsMethod())
        return new ModuleValue(declaration);

      return !mfv.IsInstanceMember && mfv.CompiledName.StartsWith("op_", StringComparison.Ordinal)
        ? new FSharpSignOperator<TopPatternDeclarationBase>(declaration)
        : new ModuleFunction(declaration);
    }

    [CanBeNull]
    private IDeclaredElement TryCreateDeclaredElementFast(ITypeDeclaration typeDeclaration)
    {
      var binding = TopBindingNavigator.GetByHeadPattern((IFSharpPattern) this);
      if (binding == null)
        return null;

      if (binding.IsMutable)
        return CreateValue(typeDeclaration);

      if (binding.HasParameters)
      {
        if (this.TryCreateOperator() is { } opDeclaredElement)
          return opDeclaredElement;

        return typeDeclaration is IFSharpTypeDeclaration
          ? new FSharpTypePrivateMethod(this)
          : new ModuleFunction(this);
      }

      var chameleonExpr = binding.ChameleonExpression;

      // No expression in signatures or parse error.
      if (chameleonExpr == null)
        return null;

      if (TryCreateLiteral(binding, chameleonExpr) is { } literal)
        return literal;

      return chameleonExpr.IsSimpleValueExpression()
        ? CreateValue(typeDeclaration)
        : null;
    }

    [CanBeNull]
    private IDeclaredElement TryCreateLiteral([NotNull] IBinding binding, [NotNull] IChameleonExpression chameleonExpr)
    {
      if (!binding.Attributes.HasAttribute("Literal"))
        return null;

      return chameleonExpr.IsLiteralExpression()
        ? new FSharpLiteral(this)
        : null;
    }

    [NotNull]
    private IDeclaredElement CreateValue(ITypeDeclaration typeDeclaration) =>
      typeDeclaration is IFSharpTypeDeclaration
        ? new FSharpTypePrivateField(this)
        : new ModuleValue(this);

    public virtual IType GetPatternType() => TypeFactory.CreateUnknownType(GetPsiModule());

    [CanBeNull] public abstract IBindingLikeDeclaration Binding { get; }

    public bool CanBeMutable => Binding != null;

    public override bool IsStatic =>
      (Binding as IBinding)?.StaticKeyword != null;

    public virtual IEnumerable<IFSharpPattern> NestedPatterns =>
      EmptyList<IFSharpPattern>.Instance;

    public virtual IEnumerable<IFSharpDeclaration> Declarations =>
      NestedPatterns.OfType<IFSharpDeclaration>();

    public bool IsLocal => false;

    public virtual ConstantValue ConstantValue => ConstantValue.NOT_COMPILE_TIME_CONSTANT;
  }
}
