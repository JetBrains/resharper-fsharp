using System.Collections.Generic;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IRecordRepresentation
  {
    IList<ITypeOwner> GetFields();
  }
}
