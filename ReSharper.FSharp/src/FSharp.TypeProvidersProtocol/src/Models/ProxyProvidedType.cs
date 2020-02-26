using FSharp.Compiler;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using ProvidedType = FSharp.Compiler.ExtensionTyping.ProvidedType;

namespace JetBrains.ReSharper.Plugins.FSharp.Models
{
  public class ProxyProvidedType : ProvidedType
  {
    private readonly RdProvidedType rdProvidedType;

    internal ProxyProvidedType(RdProvidedType rdProvidedType): base(typeof(string), ExtensionTyping.ProvidedTypeContext.Empty)
    {
      this.rdProvidedType = rdProvidedType;
    }

    public static ProxyProvidedType Create(RdProvidedType type)
    {
      return new ProxyProvidedType(type);
    }

    public override string Name => rdProvidedType.Name;
    public override string FullName => rdProvidedType.FullName;
    public override string Namespace => rdProvidedType.Namespace;
    public override bool IsGenericParameter => rdProvidedType.IsGenericParameter;
    public override bool IsValueType => rdProvidedType.IsValueType;
    public override bool IsByRef => rdProvidedType.IsByRef;
    public override bool IsPointer => rdProvidedType.IsPointer;
    public override bool IsPublic => rdProvidedType.IsPublic;
    public override bool IsNestedPublic => rdProvidedType.IsNestedPublic;
    public override bool IsEnum => rdProvidedType.IsEnum;
    public override bool IsClass => rdProvidedType.IsClass;
    public override bool IsSealed => rdProvidedType.IsSealed;
    public override bool IsAbstract => rdProvidedType.IsAbstract;
    public override bool IsInterface => rdProvidedType.IsInterface;
  }
}
