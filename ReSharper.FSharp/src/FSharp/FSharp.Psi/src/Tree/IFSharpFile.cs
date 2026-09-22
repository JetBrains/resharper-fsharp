using System.Collections.Generic;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Tree
{
  public partial interface IFSharpFile : IFSharpFileCheckInfoOwner, IFileImpl
  {
    IReadOnlyList<TreeOffset> ObjectExpressionOffsets { get; }  }
}