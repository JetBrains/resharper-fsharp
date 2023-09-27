using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils;
using static FSharp.Compiler.TypeProviders;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
{
  public class ProxyProvidedVar : ProvidedVar
  {
    private ProxyProvidedVar(string name, bool isMutable, ProvidedType type) :
      base(null, type.Context)
    {
      Name = name;
      IsMutable = isMutable;
      Type = type;
    }

    public static ProxyProvidedVar Create(string name, bool isMutable, ProvidedType type) => new(name, isMutable, type);

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
