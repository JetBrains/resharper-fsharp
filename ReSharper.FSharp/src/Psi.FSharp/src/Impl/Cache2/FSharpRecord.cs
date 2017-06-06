using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement;
using JetBrains.ReSharper.Psi.FSharp.Impl.DeclaredElement.CompilerGenerated;
using JetBrains.Util;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2
{
  internal class FSharpRecord : FSharpSimpleTypeBase
  {
    public FSharpRecord([NotNull] IClassPart part) : base(part)
    {
    }

    public override IEnumerable<ITypeMember> GetMembers()
    {
      var ctor = new FSharpGeneratedConstructor(this, base.GetMembers().OfType<FSharpFieldProperty>().AsArray());
      return base.GetMembers().Prepend(ctor);
    }
  }
}