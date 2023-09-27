using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IFSharpFieldProperty : IGeneratedConstructorParameterOwner, IProperty
  {
  }

  public interface IGeneratedConstructorParameterOwner : ITypeOwner
  {
    [CanBeNull]
    IParameter GetGeneratedParameter();
  }

  public interface IGeneratedConstructorOwner
  {
    [CanBeNull]
    IParametersOwner GetConstructor();
  }
}
