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

    private val RdProvidedMemberInfo = baseclass {
        field("Name", string)

        call("DeclaringType", void, RdProvidedType.nullable)
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

        call("GetParameters", void, array(RdProvidedParameterInfo))
        call("GetGenericArguments", void, array(RdProvidedType))
        //call("GetStaticParametersForMethod", RdTypeProvider, array(RdProvidedParameterInfo))
    }


    private val RdResolutionEnvironment = structdef {
        field("resolutionFolder", string)
        field("outputFile", string.nullable) //string option
        field("showResolutionMessages", bool)
        field("referencedAssemblies", array(string))
        field("temporaryFolder", string)
    }

    private val RdProvidedNamespace = classdef {
        field("namespaceName", string)

        call("getNestedNamespaces", void, array(this))
        call("getTypes", void, array(RdProvidedType))
        call("resolveTypeName", string, RdProvidedType)
    }

    private val RdTypeProvider = classdef {
        //sink("invalidate", void).async

        call("GetNamespaces", void, array(RdProvidedNamespace))
        call("GetInvokerExpression", GetInvokerExpressionParameters, string)
        call("GetGeneratedAssemblyContents", GetGeneratedAssemblyContentsParameters, array(byte))
    }

    private val ApplyStaticArgumentsParameters = structdef {
        field("TypeProviderId", int)
        field("TypeWithoutArguments", string)
        field("TypePathWithArguments", array(string))
        field("staticArguments", array(string))
    }

    private val GetStaticArgumentsParameters = structdef {
        field("TypeProviderId", int)
        field("TypeWithoutArguments", string)
    }

    private val GetInvokerExpressionParameters = structdef {
        field("TypeProviderId", int)
        field("syntheticMethodBase", string)
        field("parameters", string)
    }

    private val GetGeneratedAssemblyContentsParameters = structdef {
        field("TypeProviderId", int)
        field("syntheticMethodBase", string)
        field("parameters", string)
    }

    private val RdPublicKey = structdef {
        field("IsKey", bool)
        field("IsKeyToken", bool)
        field("Key", array(byte))
        field("KeyToken", array(byte))
    }

    private val RdVersion = structdef {
        field("Major", int)
        field("Minor", int)
        field("Build", int)
        field("Revision", int)
    }

    private val RdILAssemblyRef = structdef {
        field("Name", string)
        field("QualifiedName", string)
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
        field("IsModuleRef", bool)
        field("IsAssemblyRef", bool)
        field("ModuleRef", RdILModuleRef.nullable)
        field("AssemblyRef", RdILAssemblyRef.nullable)
        field("QualifiedName", string)
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
        field("RdILScopeRefOfRuntimeAssembly", RdILScopeRef)
        field("designTimeAssemblyNameString", string)
        field("RdResolutionEnvironment", RdResolutionEnvironment)
        field("isInvalidationSupported", bool)
        field("isInteractive", bool)
        field("systemRuntimeAssemblyRdVersion", RdVersion)
        field("systemRuntimeContainsType", RdSystemRuntimeContainsType)
    }

    private val RdFakeDllInfo = structdef {
        field("FileName", string)
    }

    private val RdFakeTcImports = structdef {
        field("Base", this.nullable)
        field("dllInfos", array(RdFakeDllInfo))
    }

    init {
        RdProvidedType = classdef extends RdProvidedMemberInfo {
            field("FullName", string.nullable)
            field("Namespace", string.nullable)
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

            call("BaseType", void, this.nullable)
            call("GetNestedType", string, this)
            call("GetNestedTypes", void, array(this))
            call("GetAllNestedTypes", void, array(this))
            call("GetInterfaces", void, array(this))
            call("GetGenericTypeDefinition", void, this)
            call("GetElementType", void, this)
            call("GetGenericArguments", void, array(this))
            call("GetArrayRank", void, int)
            call("GetEnumUnderlyingType", void, this.nullable)
            call("GetProperties", void, array(RdProvidedPropertyInfo))
            call("GetProperty", string, RdProvidedPropertyInfo)
            call("GenericParameterPosition", void, int)
            call("GetStaticParameters", void, array(RdProvidedParameterInfo))
            //call("ApplyStaticArguments", ApplyStaticArgumentsParameters, this)
            call("GetMethods", void, array(RdProvidedMethodInfo))
        }

        RdProvidedPropertyInfo = classdef extends RdProvidedMemberInfo {
            field("CanRead", bool)
            field("CanWrite", bool)

            call("PropertyType", void, RdProvidedType)
            call("GetGetMethod", void, RdProvidedMethodInfo)
            call("GetSetMethod", void, RdProvidedMethodInfo)
            call("GetIndexParameters", void, array(RdProvidedParameterInfo))
        }

        RdProvidedParameterInfo = classdef {
            field("Name", string)
            field("IsIn", bool)
            field("IsOut", bool)
            field("IsOptional", bool)
            //field("RawDefaultValue : obj
            field("HasDefaultValue", bool)

            call("ParameterType", void, RdProvidedType)
        }

        RdProvidedMethodInfo = classdef extends RdProvidedMethodBase {
            field("MetadataToken", int)

            call("ReturnType", void, RdProvidedType)
        }

        call("InstantiateTypeProvidersOfAssembly", InstantiateTypeProvidersOfAssemblyParameters, array(RdTypeProvider))
    }
}

