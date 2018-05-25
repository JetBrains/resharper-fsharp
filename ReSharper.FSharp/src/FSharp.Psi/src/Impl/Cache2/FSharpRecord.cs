using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Parts;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2
{
  internal class FSharpRecord : FSharpSimpleTypeBase
  {
    public FSharpRecord([NotNull] IClassPart part) : base(part)
    {
    }

    public override IEnumerable<ITypeMember> GetMembers() =>
      IsCliMutable ? base.GetMembers().Prepend(DefaultConstructor) : base.GetMembers();

    public bool IsCliMutable
    {
      get
      {
        foreach (var part in EnumerateParts())
          if (part is RecordPart recordPart && recordPart.HasCliMutable)
            return true;
        return false;
      }
    }
  }
}