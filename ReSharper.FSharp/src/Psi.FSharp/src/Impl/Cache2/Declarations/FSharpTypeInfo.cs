using System.Collections.Generic;
using JetBrains.Annotations;

namespace JetBrains.ReSharper.Psi.FSharp.Impl.Cache2.Declarations
{
  internal class FSharpTypeInfo
  {
    public FSharpTypeInfo([CanBeNull] string clrName, FSharpPartKind typeKind)
    {
      ClrName = clrName;
      TypeKind = typeKind;
    }

    public readonly string ClrName;
    public readonly FSharpPartKind TypeKind;
    public readonly HashSet<string> MemberNames = new HashSet<string>();
  }
}