﻿using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using Microsoft.FSharp.Quotations;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedVarWithCache : ProvidedVar, IProxyProvidedNamedEntity
  {
    private readonly RdProvidedVar myVar;
    private readonly int myTypeProviderId;
    private readonly ProvidedTypeContext myContext;
    private readonly RdFSharpTypeProvidersLoaderModel myProcessModel;
    private readonly IProvidedTypesCache myCache;

    public int EntityId => myVar.EntityId;
    private RdProvidedVarProcessModel RdProvidedVarProcessModel => myProcessModel.RdProvidedVarProcessModel;

    private ProxyProvidedVarWithCache(RdProvidedVar var, int typeProviderId, ProvidedTypeContext context,
      RdFSharpTypeProvidersLoaderModel processModel, IProvidedTypesCache cache) : base(
      FSharpVar.Global("__fake__", typeof(string)), context)
    {
      myVar = var;
      myTypeProviderId = typeProviderId;
      myContext = context;
      myProcessModel = processModel;
      myCache = cache;
    }

    [ContractAnnotation("var:null => null")]
    public static ProxyProvidedVarWithCache Create(RdProvidedVar var,
      int typeProviderId,
      RdFSharpTypeProvidersLoaderModel processModel,
      ProvidedTypeContext context, IProvidedTypesCache cache) =>
      var == null ? null : new ProxyProvidedVarWithCache(var, typeProviderId, context, processModel, cache);

    public override string Name => myVar.Name;
    public override bool IsMutable => myVar.IsMutable;

    public override ProvidedType Type =>
      myCache.GetOrCreateWithContext(myTypeId ??= RdProvidedVarProcessModel.Type.Sync(EntityId), myTypeProviderId,
        myContext);

    public string FullName => Type.FullName + Name;

    //TODO: reduce allocations count
    public override bool Equals(object obj) => obj switch
    {
      ProvidedVar y => FullName == y.Type.FullName + y.Name,
      _ => false
    };

    public override int GetHashCode() => FullName.GetHashCode();

    private int? myTypeId;
  }
}
