using System;
using System.Globalization;
using JetBrains.Annotations;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using ClientRdStaticArg = JetBrains.Rider.FSharp.TypeProvidersProtocol.Client.RdStaticArg;
using ClientRdTypeName = JetBrains.Rider.FSharp.TypeProvidersProtocol.Client.RdTypeName;

//TODO: [assembly: InternalsVisibleTo("JetBrains.ReSharper.Plugins.FSharp.TypeProvidersLoader")]

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils
{
  public static class PrimitiveTypesBoxerUnboxer
  {
    [ContractAnnotation("null => null")]
    public static RdStaticArg BoxToServerStaticArg(this object value)
    {
      if (value == null) return null;
      return value switch
      {
        sbyte x => new RdStaticArg(RdTypeName.@sbyte, x.ToString()),
        short x => new RdStaticArg(RdTypeName.@short, x.ToString()),
        int x => new RdStaticArg(RdTypeName.@int, x.ToString()),
        long x => new RdStaticArg(RdTypeName.@long, x.ToString()),
        byte x => new RdStaticArg(RdTypeName.@byte, x.ToString()),
        ushort x => new RdStaticArg(RdTypeName.@ushort, x.ToString()),
        uint x => new RdStaticArg(RdTypeName.@uint, x.ToString()),
        ulong x => new RdStaticArg(RdTypeName.@ulong, x.ToString()),
        decimal x => new RdStaticArg(RdTypeName.@decimal, x.ToString(CultureInfo.InvariantCulture)),
        float x => new RdStaticArg(RdTypeName.@float, x.ToString(CultureInfo.InvariantCulture)),
        double x => new RdStaticArg(RdTypeName.@double, x.ToString(CultureInfo.InvariantCulture)),
        char x => new RdStaticArg(RdTypeName.@char, x.ToString()),
        bool x => new RdStaticArg(RdTypeName.@bool, x.ToString()),
        string x => new RdStaticArg(RdTypeName.@string, x),
        _ when value.GetType() is var type && type.IsEnum => ((int) value).BoxToServerStaticArg(),
        _ => throw new ArgumentException($"Unexpected static arg with type {value.GetType().FullName}")
      };
    }

    [ContractAnnotation("null => null")]
    public static ClientRdStaticArg BoxToClientStaticArg(this object value)
    {
      if (value == null) return null;
      return value switch
      {
        sbyte x => new ClientRdStaticArg(ClientRdTypeName.@sbyte, x.ToString()),
        short x => new ClientRdStaticArg(ClientRdTypeName.@short, x.ToString()),
        int x => new ClientRdStaticArg(ClientRdTypeName.@int, x.ToString()),
        long x => new ClientRdStaticArg(ClientRdTypeName.@long, x.ToString()),
        byte x => new ClientRdStaticArg(ClientRdTypeName.@byte, x.ToString()),
        ushort x => new ClientRdStaticArg(ClientRdTypeName.@ushort, x.ToString()),
        uint x => new ClientRdStaticArg(ClientRdTypeName.@uint, x.ToString()),
        ulong x => new ClientRdStaticArg(ClientRdTypeName.@ulong, x.ToString()),
        decimal x => new ClientRdStaticArg(ClientRdTypeName.@decimal, x.ToString(CultureInfo.InvariantCulture)),
        float x => new ClientRdStaticArg(ClientRdTypeName.@float, x.ToString(CultureInfo.InvariantCulture)),
        double x => new ClientRdStaticArg(ClientRdTypeName.@double, x.ToString(CultureInfo.InvariantCulture)),
        char x => new ClientRdStaticArg(ClientRdTypeName.@char, x.ToString()),
        bool x => new ClientRdStaticArg(ClientRdTypeName.@bool, x.ToString()),
        string x => new ClientRdStaticArg(ClientRdTypeName.@string, x),
        _ when value.GetType() is var type && type.IsEnum => ((int) value).BoxToClientStaticArg(),
        _ => throw new ArgumentException($"Unexpected static arg with type {value.GetType().FullName}")
      };
    }

    [ContractAnnotation("null => null")]
    public static object Unbox(this RdStaticArg arg)
    {
      if (arg == null) return null;
      return arg.TypeName switch
      {
        RdTypeName.@sbyte => sbyte.Parse(arg.Value),
        RdTypeName.@short => short.Parse(arg.Value),
        RdTypeName.@int => int.Parse(arg.Value),
        RdTypeName.@long => long.Parse(arg.Value),
        RdTypeName.@byte => byte.Parse(arg.Value),
        RdTypeName.@ushort => ushort.Parse(arg.Value),
        RdTypeName.@uint => uint.Parse(arg.Value),
        RdTypeName.@ulong => ulong.Parse(arg.Value),
        RdTypeName.@decimal => decimal.Parse(arg.Value),
        RdTypeName.@float => float.Parse(arg.Value),
        RdTypeName.@double => double.Parse(arg.Value),
        RdTypeName.@char => char.Parse(arg.Value),
        RdTypeName.@bool => bool.Parse(arg.Value),
        RdTypeName.@string => arg.Value,
        _ => throw new ArgumentException($"Unexpected static arg with type {arg.TypeName}")
      };
    }

    [ContractAnnotation("null => null")]
    public static object Unbox(this ClientRdStaticArg arg)
    {
      if (arg == null) return null;
      return arg.TypeName switch
      {
        ClientRdTypeName.@sbyte => sbyte.Parse(arg.Value),
        ClientRdTypeName.@short => short.Parse(arg.Value),
        ClientRdTypeName.@int => int.Parse(arg.Value),
        ClientRdTypeName.@long => long.Parse(arg.Value),
        ClientRdTypeName.@byte => byte.Parse(arg.Value),
        ClientRdTypeName.@ushort => ushort.Parse(arg.Value),
        ClientRdTypeName.@uint => uint.Parse(arg.Value),
        ClientRdTypeName.@ulong => ulong.Parse(arg.Value),
        ClientRdTypeName.@decimal => decimal.Parse(arg.Value),
        ClientRdTypeName.@float => float.Parse(arg.Value),
        ClientRdTypeName.@double => double.Parse(arg.Value),
        ClientRdTypeName.@char => char.Parse(arg.Value),
        ClientRdTypeName.@bool => bool.Parse(arg.Value),
        ClientRdTypeName.@string => arg.Value,
        _ => throw new ArgumentException($"Unexpected static arg with type {arg.TypeName}")
      };
    }
  }
}
