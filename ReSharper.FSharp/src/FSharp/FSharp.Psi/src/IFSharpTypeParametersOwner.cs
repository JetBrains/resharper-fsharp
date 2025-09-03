using System.Collections.Generic;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi;

public interface IFSharpTypeParametersOwner : IFSharpDeclaredElement
{
  IList<ITypeParameter> AllTypeParameters { get; }
}
