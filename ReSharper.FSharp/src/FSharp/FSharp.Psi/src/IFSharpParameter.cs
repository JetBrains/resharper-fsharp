using System.Collections.Generic;
using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi;

// todo: FcsType, FCS param
public interface IFSharpParameter : IFSharpDeclaredElement, IParameter
{
  FSharpParameterIndex FSharpIndex { get; }
}

public interface IFSharpGeneratedParameterFromPattern : IFSharpParameter
{
  IEnumerable<IFSharpParameterDeclaration> GetParameterOriginDeclarations();
  IEnumerable<ILocalVariable> GetParameterOriginElements();
}
