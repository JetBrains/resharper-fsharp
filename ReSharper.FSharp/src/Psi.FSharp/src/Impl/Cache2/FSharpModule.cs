using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  public class FSharpModule : Class
  {
    public FSharpModule([NotNull] IClassPart part) : base(part)
    {
    }

    protected override bool AcceptsPart(TypePart part)
    {
      return part is ModulePart;
    }

    protected override IList<IDeclaredType> CalcSuperTypes()
    {
      return new[] {Module.GetPredefinedType().Object};
    }

    // todo: calc access modifiers in part constructor
    protected override MemberDecoration Modifiers
    {
      get
      {
        var modifiers = base.Modifiers;
        modifiers.IsAbstract = true;
        modifiers.IsStatic = true;
        modifiers.IsSealed = true;
        return modifiers;
      }
    }
  }
}