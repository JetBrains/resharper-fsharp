using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Server;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using ClientRdFakeTcImports = JetBrains.Rider.FSharp.TypeProviders.Protocol.Client.RdFakeTcImports;
using ClientRdFakeDllInfo = JetBrains.Rider.FSharp.TypeProviders.Protocol.Client.RdFakeDllInfo;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol
{
  internal static class ReflectionUtils
  {
    private const BindingFlags BIND_ALL = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic |
                                          BindingFlags.Static | BindingFlags.Instance;

    public static object GetProperty(this object obj, string nm)
    {
      var ty = obj.GetType();
      var prop = ty.GetProperty(nm, BIND_ALL);
      var v = prop.GetValue(obj, null);
      return v;
    }

    public static object GetField(this object obj, string nm)
    {
      var ty = obj.GetType();
      var fld = ty.GetField(nm, BIND_ALL);
      var v = fld.GetValue(obj);
      return v;
    }
  }

  public static class TcImportsHack
  {
    record FakeTcImportsBaseValue(RdFakeTcImports baseFakeTcImports)
    {
      public FakeTcImports Value => new(baseFakeTcImports.DllInfos, baseFakeTcImports.Base);
    }

    record FakeTcImports(RdFakeDllInfo[] DllInfos, RdFakeTcImports baseFakeTcImports)
    {
      public RdFakeDllInfo[] dllInfos = DllInfos;
      public FakeTcImportsBaseValue Base => new(baseFakeTcImports);
      public bool SystemRuntimeContainsType(string _) => true; // todo: smart implementation
    }

    record FakeSystemRuntimeContainsTypeRef(RdFakeTcImports fakeTcImports)
    {
      public FSharpFunc<string, bool> Value =>
        new FakeTcImportsClosure(new FakeTcImports(fakeTcImports.DllInfos, fakeTcImports.Base));
    }

    private sealed class FakeTcImportsClosure : FSharpFunc<string, bool>
    {
      private readonly FakeTcImports tcImports;

      public FakeTcImportsClosure(FakeTcImports tcImports) => this.tcImports = tcImports;

      public override bool Invoke(string x) => tcImports.SystemRuntimeContainsType(x);
    }

    private sealed class SystemRuntimeContainsTypeRefClosure : FSharpFunc<string, bool>
    {
      private readonly FakeSystemRuntimeContainsTypeRef systemRuntimeContainsTypeRef;

      public SystemRuntimeContainsTypeRefClosure(FakeSystemRuntimeContainsTypeRef systemRuntimeContainsTypeRef) =>
        this.systemRuntimeContainsTypeRef = systemRuntimeContainsTypeRef;

      public override bool Invoke(string x) => systemRuntimeContainsTypeRef.Value.Invoke(x);
    }


    // The type provider must not contain strong references to remote TcImport objects.
    // The legacy Type Provider SDK gets dllInfos data from the 'systemRuntimeContainsType' closure.
    // This hack allows you to pull this data for transfer between processes.
    public static ClientRdFakeTcImports GetFakeTcImports(FSharpFunc<string, bool> runtimeContainsType)
    {
      ClientRdFakeDllInfo[] getDllInfos(object imports) => (imports.GetField("dllInfos") as IEnumerable<object>)
        .Select(dllInfo => new ClientRdFakeDllInfo(dllInfo.GetProperty("FileName") as string))
        .ToArray();

      var tcImports =
        runtimeContainsType.GetField("systemRuntimeContainsTypeRef").GetProperty("Value").GetField("tcImports");

      var tcImportsDllInfos = getDllInfos(tcImports);
      var baseTcImports = tcImports.GetProperty("Base").GetProperty("Value");
      var baseTcImportsDllInfos = getDllInfos(baseTcImports);
      var fakeBaseTcImports = new ClientRdFakeTcImports(null, baseTcImportsDllInfos);

      return new ClientRdFakeTcImports(fakeBaseTcImports, tcImportsDllInfos);
    }

    public static FSharpFunc<string, bool> InjectFakeTcImports(RdFakeTcImports fakeTcImports) =>
      new SystemRuntimeContainsTypeRefClosure(new FakeSystemRuntimeContainsTypeRef(fakeTcImports));
  }
}
