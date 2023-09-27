using System;
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
      if (mfv is not {IsDispatchSlot: true})
        return false;

      var mfvEntity = mfv.DeclaringEntity;
      if (mfvEntity == null)
        return false;

      var logicalName = mfv.LogicalName;
      var mfvType = mfv.FullType.GenericArguments[1];

      return mfvEntity.Value.MembersFunctionsAndValues.Any(m =>
        m.IsOverrideOrExplicitInterfaceImplementation &&
        logicalName == m.LogicalName &&
        (m.XmlDocSig == mfv.XmlDocSig && m.ImplementedAbstractSignatures.Count == 0 ||
         mfvType.Equals(m.FullType) &&
         m.ImplementedAbstractSignatures.Count == 1 &&
         m.ImplementedAbstractSignatures[0].DeclaringType.Equals(mfvEntity.Value.AsType())));
    }

    protected override string DeclaredElementName => NameIdentifier.GetCompiledName(Attributes);
    public override IFSharpIdentifier NameIdentifier => (IFSharpIdentifier) Identifier;

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
  }
}
