using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
{
  public class ProvidedTypeContextHolder
  {
    public ProvidedTypeContext Context { get; set; }

    public static ProvidedTypeContextHolder Create() =>
      new ProvidedTypeContextHolder {Context = ProvidedTypeContext.Empty};
  }
}
