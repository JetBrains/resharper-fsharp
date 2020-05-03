using System.Collections.Generic;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public interface IFSharpArgumentsOwner : IArgumentsOwner, IFSharpReferenceOwner
  {
    IList<IFSharpExpression> AppliedExpressions { get; }
  }
}
