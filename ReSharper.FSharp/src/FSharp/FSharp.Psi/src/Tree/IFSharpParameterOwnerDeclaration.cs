using System.Collections.Generic;
using FSharp.Compiler.Symbols;
using JetBrains.Annotations;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

public interface IFSharpParameterOwnerDeclaration : IFSharpDeclaration
{
  [CanBeNull]
  IFSharpParameterDeclaration GetParameterDeclaration(FSharpParameterIndex index);

  IList<IList<IFSharpParameterDeclaration>> GetParameterDeclarations();
  
  void SetParameterFcsType(FSharpParameterIndex index, FSharpType fcsType);
}
