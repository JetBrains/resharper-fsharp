using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class FSharpUnionCase : FSharpClassLikeElement<UnionCasePart>
  {
    public bool IsSingletonCase { get; }

    public FSharpUnionCase([NotNull] IClassPart part, bool isSingletonCase) : base(part)
    {
      IsSingletonCase = isSingletonCase;
    }

    public IEnumerable<FSharpFieldProperty> CaseFields =>
      GetMembers().OfType<FSharpFieldProperty>();
  }
}