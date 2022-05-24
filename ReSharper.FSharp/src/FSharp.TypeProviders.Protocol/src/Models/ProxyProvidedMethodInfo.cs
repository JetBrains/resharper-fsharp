using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Exceptions;
using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Utils;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Client;
using JetBrains.Util.Concurrency;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models
{
  [SuppressMessage("ReSharper", "CoVariantArrayConversion")]
  public class ProxyProvidedMethodInfo : ProvidedMethodInfo, IRdProvidedEntity
  {
    private readonly RdProvidedMethodInfo myMethodInfo;
    private readonly IProxyTypeProvider myTypeProvider;
    private readonly TypeProvidersContext myTypeProvidersContext;
    public int EntityId => myMethodInfo.EntityId;
    public RdProvidedEntityType EntityType => RdProvidedEntityType.MethodInfo;
    public RdCustomAttributeData[] Attributes => myCustomAttributes.Value;

    private RdProvidedMethodInfoProcessModel RdProvidedMethodInfoProcessModel =>
      myTypeProvidersContext.Connection.ProtocolModel.RdProvidedMethodInfoProcessModel;

    private ProxyProvidedMethodInfo(RdProvidedMethodInfo methodInfo, IProxyTypeProvider typeProvider,
      TypeProvidersContext typeProvidersContext) : base(null, ProvidedConst.EmptyContext)
    {
      myMethodInfo = methodInfo;
      myTypeProvider = typeProvider;
      myTypeProvidersContext = typeProvidersContext;

      myParameters = new InterruptibleLazy<ProvidedParameterInfo[]>(
        () => myTypeProvidersContext.Connection
          .ExecuteWithCatch(() => RdProvidedMethodInfoProcessModel.GetParameters.Sync(EntityId))
          .Select(t => ProxyProvidedParameterInfo.Create(t, typeProvider, typeProvidersContext))
          .ToArray());

      myStaticParameters = new InterruptibleLazy<ProvidedParameterInfo[]>(() => myTypeProvidersContext.Connection
        .ExecuteWithCatch(() => RdProvidedMethodInfoProcessModel.GetStaticParametersForMethod.Sync(EntityId))
        .Select(t => ProxyProvidedParameterInfo.Create(t, typeProvider, typeProvidersContext))
        .ToArray());

      myGenericArguments = new InterruptibleLazy<ProvidedType[]>(() =>
        myTypeProvidersContext.ProvidedTypesCache.GetOrCreateBatch(methodInfo.GenericArguments, typeProvider));

      myTypes = new InterruptibleLazy<ProvidedType[]>(() =>
        myTypeProvidersContext.ProvidedTypesCache.GetOrCreateBatch(
          new[] { methodInfo.DeclaringType, methodInfo.ReturnType }, typeProvider));

      myCustomAttributes = new InterruptibleLazy<RdCustomAttributeData[]>(() =>
        myTypeProvidersContext.ProvidedCustomAttributeProvider.GetCustomAttributes(this));
    }

    [ContractAnnotation("methodInfo:null => null")]
    public static ProxyProvidedMethodInfo Create(RdProvidedMethodInfo methodInfo, IProxyTypeProvider typeProvider,
      TypeProvidersContext typeProvidersContext) =>
      methodInfo == null ? null : new ProxyProvidedMethodInfo(methodInfo, typeProvider, typeProvidersContext);

    public override string Name => myMethodInfo.Name;
    public override bool IsAbstract => HasFlag(RdProvidedMethodFlags.IsAbstract);
    public override bool IsConstructor => HasFlag(RdProvidedMethodFlags.IsConstructor);
    public override bool IsFamily => HasFlag(RdProvidedMethodFlags.IsFamily);
    public override bool IsFinal => HasFlag(RdProvidedMethodFlags.IsFinal);
    public override bool IsPublic => HasFlag(RdProvidedMethodFlags.IsPublic);
    public override bool IsStatic => HasFlag(RdProvidedMethodFlags.IsStatic);
    public override bool IsVirtual => HasFlag(RdProvidedMethodFlags.IsVirtual);
    public override bool IsGenericMethod => HasFlag(RdProvidedMethodFlags.IsGenericMethod);
    public override bool IsFamilyAndAssembly => HasFlag(RdProvidedMethodFlags.IsFamilyAndAssembly);
    public override bool IsFamilyOrAssembly => HasFlag(RdProvidedMethodFlags.IsFamilyOrAssembly);
    public override bool IsHideBySig => HasFlag(RdProvidedMethodFlags.IsHideBySig);
    public override int MetadataToken => myMethodInfo.MetadataToken;

    public override ProvidedType DeclaringType => myTypes.Value[0];

    public override ProvidedType ReturnType => myTypes.Value[1];

    public override ProvidedParameterInfo[] GetStaticParametersForMethod(ITypeProvider provider) =>
      myStaticParameters.Value;

    public override ProvidedMethodBase ApplyStaticArgumentsForMethod(ITypeProvider provider,
      string fullNameAfterArguments, object[] staticArgs)
    {
      myAppliedMethods ??= new Dictionary<string, ProvidedMethodBase>();
      var key = string.Join(".", fullNameAfterArguments) + "+" + string.Join(",", staticArgs);
      if (myAppliedMethods.TryGetValue(key, out var method)) return method;

      var staticArgDescriptions = staticArgs.Select(PrimitiveTypesBoxer.BoxToServerStaticArg).ToArray();
      method = Create(myTypeProvidersContext.Connection.ExecuteWithCatch(() =>
        RdProvidedMethodInfoProcessModel.ApplyStaticArgumentsForMethod.Sync(
          new ApplyStaticArgumentsForMethodArgs(EntityId, fullNameAfterArguments, staticArgDescriptions),
          RpcTimeouts.Maximal)), myTypeProvider, myTypeProvidersContext);

      myAppliedMethods.Add(key, method);

      return method;
    }

    public override ProvidedParameterInfo[] GetParameters() => myParameters.Value;
    public override ProvidedType[] GetGenericArguments() => myGenericArguments.Value;

    public override
      FSharpOption<Tuple<FSharpList<FSharpOption<object>>, FSharpList<Tuple<string, FSharpOption<object>>>>>
      GetAttributeConstructorArgs(ITypeProvider _, string attribName) =>
      myTypeProvidersContext.ProvidedCustomAttributeProvider.GetAttributeConstructorArgs(myCustomAttributes.Value,
        attribName);

    public override FSharpOption<Tuple<string, int, int>> GetDefinitionLocationAttribute(ITypeProvider _) =>
      myTypeProvidersContext.ProvidedCustomAttributeProvider.GetDefinitionLocationAttribute(myCustomAttributes.Value);

    public override string[] GetXmlDocAttributes(ITypeProvider _) =>
      myXmlDocs ??=
        myTypeProvidersContext.ProvidedCustomAttributeProvider.GetXmlDocAttributes(myCustomAttributes.Value);

    public override bool GetHasTypeProviderEditorHideMethodsAttribute(ITypeProvider _) =>
      myTypeProvidersContext.ProvidedCustomAttributeProvider.GetHasTypeProviderEditorHideMethodsAttribute(
        myCustomAttributes.Value);

    private bool HasFlag(RdProvidedMethodFlags flag) => (myMethodInfo.Flags & flag) == flag;

    private string[] myXmlDocs;
    private Dictionary<string, ProvidedMethodBase> myAppliedMethods;
    private readonly InterruptibleLazy<ProvidedType[]> myTypes;
    private readonly InterruptibleLazy<ProvidedParameterInfo[]> myParameters;
    private readonly InterruptibleLazy<ProvidedParameterInfo[]> myStaticParameters;
    private readonly InterruptibleLazy<ProvidedType[]> myGenericArguments;
    private readonly InterruptibleLazy<RdCustomAttributeData[]> myCustomAttributes;
  }
}
