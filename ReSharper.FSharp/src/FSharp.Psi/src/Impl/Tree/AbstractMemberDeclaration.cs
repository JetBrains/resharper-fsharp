using System;
using System.Collections.Generic;
using System.Linq;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class AbstractMemberDeclaration
  {
    private bool? myHasDefaultImplementation;

    protected override void ClearCachedData()
    {
      base.ClearCachedData();
      myHasDefaultImplementation = null;
    }

    public bool HasDefaultImplementation
    {
      get
      {
        if (myHasDefaultImplementation != null)
          return myHasDefaultImplementation.Value;

        lock (this)
          return myHasDefaultImplementation ??=
            CalcHasDefaultImplementation(GetFcsSymbol() as FSharpMemberOrFunctionOrValue);
      }
    }

    private static bool CalcHasDefaultImplementation([CanBeNull] FSharpMemberOrFunctionOrValue mfv)
    {
      if (mfv is not { IsDispatchSlot: true })
        return false;

      var mfvEntity = mfv.DeclaringEntity;
      if (mfvEntity == null)
        return false;

      var logicalName = mfv.LogicalName;
      var mfvType = mfv.FullType.GenericArguments[1];

      return mfvEntity.Value.MembersFunctionsAndValues.Any(m =>
        m.IsOverrideOrExplicitInterfaceImplementation &&
        logicalName == m.LogicalName &&
        (m.XmlDocSig == mfv.XmlDocSig ||
         mfvType.Equals(m.FullType) &&
         mfv.ImplementedAbstractSignatures.Count == 1 &&
         mfv.ImplementedAbstractSignatures[0].DeclaringType.Equals(mfvEntity.Value.AsType())));
    }

    protected override string DeclaredElementName => NameIdentifier.GetCompiledName(Attributes);
    public override IFSharpIdentifierLikeNode NameIdentifier => (IFSharpIdentifierLikeNode)Identifier;

    protected override IDeclaredElement CreateDeclaredElement() =>
      !(Parent is IMemberConstraint) && GetFcsSymbol() is { } fcsSymbol
        ? CreateDeclaredElement(fcsSymbol)
        : null;

    protected override IDeclaredElement CreateDeclaredElement(FSharpSymbol fcsSymbol)
    {
      if (!(fcsSymbol is FSharpMemberOrFunctionOrValue mfv))
        return null;

      // workaround for RIDER-26985, FCS provides wrong info for abstract events.
      var logicalName = mfv.LogicalName;
      if (logicalName.StartsWith("add_", StringComparison.Ordinal) ||
          logicalName.StartsWith("remove_", StringComparison.Ordinal))
      {
        if (mfv.Attributes.HasAttributeInstance(FSharpPredefinedType.CLIEventAttribute))
          return new AbstractFSharpCliEvent(this);
      }

      if (mfv.IsProperty)
        return new FSharpProperty<AbstractMemberDeclaration>(this, mfv);

      var property = mfv.AccessorProperty;
      if (property != null)
        return new FSharpProperty<AbstractMemberDeclaration>(this, property.Value);

      return new FSharpMethod<AbstractMemberDeclaration>(this);
    }

    public override bool IsStatic => StaticKeyword != null;


    // public IParameterDeclaration AddParameterDeclarationBefore(ParameterKind kind, IType parameterType,
    //   string parameterName, IParameterDeclaration anchor) =>
    //   throw new NotImplementedException();
    //
    // public IParameterDeclaration AddParameterDeclarationAfter(ParameterKind kind, IType parameterType,
    //   string parameterName, IParameterDeclaration anchor) =>
    //   throw new NotImplementedException();
    //
    // public void RemoveParameterDeclaration(int index) =>
    //   throw new NotImplementedException();

    public IParametersOwner DeclaredParametersOwner => (IParametersOwner)DeclaredElement;

    public IList<IFSharpParameterDeclarationGroup> ParameterGroups => this.GetParameterGroups();

    public IFSharpParameterDeclaration GetParameter((int group, int index) position) =>
      FSharpImplUtil.GetParameter(this, position);

    public IList<IFSharpParameterDeclaration> ParameterDeclarations => this.GetParameterDeclarations();
  }
}
