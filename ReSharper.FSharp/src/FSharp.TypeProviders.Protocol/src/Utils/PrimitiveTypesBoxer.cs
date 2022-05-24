using System;
using System.Globalization;
using JetBrains.Annotations;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;
using ServerRdStaticArg = JetBrains.Rider.FSharp.TypeProviders.Protocol.Server.RdStaticArg;
using ServerRdTypeName = JetBrains.Rider.FSharp.TypeProviders.Protocol.Server.RdTypeName;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils
{
  public static class PrimitiveTypesBoxer
  {
    [ContractAnnotation("null => null")]
    public static RdStaticArg BoxToServerStaticArg(object value)
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
        DBNull _ => new RdStaticArg(RdTypeName.dbnull, ""),
        _ when value.GetType() is var type && type.IsEnum => BoxToServerStaticArg((int)value),
        _ => throw new ArgumentException($"Unexpected static arg with type {value.GetType().FullName}")
      };
    }

    [ContractAnnotation("value:null => null")]
    public static ServerRdStaticArg BoxToClientStaticArg(object value, bool safeMode = false)
    {
      if (value == null) return null;
      return value switch
      {
        sbyte x => new ServerRdStaticArg(ServerRdTypeName.@sbyte, x.ToString()),
        short x => new ServerRdStaticArg(ServerRdTypeName.@short, x.ToString()),
        int x => new ServerRdStaticArg(ServerRdTypeName.@int, x.ToString()),
        long x => new ServerRdStaticArg(ServerRdTypeName.@long, x.ToString()),
        byte x => new ServerRdStaticArg(ServerRdTypeName.@byte, x.ToString()),
        ushort x => new ServerRdStaticArg(ServerRdTypeName.@ushort, x.ToString()),
        uint x => new ServerRdStaticArg(ServerRdTypeName.@uint, x.ToString()),
        ulong x => new ServerRdStaticArg(ServerRdTypeName.@ulong, x.ToString()),
        decimal x => new ServerRdStaticArg(ServerRdTypeName.@decimal, x.ToString(CultureInfo.InvariantCulture)),
        float x => new ServerRdStaticArg(ServerRdTypeName.@float, x.ToString(CultureInfo.InvariantCulture)),
        double x => new ServerRdStaticArg(ServerRdTypeName.@double, x.ToString(CultureInfo.InvariantCulture)),
        char x => new ServerRdStaticArg(ServerRdTypeName.@char, x.ToString()),
        bool x => new ServerRdStaticArg(ServerRdTypeName.@bool, x.ToString()),
        string x => new ServerRdStaticArg(ServerRdTypeName.@string, x),
        DBNull _ => new ServerRdStaticArg(ServerRdTypeName.@dbnull, ""),
        _ when value.GetType() is var type && type.IsEnum => BoxToClientStaticArg((int)value, safeMode),
        _ => safeMode
          ? new ServerRdStaticArg(ServerRdTypeName.unknown, value.ToString())
          : throw new ArgumentException($"Unexpected static arg with type {value.GetType().FullName}")
      };
    }

    [ContractAnnotation("arg: null => null")]
    public static object Unbox(this RdStaticArg arg, bool safeMode = false)
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
        RdTypeName.@decimal => decimal.Parse(arg.Value, CultureInfo.InvariantCulture),
        RdTypeName.@float => float.Parse(arg.Value, CultureInfo.InvariantCulture),
        RdTypeName.@double => double.Parse(arg.Value, CultureInfo.InvariantCulture),
        RdTypeName.@char => char.Parse(arg.Value),
        RdTypeName.@bool => bool.Parse(arg.Value),
        RdTypeName.@string => arg.Value,
        RdTypeName.dbnull => DBNull.Value,
        _ => safeMode ? arg.Value : throw new ArgumentException($"Unexpected static arg with type {arg.TypeName}")
      };
    }

    [ContractAnnotation("null => null")]
    public static object Unbox(this ServerRdStaticArg arg)
    {
      if (arg == null) return null;
      return arg.TypeName switch
      {
        ServerRdTypeName.@sbyte => sbyte.Parse(arg.Value),
        ServerRdTypeName.@short => short.Parse(arg.Value),
        ServerRdTypeName.@int => int.Parse(arg.Value),
        ServerRdTypeName.@long => long.Parse(arg.Value),
        ServerRdTypeName.@byte => byte.Parse(arg.Value),
        ServerRdTypeName.@ushort => ushort.Parse(arg.Value),
        ServerRdTypeName.@uint => uint.Parse(arg.Value),
        ServerRdTypeName.@ulong => ulong.Parse(arg.Value),
        ServerRdTypeName.@decimal => decimal.Parse(arg.Value, CultureInfo.InvariantCulture),
        ServerRdTypeName.@float => float.Parse(arg.Value, CultureInfo.InvariantCulture),
        ServerRdTypeName.@double => double.Parse(arg.Value, CultureInfo.InvariantCulture),
        ServerRdTypeName.@char => char.Parse(arg.Value),
        ServerRdTypeName.@bool => bool.Parse(arg.Value),
        ServerRdTypeName.@string => arg.Value,
        ServerRdTypeName.dbnull => DBNull.Value,
        _ => throw new ArgumentException($"Unexpected static arg with type {arg.TypeName}")
      };
    }

    public static object[] Unbox([NotNull] this ServerRdStaticArg[] args)
    {
      var result = new object[args.Length];
      for (var i = 0; i < args.Length; i++) result[i] = args[i].Unbox();

      return result;
    }
  }
}
