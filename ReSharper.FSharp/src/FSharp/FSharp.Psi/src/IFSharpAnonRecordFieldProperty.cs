using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi
{
  public interface IFSharpAnonRecordFieldProperty : IFSharpDeclaredElement, ITypeOwner
  {
    IFSharpAnonRecordFieldProperty SetName(string newName);
  }
}
