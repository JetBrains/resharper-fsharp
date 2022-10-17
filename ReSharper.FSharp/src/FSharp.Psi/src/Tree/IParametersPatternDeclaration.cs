using System.Collections.Generic;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IParametersPatternDeclaration : /*IFSharpParameterDeclarationGroup,*/ IFSharpDeclaration
  {
    bool IgnoresIntermediateParens { get; }
    IFSharpParameterDeclarationGroup ParameterDeclarationGroup { get; }
    IList<IFSharpParameterDeclaration> ParameterDeclarations { get; }
  }
}
