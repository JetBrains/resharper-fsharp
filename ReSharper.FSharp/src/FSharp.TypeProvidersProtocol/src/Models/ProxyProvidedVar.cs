using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedVar : ProvidedVar
  {
    private ProxyProvidedVar(string name, bool isMutable, ProvidedType type, ProvidedTypeContextHolder context) :
      base(null, context.Context)
    {
      Name = name;
      IsMutable = isMutable;
      Type = type;
    }

    public static ProxyProvidedVar Create(string name, bool isMutable, ProvidedType type,
      ProvidedTypeContextHolder context) => new ProxyProvidedVar(name, isMutable, type, context);

    public override string Name { get; }

    public override bool IsMutable { get; }

    public override ProvidedType Type { get; }

    public override bool Equals(object obj) => obj switch
    {
      ProvidedVar y => Name == y.Name && ProvidedTypesComparer.Instance.Equals(Type, y.Type),
      _ => false
    };

    public override int GetHashCode() => Name.GetHashCode();
  }
}
