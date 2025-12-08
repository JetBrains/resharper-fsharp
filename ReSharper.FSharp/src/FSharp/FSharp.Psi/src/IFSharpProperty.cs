using System.Collections.Generic;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IFSharpProperty : IProperty, IFSharpParameterOwner, IFSharpRepresentationAccessRightsOwner
  {
    public bool IsIndexerLike { get; }
    public IEnumerable<IMethod> Accessors { get; }
  }
}
