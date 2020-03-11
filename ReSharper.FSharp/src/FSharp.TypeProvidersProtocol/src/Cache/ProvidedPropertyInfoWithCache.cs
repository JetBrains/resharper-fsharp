using System.Reflection;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache
{
  public class ProvidedPropertyInfoWithCache: ProvidedPropertyInfo
  {
    public ProvidedPropertyInfoWithCache(PropertyInfo x, ProvidedTypeContext ctxt) : base(x, ctxt)
    {
    }
  }
}
