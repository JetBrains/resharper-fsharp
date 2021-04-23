using JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol.Models;
using JetBrains.Rider.FSharp.TypeProviders.Protocol.Server;
using JetBrains.Util;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.ModelCreators
{
  public class ProvidedTypeCreator : UniqueProvidedCreatorWithCacheBase<ProvidedType, RdOutOfProcessProvidedType>
  {
    private readonly TypeProvidersContext myTypeProvidersContext;

    public ProvidedTypeCreator(TypeProvidersContext typeProvidersContext) :
      base(typeProvidersContext.ProvidedTypesCache) => myTypeProvidersContext = typeProvidersContext;

    protected override RdOutOfProcessProvidedType CreateRdModelInternal(ProvidedType providedModel, int entityId,
      int typeProviderId)
    {
      var logger = myTypeProvidersContext.Logger;

      var isValueType = logger.Catch(() => !providedModel.IsGenericParameter && providedModel.IsValueType);
      var isClass = logger.Catch(() => !providedModel.IsGenericParameter && providedModel.IsClass);

      var baseTypeId = !providedModel.IsGenericParameter
        ? GetOrCreateId(providedModel.BaseType, typeProviderId)
        : ProvidedConst.DefaultId;

      var declaringTypeId = providedModel.IsGenericParameter ||
                            providedModel.IsArray && providedModel.GetElementType().IsGenericParameter
        ? ProvidedConst.DefaultId
        : GetOrCreateId(providedModel.DeclaringType, typeProviderId);

      var flags = RdProvidedTypeFlags.None;
      if (isClass) flags |= RdProvidedTypeFlags.IsClass;
      if (isValueType) flags |= RdProvidedTypeFlags.IsValueType;
      if (logger.Catch(() => providedModel.IsVoid)) flags |= RdProvidedTypeFlags.IsVoid;
      if (logger.Catch(() => providedModel.IsEnum)) flags |= RdProvidedTypeFlags.IsEnum;
      if (logger.Catch(() => providedModel.IsArray)) flags |= RdProvidedTypeFlags.IsArray;
      if (logger.Catch(() => providedModel.IsByRef)) flags |= RdProvidedTypeFlags.IsByRef;
      if (logger.Catch(() => providedModel.IsSealed)) flags |= RdProvidedTypeFlags.IsSealed;
      if (logger.Catch(() => providedModel.IsPublic)) flags |= RdProvidedTypeFlags.IsPublic;
      if (logger.Catch(() => providedModel.IsErased)) flags |= RdProvidedTypeFlags.IsErased;
      if (logger.Catch(() => providedModel.IsMeasure)) flags |= RdProvidedTypeFlags.IsMeasure;
      if (logger.Catch(() => providedModel.IsPointer)) flags |= RdProvidedTypeFlags.IsPointer;
      if (logger.Catch(() => providedModel.IsAbstract)) flags |= RdProvidedTypeFlags.IsAbstract;
      if (logger.Catch(() => providedModel.IsInterface)) flags |= RdProvidedTypeFlags.IsInterface;
      if (logger.Catch(() => providedModel.IsGenericType)) flags |= RdProvidedTypeFlags.IsGenericType;
      if (logger.Catch(() => providedModel.IsNestedPublic)) flags |= RdProvidedTypeFlags.IsNestedPublic;
      if (logger.Catch(() => providedModel.IsSuppressRelocate)) flags |= RdProvidedTypeFlags.IsSuppressRelocate;
      if (logger.Catch(() => providedModel.IsGenericParameter)) flags |= RdProvidedTypeFlags.IsGenericParameter;

      var genericParameters = providedModel.IsGenericType
        ? providedModel
          .GetGenericArguments()
          .CreateIds(myTypeProvidersContext.ProvidedTypeRdModelsCreator, typeProviderId)
        : null;

      var assembly = myTypeProvidersContext.ProvidedAssemblyRdModelsCreator.GetOrCreateId(
        logger.Catch(() => providedModel.Assembly), typeProviderId);

      var fullName = logger.Catch(() => providedModel.FullName) ?? "";
      var @namespace = logger.Catch(() => providedModel.Namespace) ?? "";

      return new RdOutOfProcessProvidedType(baseTypeId, declaringTypeId, fullName,
        @namespace, flags, genericParameters, assembly, providedModel.Name, entityId);
    }
  }
}
