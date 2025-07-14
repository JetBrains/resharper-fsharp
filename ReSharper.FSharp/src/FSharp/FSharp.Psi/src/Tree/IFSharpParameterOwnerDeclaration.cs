using System.Collections.Generic;
using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

public interface IFSharpParameterOwnerDeclaration : IFSharpDeclaration
{
  [CanBeNull]
  IFSharpParameterDeclaration GetParameterDeclaration(FSharpParameterIndex index);

  IList<IList<IFSharpParameterDeclaration>> GetParameterDeclarations();
}
