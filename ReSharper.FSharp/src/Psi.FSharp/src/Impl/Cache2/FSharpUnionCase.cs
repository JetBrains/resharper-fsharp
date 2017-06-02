using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class FSharpUnionCase : FSharpTypeBase
  {

    public FSharpUnionCase([NotNull] IClassPart part) : base(part)
    {
    }

    public IEnumerable<FSharpFieldProperty> CaseFields =>
      GetMembers().OfType<FSharpFieldProperty>();
  }
}