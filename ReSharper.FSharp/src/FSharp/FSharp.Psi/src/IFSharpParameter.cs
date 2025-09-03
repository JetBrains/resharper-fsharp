using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi;

public interface IFSharpParameter : IFSharpDeclaredElement, IParameter
{
  FSharpParameterIndex FSharpIndex { get; }

  IEnumerable<IFSharpParameterDeclaration> GetParameterOriginDeclarations();
  IEnumerable<ILocalVariable> GetParameterOriginElements();
}
