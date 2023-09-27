using System.Collections.Generic;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public interface IFSharpArgumentsOwner : IArgumentsOwner
  {
    /// List of arguments aligned with their matching parameter.
    /// e.g. index #2 is the argument that matches with param #2 on the invoked reference.
    /// A null element at a given index means there is no argument matching that parameter.
    IList<IArgument> ParameterArguments { get; }
  }
}
