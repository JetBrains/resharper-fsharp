package model

import com.jetbrains.rider.model.nova.ide.SolutionModel
import com.jetbrains.rd.generator.nova.*
import com.jetbrains.rd.generator.nova.PredefinedType.*
import com.jetbrains.rd.generator.nova.csharp.CSharp50Generator
import java.io.File

@Suppress("unused")
object RdFSharpTypeProvidersLoaderModel : Root(
        CSharp50Generator(FlowTransform.AsIs, "JetBrains.Rider.FSharp.TypeProvidersProtocol.Server", File("C:\\Programming\\fsharp-support\\ReSharper.FSharp\\src\\FSharp.TypeProvidersProtocol\\src\\Server")),
        CSharp50Generator(FlowTransform.Reversed, "JetBrains.Rider.FSharp.TypeProvidersProtocol.Client", File("C:\\Programming\\fsharp-support\\ReSharper.FSharp\\src\\FSharp.TypeProvidersProtocol\\src\\Client"))
) {
    lateinit var RdProvidedType: Class.Concrete
    lateinit var RdProvidedPropertyInfo: Class.Concrete
    lateinit var RdProvidedMethodInfo: Class.Concrete
    lateinit var RdProvidedParameterInfo: Class.Concrete

    private val RdProvidedMemberProcessModel = baseclass {
        call("DeclaringType", int, RdProvidedType.nullable)
    }

    private val RdProvidedMethodBaseProcessModel = baseclass extends RdProvidedMemberProcessModel {
        call("GetParameters", int, array(RdProvidedParameterInfo))
        call("GetGenericArguments", int, array(int))
        //call("GetStaticParametersForMethod", RdTypeProvider, array(RdProvidedParameterInfo))
    }

    private val RdProvidedEntity = baseclass {
        field("EntityId", int)
    }

    private val RdProvidedMemberInfo = baseclass extends RdProvidedEntity {
        field("Name", string)
    }

    private val RdProvidedMethodBase = baseclass extends RdProvidedMemberInfo {
        field("IsGenericMethod", bool)
        field("IsStatic", bool)
        field("IsFamily", bool)
        field("IsFamilyAndAssembly", bool)
        field("IsFamilyOrAssembly", bool)
        field("IsVirtual", bool)
        field("IsFinal", bool)
        field("IsPublic", bool)
        field("IsAbstract", bool)
        field("IsHideBySig", bool)
        field("IsConstructor", bool)
    }

    private val RdResolutionEnvironment = structdef {
        field("resolutionFolder", string)
        field("outputFile", string.nullable) //string option
        field("showResolutionMessages", bool)
        field("referencedAssemblies", array(string))
        field("temporaryFolder", string)
    }

    private val RdProvidedNamespaceProcessModel = aggregatedef("RdProvidedNamespaceProcessModel") {
        call("GetNestedNamespaces", int, array(RdProvidedNamespace))
        call("GetTypes", int, array(int))
        call("ResolveTypeName", structdef("ResolveTypeNameArgs") {
            field("Id", int)
            field("TypeFullName", string)
        }, int)
    }

    private val RdProvidedNamespace = classdef {
        field("NamespaceName", string)
        field("EntityId", int)
    }

    private val RdTypeProviderProcessModel = aggregatedef("RdTypeProviderProcessModel") {
        signal("Invalidate", int).async
        call("GetNamespaces", int, array(RdProvidedNamespace))
        call("GetProvidedType", structdef("GetProvidedTypeArgs") {
            field("Id", int)
        }, RdProvidedType)
        call("GetInvokerExpression", structdef("GetInvokerExpressionArgs") {
            field("TypeProviderId", int)
            field("ProvidedMethodBaseId", int)
            field("IsConstructor", bool)
            field("ProvidedVarParamExprIds", array(int)) //with constructor
        }, RdProvidedExpr)
        call("Dispose", int, void)
    }

    private val RdStaticArg = structdef {
        field("TypeName", string)
        field("Value", string)
    }

    private val ApplyStaticArgumentsParameters = structdef {
        field("Id", int)
        field("TypePathWithArguments", array(string))
        field("StaticArguments", array(RdStaticArg))
    }

    private val GetStaticArgumentsParameters = structdef {
        field("TypeProviderId", int)
        field("TypeWithoutArguments", string)
    }

    private val GetGeneratedAssemblyContentsParameters = structdef {
        field("TypeProviderId", int)
        field("syntheticMethodBase", string)
        field("parameters", string)
    }

    private val RdPublicKey = structdef {
        field("IsKey", bool)
        field("Data", array(byte))
    }

    private val RdVersion = structdef {
        field("Major", int)
        field("Minor", int)
        field("Build", int)
        field("Revision", int)
    }

    private val RdILAssemblyRef = structdef {
        field("Name", string)
        field("Hash", array(byte).nullable)
        field("RdPublicKey", RdPublicKey.nullable)
        field("Retargetable", bool)
        field("RdVersion", RdVersion.nullable)
        field("Locale", string.nullable)
    }

    private val RdILModuleRef = structdef {
        field("Name", string)
        field("HasMetadata", bool)
        field("Hash", array(byte).nullable)
    }

    private val RdILScopeRef = structdef {
        field("IsLocal", bool)
        field("IsPrimaryAssembly", bool)
        field("ModuleRef", RdILModuleRef.nullable)
        field("AssemblyRef", RdILAssemblyRef.nullable)
    }

    private val RdSystemRuntimeContainsType = structdef {
        field("systemRuntimeContainsTypeRef", structdef {
            field("value", structdef {
                field("fakeTcImports", RdFakeTcImports)
            })
        })
    }

    private val InstantiateTypeProvidersOfAssemblyParameters = structdef {
        field("runTimeAssemblyFileName", string)
        field("IlScopeRefOfRuntimeAssembly", RdILScopeRef)
        field("designTimeAssemblyNameString", string)
        field("RdResolutionEnvironment", RdResolutionEnvironment)
        field("isInvalidationSupported", bool)
        field("isInteractive", bool)
        field("systemRuntimeAssemblyVersion", string)
        field("compilerToolsPath", array(string))
        field("systemRuntimeContainsType", RdSystemRuntimeContainsType)
    }

    private val RdFakeDllInfo = structdef {
        field("FileName", string)
    }

    private val RdFakeTcImports = structdef {
        field("Base", this.nullable)
        field("dllInfos", array(RdFakeDllInfo))
    }

    private val RdProvidedTypeProcessModel = aggregatedef("RdProvidedTypeProcessModel") {
        call("BaseType", int, int.nullable)
        call("GetNestedTypes", int, array(int))
        call("GetAllNestedTypes", int, array(int))
        call("GetInterfaces", int, array(int))
        call("GetGenericTypeDefinition", int, int)
        call("GetElementType", int, int)
        call("GetGenericArguments", int, array(int))
        call("GetArrayRank", int, int)
        call("GetEnumUnderlyingType", int, int.nullable)
        call("GetProperties", int, array(RdProvidedPropertyInfo))
        call("GenericParameterPosition", int, int)
        call("GetStaticParameters", int, array(RdProvidedParameterInfo))
        call("ApplyStaticArguments", ApplyStaticArgumentsParameters, int)
        call("GetMethods", int, array(RdProvidedMethodInfo))
        call("DeclaringType", int, int.nullable)
        call("Assembly", int, RdProvidedAssembly)
        call("MakePointerType", int, int)
        call("MakeByRefType", int, int)
        call("MakeArrayType", structdef("MakeArrayTypeArgs") {
            field("Id", int)
            field("Rank", int)
        }, int)
        call("GetFields", int, array(RdProvidedFieldInfo))
        call("GetEvents", int, array(RdProvidedEventInfo))
        call("GetConstructors", int, array(RdProvidedConstructorInfo))
        call("Fresh", int, RdProvidedVar)
    }

    private val RdProvidedMethodInfoProcessModel = aggregatedef("RdProvidedMethodInfoProcessModel") {
        call("DeclaringType", int, int.nullable)
        call("ReturnType", int, int)
        call("GetParameters", int, array(RdProvidedParameterInfo))
        call("GetGenericArguments", int, array(int))
    }

    private val RdProvidedConstructorInfoProcessModel = aggregatedef("RdProvidedConstructorInfoProcessModel") {
        call("DeclaringType", int, int.nullable)
        call("GetParameters", int, array(RdProvidedParameterInfo))
        call("GetGenericArguments", int, array(int))
        call("GetStaticParametersForMethod", int, array(RdProvidedParameterInfo))
    }

    private val RdProvidedPropertyInfoProcessModel = aggregatedef("RdProvidedPropertyInfoProcessModel") {
        call("DeclaringType", int, int.nullable)
        call("PropertyType", int, int)
        call("GetGetMethod", int, RdProvidedMethodInfo.nullable)
        call("GetSetMethod", int, RdProvidedMethodInfo.nullable)
        call("GetIndexParameters", int, array(RdProvidedParameterInfo))
    }

    private val RdProvidedParameterInfoProcessModel = aggregatedef("RdProvidedParameterInfoProcessModel") {
        call("ParameterType", int, int)
    }

    private val RdTypeProvider = structdef {
        field("TypeProviderId", int)
    }

    private val RdAssemblyName = structdef {
        field("Name", string)
        field("PublicKey", RdPublicKey.nullable)
        field("Version", string)
        field("Flags", int)
    }

    private val RdProvidedAssembly = structdef {
        field("FullName", string)
        field("EntityId", int)
    }

    private val RdProvidedAssemblyProcessModel = aggregatedef("RdProvidedAssemblyProcessModel") {
        call("GetManifestModuleContents", int, array(byte))
        call("GetName", int, RdAssemblyName)
    }

    private val RdProvidedFieldInfoProcessModel = aggregatedef("RdProvidedFieldInfoProcessModel") {
        call("FieldType", int, int)
        call("DeclaringType", int, int)
        call("GetRawConstantValue", int, RdStaticArg)
    }

    private val RdProvidedFieldInfo = classdef extends RdProvidedMemberInfo {
        field("IsInitOnly", bool)
        field("IsStatic", bool)
        field("IsSpecialName", bool)
        field("IsLiteral", bool)
        field("GetRawConstantValue", RdStaticArg)
        field("IsPublic", bool)
        field("IsFamily", bool)
        field("IsFamilyAndAssembly", bool)
        field("IsFamilyOrAssembly", bool)
        field("IsPrivate", bool)
    }

    private val RdProvidedEventInfoProcessModel = aggregatedef("RdProvidedEventInfoProcessModel") {
        call("GetAddMethod", int, RdProvidedMethodInfo)
        call("GetRemoveMethod", int, RdProvidedMethodInfo)
        call("DeclaringType", int, int.nullable)
        call("EventHandlerType", int, int)
    }

    private val RdProvidedEventInfo = classdef extends RdProvidedMemberInfo {

    }

    private val RdProvidedConstructorInfo = classdef extends RdProvidedMethodBase {
    }

    private val RdProvidedVar = classdef extends RdProvidedMemberInfo {
        field("IsMutable", bool)
    }

    private val RdProvidedExpr = classdef extends RdProvidedEntity {
        field("UnderlyingExpressionString", string)
    }

    private val RdProvidedExprProcessModel = aggregatedef("RdProvidedExprProcessModel") {
        call("Type", int, int)
        call("GetExprType", int, RdProvidedExprType)
    }

    private val RdProvidedVarProcessModel = aggregatedef("RdProvidedVarProcessModel") {
        call("Type", int, int)
    }

    private val RdProvidedExprType = classdef {
        field("HasValue", bool)
        field("ProvidedNewArrayExpr", classdef("ProvidedNewArrayExpr") {
            field("ProvidedType", int)
            field("ProvidedExprs", array(RdProvidedExpr))
        }.nullable)
        //field("ProvidedAddressOfExpr", classdef {
        //    field("ProvidedExpr", RdProvidedExpr)
        //})
        field("ProvidedNewObjectExpr", classdef("ProvidedNewObjectExpr") {
            field("ProvidedConstructorInfo", RdProvidedConstructorInfo)
            field("ProvidedExprs", array(RdProvidedExpr))
        }.nullable)
        field("ProvidedWhileLoopExpr", classdef("ProvidedWhileLoopExpr") {
            field("ProvidedExpr1", RdProvidedExpr)
            field("ProvidedExpr2", RdProvidedExpr)
        }.nullable)
        field("ProvidedNewDelegateExpr", classdef("ProvidedNewDelegateExpr") {
            field("ProvidedType", int)
            field("ProvidedVars", array(RdProvidedVar))
            field("ProvidedExpr", RdProvidedExpr)
        }.nullable)
        field("ProvidedForIntegerRangeLoopExpr", classdef("ProvidedForIntegerRangeLoopExpr") {
            field("ProvidedVar", RdProvidedVar)
            field("ProvidedExpr1", RdProvidedExpr)
            field("ProvidedExpr2", RdProvidedExpr)
            field("ProvidedExpr3", RdProvidedExpr)
        }.nullable)
        field("ProvidedSequentialExpr", classdef("ProvidedSequentialExpr") {
            field("ProvidedExpr1", RdProvidedExpr)
            field("ProvidedExpr2", RdProvidedExpr)
        }.nullable)
        field("ProvidedTryWithExpr", classdef("ProvidedTryWithExpr") {
            field("ProvidedExpr1", RdProvidedExpr)
            field("ProvidedVar1", RdProvidedVar)
            field("ProvidedExpr2", RdProvidedExpr)
            field("ProvidedVar2", RdProvidedVar)
            field("ProvidedExpr3", RdProvidedExpr)
        }.nullable)
        field("ProvidedTryFinallyExpr", classdef("ProvidedTryFinallyExpr") {
            field("ProvidedExpr1", RdProvidedExpr)
            field("ProvidedExpr2", RdProvidedExpr)
        }.nullable)
        field("ProvidedLambdaExpr", classdef("ProvidedLambdaExpr") {
            field("ProvidedVar", RdProvidedVar)
            field("ProvidedExpr", RdProvidedExpr)
        }.nullable)
        field("ProvidedCallExpr", classdef("ProvidedCallExpr") {
            field("ProvidedExpr", RdProvidedExpr.nullable)
            field("ProvidedMethodInfo", RdProvidedMethodInfo)
            field("ProvidedExprs", array(RdProvidedExpr))
        }.nullable)
        field("ProvidedConstantExpr", classdef("ProvidedConstantExpr") {
            field("Obj", RdStaticArg)
            field("ProvidedType", int)
        }.nullable)
        field("ProvidedDefaultExpr", classdef("ProvidedDefaultExpr") {
            field("ProvidedType", int)
        }.nullable)
        field("ProvidedNewTupleExpr", classdef("ProvidedNewTupleExpr") {
            field("ProvidedExprs", array(RdProvidedExpr))
        }.nullable)
        field("ProvidedTupleGetExpr", classdef("ProvidedTupleGetExpr") {
            field("ProvidedExpr", RdProvidedExpr)
            field("Int", int)
        }.nullable)
        field("ProvidedTypeAsExpr", classdef("ProvidedTypeAsExpr") {
            field("ProvidedExpr", RdProvidedExpr)
            field("ProvidedType", int)
        }.nullable)
        field("ProvidedTypeTestExpr", classdef("ProvidedTypeTestExpr") {
            field("ProvidedExpr", RdProvidedExpr)
            field("ProvidedType", int)
        }.nullable)
        field("ProvidedLetExpr", classdef("ProvidedLetExpr") {
            field("ProvidedVar", RdProvidedVar)
            field("ProvidedExpr1", RdProvidedExpr)
            field("ProvidedExpr2", RdProvidedExpr)
        }.nullable)
        field("ProvidedVarSetExpr", classdef("ProvidedVarSetExpr") {
            field("ProvidedVar", RdProvidedVar)
            field("ProvidedExpr", RdProvidedExpr)
        }.nullable)
        field("ProvidedIfThenElseExpr", classdef("ProvidedIfThenElseExpr") {
            field("ProvidedExpr1", RdProvidedExpr)
            field("ProvidedExpr2", RdProvidedExpr)
            field("ProvidedExpr3", RdProvidedExpr)
        }.nullable)
        field("ProvidedVarExpr", classdef("ProvidedVarExpr") {
            field("ProvidedVar", RdProvidedVar)
        }.nullable)
    }

    init {
        RdProvidedType = classdef extends RdProvidedMemberInfo {
            field("FullName", string.nullable)
            field("Namespace", string.nullable)
            field("IsVoid", bool)
            field("IsGenericParameter", bool)
            field("IsValueType", bool)
            field("IsByRef", bool)
            field("IsPointer", bool)
            field("IsEnum", bool)
            field("IsArray", bool)
            field("IsInterface", bool)
            field("IsClass", bool)
            field("IsSealed", bool)
            field("IsAbstract", bool)
            field("IsPublic", bool)
            field("IsNestedPublic", bool)
            field("IsSuppressRelocate", bool)
            field("IsErased", bool)
            field("IsGenericType", bool)
            field("IsMeasure", bool)
        }

        RdProvidedPropertyInfo = classdef extends RdProvidedMemberInfo {
            field("CanRead", bool)
            field("CanWrite", bool)
        }

        RdProvidedParameterInfo = classdef extends RdProvidedMemberInfo {
            field("IsIn", bool)
            field("IsOut", bool)
            field("IsOptional", bool)
            field("RawDefaultValue", RdStaticArg.nullable)
            field("HasDefaultValue", bool)
        }

        RdProvidedMethodInfo = classdef extends RdProvidedMethodBase {
            field("MetadataToken", int)
        }

        field("RdTypeProviderProcessModel", RdTypeProviderProcessModel)
        field("RdProvidedNamespaceProcessModel", RdProvidedNamespaceProcessModel)
        field("RdProvidedTypeProcessModel", RdProvidedTypeProcessModel)
        field("RdProvidedPropertyInfoProcessModel", RdProvidedPropertyInfoProcessModel)
        field("RdProvidedMethodInfoProcessModel", RdProvidedMethodInfoProcessModel)
        field("RdProvidedConstructorInfoProcessModel", RdProvidedConstructorInfoProcessModel)
        field("RdProvidedParameterInfoProcessModel", RdProvidedParameterInfoProcessModel)
        field("RdProvidedAssemblyProcessModel", RdProvidedAssemblyProcessModel)
        field("RdProvidedFieldInfoProcessModel", RdProvidedFieldInfoProcessModel)
        field("RdProvidedEventInfoProcessModel", RdProvidedEventInfoProcessModel)
        field("RdProvidedExprProcessModel", RdProvidedExprProcessModel)
        field("RdProvidedVarProcessModel", RdProvidedVarProcessModel)
        call("InstantiateTypeProvidersOfAssembly", InstantiateTypeProvidersOfAssemblyParameters, array(RdTypeProvider))
    }
}
