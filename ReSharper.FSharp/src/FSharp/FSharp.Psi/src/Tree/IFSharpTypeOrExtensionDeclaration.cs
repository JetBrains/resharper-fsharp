﻿namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IFSharpTypeOrExtensionDeclaration
  {
    bool IsPrimary { get; }
    ITypeParameterDeclarationList TypeParameterDeclarationList { get; }
  }
}
