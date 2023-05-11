using JetBrains.ReSharper.Plugins.FSharp.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Injections
{
  public interface IInjectionHostNode: IFSharpExpression
  {
    bool IsValidHost { get; }
  }
}
