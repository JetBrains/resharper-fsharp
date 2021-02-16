﻿using System;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Rd.Tasks;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Cache;
using JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils;
using JetBrains.Rider.FSharp.TypeProvidersProtocol.Server;
using JetBrains.Util.Concurrency;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Core.CompilerServices;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Models
{
  public class ProxyProvidedConstructorInfo : ProvidedConstructorInfo, IRdProvidedEntity
  {
    private readonly RdProvidedConstructorInfo myConstructorInfo;
    private readonly int myTypeProviderId;
    private readonly TypeProvidersContext myTypeProvidersContext;
    private readonly ProvidedTypeContextHolder myContext;
    public int EntityId => myConstructorInfo.EntityId;
    public RdProvidedEntityType EntityType => RdProvidedEntityType.ConstructorInfo;

    private RdProvidedConstructorInfoProcessModel RdProvidedConstructorInfoProcessModel =>
      myTypeProvidersContext.Connection.ProtocolModel.RdProvidedConstructorInfoProcessModel;

    private ProxyProvidedConstructorInfo(RdProvidedConstructorInfo constructorInfo, int typeProviderId,
      TypeProvidersContext typeProvidersContext, ProvidedTypeContextHolder context) : base(null, context.Context)
    {
      myConstructorInfo = constructorInfo;
      myTypeProviderId = typeProviderId;
      myTypeProvidersContext = typeProvidersContext;
      myContext = context;

      myParameters = new InterruptibleLazy<ProvidedParameterInfo[]>(() =>
        // ReSharper disable once CoVariantArrayConversion
        myTypeProvidersContext.Connection
          .ExecuteWithCatch(() => RdProvidedConstructorInfoProcessModel.GetParameters.Sync(EntityId))
          .Select(t => ProxyProvidedParameterInfo.Create(t, myTypeProviderId, typeProvidersContext, myContext))
          .ToArray());

      myGenericArguments = new InterruptibleLazy<ProvidedType[]>(() =>
        myTypeProvidersContext.ProvidedTypesCache.GetOrCreateBatch(constructorInfo.GenericArguments, myTypeProviderId,
          myContext));

      myStaticParameters = new InterruptibleLazy<ProvidedParameterInfo[]>(() =>
        // ReSharper disable once CoVariantArrayConversion
        myTypeProvidersContext.Connection
          .ExecuteWithCatch(() => RdProvidedConstructorInfoProcessModel.GetStaticParametersForMethod.Sync(EntityId))
          .Select(t => ProxyProvidedParameterInfo.Create(t, myTypeProviderId, typeProvidersContext, myContext))
          .ToArray());

      myCustomAttributes = new InterruptibleLazy<RdCustomAttributeData[]>(() =>
        myTypeProvidersContext.ProvidedCustomAttributeProvider.GetCustomAttributes(this));
    }

    [ContractAnnotation("constructorInfo:null => null")]
    public static ProxyProvidedConstructorInfo Create(RdProvidedConstructorInfo constructorInfo, int typeProviderId,
      TypeProvidersContext typeProvidersContext, ProvidedTypeContextHolder context) =>
      constructorInfo == null
        ? null
        : new ProxyProvidedConstructorInfo(constructorInfo, typeProviderId, typeProvidersContext, context);

    public override string Name => myConstructorInfo.Name;
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

    public override ProvidedParameterInfo[] GetStaticParametersForMethod(ITypeProvider provider) =>
      myStaticParameters.Value;

    public override ProvidedMethodBase ApplyStaticArgumentsForMethod(ITypeProvider provider,
      string fullNameAfterArguments,
      object[] staticArgs)
    {
      var staticArgDescriptions = staticArgs.Select(PrimitiveTypesBoxer.BoxToServerStaticArg).ToArray();

      var method = Create(myTypeProvidersContext.Connection.ExecuteWithCatch(() =>
        RdProvidedConstructorInfoProcessModel.ApplyStaticArgumentsForMethod.Sync(
          new ApplyStaticArgumentsForMethodArgs(EntityId, fullNameAfterArguments, staticArgDescriptions),
          RpcTimeouts.Maximal)), myTypeProviderId, myTypeProvidersContext, myContext);

      return method;
    }

    public override ProvidedType DeclaringType =>
      myTypeProvidersContext.ProvidedTypesCache.GetOrCreate(myConstructorInfo.DeclaringType, myTypeProviderId,
        myContext);

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

    private bool HasFlag(RdProvidedMethodFlags flag) => (myConstructorInfo.Flags & flag) == flag;

    private string[] myXmlDocs;
    private readonly InterruptibleLazy<ProvidedParameterInfo[]> myParameters;
    private readonly InterruptibleLazy<ProvidedType[]> myGenericArguments;
    private readonly InterruptibleLazy<ProvidedParameterInfo[]> myStaticParameters;
    private readonly InterruptibleLazy<RdCustomAttributeData[]> myCustomAttributes;
  }
}
