using System;
using System.Linq;
using FSharp.Compiler.SourceCodeServices;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.DeclaredElement;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Tree
{
  internal partial class AbstractMemberDeclaration
  {
    protected override string DeclaredElementName => NameIdentifier.GetCompiledName(Attributes);
    public override IFSharpIdentifierLikeNode NameIdentifier => (IFSharpIdentifierLikeNode) Identifier;

    protected override IDeclaredElement CreateDeclaredElement() =>
      GetFSharpSymbol() is { } fcsSymbol
        ? CreateDeclaredElement(fcsSymbol)
        : null;

    protected override IDeclaredElement CreateDeclaredElement(FSharpSymbol fcsSymbol)
    {
      if (!(fcsSymbol is FSharpMemberOrFunctionOrValue mfv))
        return null;

      // todo: remove this and provide API in FCS and cache it somehow
      var logicalName = mfv.LogicalName;

      var hasDefault =
        mfv.DeclaringEntity?.Value.MembersFunctionsAndValues.Any(m =>
          m.IsOverrideOrExplicitInterfaceImplementation && logicalName == m.LogicalName) ?? false;

      if (hasDefault)
        return null;

      // workaround for RIDER-26985, FCS provides wrong info for abstract events.
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
  }
}
