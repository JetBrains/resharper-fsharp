using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Parts;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class FSharpModule : FSharpClass
  {
    public FSharpModule([NotNull] IClassPart part) : base(part)
    {
    }

    protected override IList<IDeclaredType> CalcSuperTypes()
    {
      return new[] {Module.GetPredefinedType().Object};
    }
  }
}