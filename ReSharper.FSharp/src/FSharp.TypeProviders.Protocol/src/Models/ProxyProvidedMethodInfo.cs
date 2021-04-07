using System;
using System.Collections.Generic;
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
  public class ProxyProvidedMethodInfo : ProvidedMethodInfo, IRdProvidedEntity
  {
    private readonly RdProvidedMethodInfo myMethodInfo;
    private readonly int myTypeProviderId;
    private readonly TypeProvidersContext myTypeProvidersContext;
    private readonly ProvidedTypeContextHolder myContext;
    public int EntityId => myMethodInfo.EntityId;
    public RdProvidedEntityType EntityType => RdProvidedEntityType.MethodInfo;

    private RdProvidedMethodInfoProcessModel RdProvidedMethodInfoProcessModel =>
      myTypeProvidersContext.Connection.ProtocolModel.RdProvidedMethodInfoProcessModel;

    private ProxyProvidedMethodInfo(RdProvidedMethodInfo methodInfo, int typeProviderId,
      TypeProvidersContext typeProvidersContext, ProvidedTypeContextHolder context) : base(null, context.Context)
    {
      myMethodInfo = methodInfo;
      myTypeProviderId = typeProviderId;
      myTypeProvidersContext = typeProvidersContext;
      myContext = context;

      myParameters = new InterruptibleLazy<ProvidedParameterInfo[]>(
        // ReSharper disable once CoVariantArrayConversion
        () => myTypeProvidersContext.Connection
          .ExecuteWithCatch(() => RdProvidedMethodInfoProcessModel.GetParameters.Sync(EntityId))
          .Select(t => ProxyProvidedParameterInfo.Create(t, myTypeProviderId, typeProvidersContext, context))
          .ToArray());

      myStaticParameters = new InterruptibleLazy<ProvidedParameterInfo[]>(
        // ReSharper disable once CoVariantArrayConversion
        () => myTypeProvidersContext.Connection
          .ExecuteWithCatch(() => RdProvidedMethodInfoProcessModel.GetStaticParametersForMethod.Sync(EntityId))
          .Select(t => ProxyProvidedParameterInfo.Create(t, myTypeProviderId, typeProvidersContext, context))
          .ToArray());

      myGenericArguments = new InterruptibleLazy<ProvidedType[]>(() =>
        myTypeProvidersContext.ProvidedTypesCache.GetOrCreateBatch(methodInfo.GenericArguments, myTypeProviderId,
          context));

      myTypes = new InterruptibleLazy<ProvidedType[]>(() =>
        myTypeProvidersContext.ProvidedTypesCache.GetOrCreateBatch(
          new[] {methodInfo.DeclaringType, methodInfo.ReturnType},
          myTypeProviderId, context));

      myCustomAttributes = new InterruptibleLazy<RdCustomAttributeData[]>(() =>
        myTypeProvidersContext.ProvidedCustomAttributeProvider.GetCustomAttributes(this));
    }

    [ContractAnnotation("methodInfo:null => null")]
    public static ProxyProvidedMethodInfo Create(RdProvidedMethodInfo methodInfo, int typeProviderId,
      TypeProvidersContext typeProvidersContext, ProvidedTypeContextHolder context) =>
      methodInfo == null
        ? null
        : new ProxyProvidedMethodInfo(methodInfo, typeProviderId, typeProvidersContext, context);

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
          RpcTimeouts.Maximal)), myTypeProviderId, myTypeProvidersContext, myContext);

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
