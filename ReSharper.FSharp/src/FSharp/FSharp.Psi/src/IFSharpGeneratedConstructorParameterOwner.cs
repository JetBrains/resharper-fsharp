using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi;

public interface IFSharpGeneratedConstructorParameterOwner : ITypeOwner
{
  [CanBeNull]
  IParameter GetGeneratedParameter();
}
