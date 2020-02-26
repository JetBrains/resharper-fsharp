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
    private val RdProvidedType = classdef {
        field("Name", string)
        field("FullName", string.nullable)
        field("Namespace", string.nullable)
        field("IsGenericParameter", bool)
        field("IsValueType", bool)
        field("IsByRef", bool)
        field("IsPointer", bool)
        field("IsEnum", bool)
        field("IsInterface", bool)
        field("IsClass", bool)
        field("IsSealed", bool)
        field("IsAbstract", bool)
        field("IsPublic", bool)
        field("IsNestedPublic", bool)
        field("IsSuppressRelocate", bool)
        field("IsErased", bool)
        field("IsGenericType", bool)
        field("BaseType", this)

        call("GetNestedType", string, this)
        call("GetNestedTypes", void, array(this))
        call("GetAllNestedTypes", void, array(this))
        call("GetInterfaces", void, array(this))
        call("GetGenericTypeDefinition", void, this)
    }

    private val ParameterInfo = structdef {

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
        call("GetStaticParameters", GetStaticArgumentsParameters, array(string))
        call("ApplyStaticArguments", ApplyStaticArgumentsParameters, array(ParameterInfo))
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

    private val InstantiateResult = structdef {
        field("TypeProviderTypeName", string)
        field("isSuccsessful", bool)
        field("TypeProviderId", int)
    }

    init {
        call("InstantiateTypeProvidersOfAssembly", InstantiateTypeProvidersOfAssemblyParameters, array(RdTypeProvider))
    }
}

