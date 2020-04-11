using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using ClientRdStaticArg = JetBrains.Rider.FSharp.TypeProvidersProtocol.Client.RdStaticArg;

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
        sbyte x => new RdStaticArg("sbyte", x.ToString()),
        short x => new RdStaticArg("short", x.ToString()),
        int x => new RdStaticArg("int", x.ToString()),
        long x => new RdStaticArg("long", x.ToString()),
        byte x => new RdStaticArg("byte", x.ToString()),
        ushort x => new RdStaticArg("ushort", x.ToString()),
        uint x => new RdStaticArg("uint", x.ToString()),
        ulong x => new RdStaticArg("ulong", x.ToString()),
        decimal x => new RdStaticArg("decimal", x.ToString(CultureInfo.InvariantCulture)),
        float x => new RdStaticArg("float", x.ToString(CultureInfo.InvariantCulture)),
        double x => new RdStaticArg("double", x.ToString(CultureInfo.InvariantCulture)),
        char x => new RdStaticArg("char", x.ToString()),
        bool x => new RdStaticArg("bool", x.ToString()),
        string x => new RdStaticArg("string", x),
        _ => throw new ArgumentException($"Unexpected static arg with type {value.GetType().FullName}")
      };
    }

    [ContractAnnotation("null => null")]
    public static ClientRdStaticArg BoxToClientStaticArg(this object value)
    {
      if (value == null) return null;
      return value switch
      {
        sbyte x => new ClientRdStaticArg("sbyte", x.ToString()),
        short x => new ClientRdStaticArg("short", x.ToString()),
        int x => new ClientRdStaticArg("int", x.ToString()),
        long x => new ClientRdStaticArg("long", x.ToString()),
        byte x => new ClientRdStaticArg("byte", x.ToString()),
        ushort x => new ClientRdStaticArg("ushort", x.ToString()),
        uint x => new ClientRdStaticArg("uint", x.ToString()),
        ulong x => new ClientRdStaticArg("ulong", x.ToString()),
        decimal x => new ClientRdStaticArg("decimal", x.ToString(CultureInfo.InvariantCulture)),
        float x => new ClientRdStaticArg("float", x.ToString(CultureInfo.InvariantCulture)),
        double x => new ClientRdStaticArg("double", x.ToString(CultureInfo.InvariantCulture)),
        char x => new ClientRdStaticArg("char", x.ToString()),
        bool x => new ClientRdStaticArg("bool", x.ToString()),
        string x => new ClientRdStaticArg("string", x),
        _ => throw new ArgumentException($"Unexpected static arg with type {value.GetType().FullName}")
      };
    }

    [ContractAnnotation("null => null")]
    public static object Unbox(this RdStaticArg arg)
    {
      if (arg == null) return null;
      return arg.TypeName switch
      {
        "sbyte" => sbyte.Parse(arg.Value),
        "short" => short.Parse(arg.Value),
        "int" => int.Parse(arg.Value),
        "long" => long.Parse(arg.Value),
        "byte" => byte.Parse(arg.Value),
        "ushort" => ushort.Parse(arg.Value),
        "uint" => uint.Parse(arg.Value),
        "ulong" => ulong.Parse(arg.Value),
        "decimal" => decimal.Parse(arg.Value),
        "float" => float.Parse(arg.Value),
        "double" => double.Parse(arg.Value),
        "char" => char.Parse(arg.Value),
        "bool" => bool.Parse(arg.Value),
        "string" => (object) arg.Value,
        _ => throw new ArgumentException($"Unexpected static arg with type {arg.TypeName}")
      };
    }

    [ContractAnnotation("null => null")]
    public static object Unbox(this ClientRdStaticArg arg)
    {
      if (arg == null) return null;
      return arg.TypeName switch
      {
        "sbyte" => sbyte.Parse(arg.Value),
        "short" => short.Parse(arg.Value),
        "int" => int.Parse(arg.Value),
        "long" => long.Parse(arg.Value),
        "byte" => byte.Parse(arg.Value),
        "ushort" => ushort.Parse(arg.Value),
        "uint" => uint.Parse(arg.Value),
        "ulong" => ulong.Parse(arg.Value),
        "decimal" => decimal.Parse(arg.Value),
        "float" => float.Parse(arg.Value),
        "double" => double.Parse(arg.Value),
        "char" => char.Parse(arg.Value),
        "bool" => bool.Parse(arg.Value),
        "string" => (object) arg.Value,
        _ => throw new ArgumentException($"Unexpected static arg with type {arg.TypeName}")
      };
    }
  }
}
