using System.Collections.Generic;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IOrPat
  {
    IList<IFSharpPattern> Patterns { get; }
  }
}
