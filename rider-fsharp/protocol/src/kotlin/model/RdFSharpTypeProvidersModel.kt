package model

import com.jetbrains.rider.model.nova.ide.SolutionModel
import com.jetbrains.rd.generator.nova.*
import com.jetbrains.rd.generator.nova.PredefinedType.*
import com.jetbrains.rd.generator.nova.csharp.CSharp50Generator
import java.io.File

@Suppress("unused")
object RdFSharpTypeProvidersModel : Root() {

    lateinit var RdProvidedPropertyInfo: Struct.Concrete
    lateinit var RdProvidedMethodInfo: Struct.Concrete
    lateinit var RdProvidedParameterInfo: Struct.Concrete

    private val RdProvidedMemberProcessModel = baseclass {
        call("DeclaringType", int, int)
    }

    private val RdProvidedMethodBaseProcessModel = baseclass extends RdProvidedMemberProcessModel {
        call("GetParameters", int, array(RdProvidedParameterInfo))
        call("GetGenericArguments", int, array(int))
    }

    private val RdProvidedEntity = basestruct {
        field("EntityId", int)
    }

    private val RdProvidedMemberInfo = basestruct extends RdProvidedEntity {
        field("Name", string.nullable)
    }

    private var RdOutOfProcessProvidedType = structdef extends RdProvidedMemberInfo {
        field("BaseType", int)
        field("DeclaringType", int)
        field("FullName", string.nullable)
        field("Namespace", string.nullable)
        field("Flags", RdProvidedTypeFlags)
        field("GenericArguments", array(int).nullable)
        field("Assembly", int)
    }

    private val RdProvidedMethodBase = basestruct extends RdProvidedMemberInfo {
        field("DeclaringType", int)
        field("Flags", RdProvidedMethodFlags)
        field("GenericArguments", array(int).nullable)
    }

    private val RdResolutionEnvironment = structdef {
        field("ResolutionFolder", string)
        field("OutputFile", string.nullable)
        field("ShowResolutionMessages", bool)
        field("ReferencedAssemblies", array(string))
        field("TemporaryFolder", string)
    }

    private val RdProvidedNamespace = structdef {
        field("Name", string.nullable) // if null then global namespace
        field("NestedNamespaces", array(this))
        field("Types", array(int))
    }

    private val RdCustomAttributeNamedArgument = structdef {
        field("MemberName", string)
        field("TypedValue", RdAttributeArg)
    }

    private val RdCustomAttributeData = structdef {
        field("FullName", string)
        field("NamedArguments", array(RdCustomAttributeNamedArgument))
        field("ConstructorArguments", array(RdAttributeArg))
    }

    private val RdTypeProviderProcessModel = aggregatedef("RdTypeProviderProcessModel") {
        signal("Invalidate", int)
        call("InvalidateExternalTP", int, void)
        call("GetProvidedNamespaces", int, array(RdProvidedNamespace))
        call("Dispose", array(int), void)
        call("GetCustomAttributes", structdef("GetCustomAttributesArgs") {
            field("EntityId", int)
            field("ProvidedEntityType", RdProvidedEntityType)
        }, array(RdCustomAttributeData))
        call(
            "InstantiateTypeProvidersOfAssembly",
            InstantiateTypeProvidersOfAssemblyParameters,
            structdef("InstantiationResult") {
                field("TypeProviders", array(RdTypeProvider))
                field("CachedIds", array(int))
            })
        call("Kill", void, void)
    }

    private val RdStaticArg = structdef {
        field("TypeName", RdTypeName)
        field("Value", string)
    }

    private val RdAttributeArgElement = structdef {
        field("TypeName", string)
        field("Value", string.nullable)
    }

    private val RdAttributeArg = structdef {
        field("TypeName", string)
        field("IsArray", bool)
        field("Values", array(RdAttributeArgElement))
    }

    private val ApplyStaticArgumentsParameters = structdef {
        field("Id", int)
        field("TypePathWithArguments", array(string))
        field("StaticArguments", array(RdStaticArg))
    }

    private val GetStaticArgumentsParameters = structdef {
        field("TypeWithoutArguments", string)
    }

    private val GetGeneratedAssemblyContentsParameters = structdef {
        field("TypeProviderId", int)
        field("SyntheticMethodBase", string)
        field("Parameters", string)
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

    private val RdSystemRuntimeContainsType = structdef {
        field("SystemRuntimeContainsTypeRef", structdef {
            field("Value", structdef {
                field("FakeTcImports", RdFakeTcImports)
            })
        })
    }

    private val InstantiateTypeProvidersOfAssemblyParameters = structdef {
        field("RunTimeAssemblyFileName", string)
        field("DesignTimeAssemblyNameString", string)
        field("RdResolutionEnvironment", RdResolutionEnvironment)
        field("IsInvalidationSupported", bool)
        field("IsInteractive", bool)
        field("SystemRuntimeAssemblyVersion", string)
        field("CompilerToolsPath", array(string))
        field("FakeTcImports", RdFakeTcImports)
        field("EnvironmentPath", string)
    }

    private val RdFakeDllInfo = structdef {
        field("FileName", string)
    }

    private val RdFakeTcImports = structdef {
        field("Base", this.nullable)
        field("DllInfos", array(RdFakeDllInfo))
    }

    private val RdProvidedTypeProcessModel = aggregatedef("RdProvidedTypeProcessModel") {
        call("GetProvidedType", int, RdOutOfProcessProvidedType)
        call("GetProvidedTypes", array(int), array(RdOutOfProcessProvidedType))
        call("GetGenericTypeDefinition", int, int)
        call("GetElementType", int, int)
        call("GetArrayRank", int, int)
        call("GetEnumUnderlyingType", int, int)
        call("GenericParameterPosition", int, int)
        call("GetStaticParameters", int, array(RdProvidedParameterInfo))
        call("ApplyStaticArguments", ApplyStaticArgumentsParameters, int)
        call("MakePointerType", int, int)
        call("MakeByRefType", int, int)
        call("MakeArrayType", structdef("MakeArrayTypeArgs") {
            field("Id", int)
            field("Rank", int)
        }, int)
        call("MakeGenericType", structdef("MakeGenericTypeArgs") {
            field("EntityId", int)
            field("ArgIds", array(int))
        }, int)
        call("GetContent", int, RdProvidedTypeContent)
        call("GetAllNestedTypes", int, array(int))
    }

    private val ApplyStaticArgumentsForMethodArgs = structdef("ApplyStaticArgumentsForMethodArgs") {
        field("EntityId", int)
        field("FullNameAfterArguments", string)
        field("StaticArgs", array(RdStaticArg))
    }

    private val RdProvidedMethodInfoProcessModel = aggregatedef("RdProvidedMethodInfoProcessModel") {
        call("GetProvidedMethodInfo", int, RdProvidedMethodInfo)
        call("GetProvidedMethodInfos", array(int), array(RdProvidedMethodInfo))
        call("GetStaticParametersForMethod", int, array(RdProvidedParameterInfo))
        call("ApplyStaticArgumentsForMethod", ApplyStaticArgumentsForMethodArgs, RdProvidedMethodInfo)
        call("GetParameters", int, array(RdProvidedParameterInfo))
    }

    private val RdProvidedConstructorInfoProcessModel = aggregatedef("RdProvidedConstructorInfoProcessModel") {
        call("GetStaticParametersForMethod", int, array(RdProvidedParameterInfo))
        call("ApplyStaticArgumentsForMethod", ApplyStaticArgumentsForMethodArgs, RdProvidedConstructorInfo)
        call("GetParameters", int, array(RdProvidedParameterInfo))
    }

    private val RdTypeProvider = structdef {
        field("EntityId", int)
        field("Name", string.nullable)
        field("FullName", string)
    }

    private val RdAssemblyName = structdef {
        field("Name", string)
        field("PublicKey", RdPublicKey.nullable)
        field("Version", string.nullable)
        field("Flags", int)
    }

    private val RdProvidedAssembly = structdef extends RdProvidedEntity {
        field("FullName", string)
        field("AssemblyName", RdAssemblyName)
    }

    private val RdProvidedAssemblyProcessModel = aggregatedef("RdProvidedAssemblyProcessModel") {
        call("GetProvidedAssembly", int, RdProvidedAssembly)
        call("GetManifestModuleContents", int, array(byte))
    }

    private val RdProvidedFieldInfo = structdef {
        field("Name", string.nullable)
        field("FieldType", int)
        field("DeclaringType", int)
        field("RawConstantValue", RdStaticArg.nullable)
        field("Flags", RdProvidedFieldFlags)
        field("CustomAttributes", array(RdCustomAttributeData))
    }

    private val RdProvidedEventInfo = structdef {
        field("Name", string.nullable)
        field("DeclaringType", int)
        field("EventHandlerType", int)
        field("AddMethod", int)
        field("RemoveMethod", int)
        field("CustomAttributes", array(RdCustomAttributeData))
    }

    private val RdProvidedTypeContent = structdef {
        field("Interfaces", array(int))
        field("Constructors", array(RdProvidedConstructorInfo))
        field("Methods", array(RdProvidedMethodInfo))
        field("Properties", array(RdProvidedPropertyInfo))
        field("Fields", array(RdProvidedFieldInfo))
        field("Events", array(RdProvidedEventInfo))
    }

    private val RdProvidedConstructorInfo = structdef extends RdProvidedMethodBase {
    }

    private val RdProvidedTypeFlags = flags {
        +"None"
        +"IsVoid"
        +"IsGenericParameter"
        +"IsValueType"
        +"IsByRef"
        +"IsPointer"
        +"IsEnum"
        +"IsArray"
        +"IsInterface"
        +"IsClass"
        +"IsSealed"
        +"IsAbstract"
        +"IsPublic"
        +"IsNestedPublic"
        +"IsSuppressRelocate"
        +"IsErased"
        +"IsGenericType"
        +"IsMeasure"
        +"IsCreatedByProvider"
    }

    private val RdProvidedFieldFlags = flags {
        +"None"
        +"IsInitOnly"
        +"IsStatic"
        +"IsSpecialName"
        +"IsLiteral"
        +"IsPublic"
        +"IsFamily"
        +"IsFamilyAndAssembly"
        +"IsFamilyOrAssembly"
        +"IsPrivate"
    }

    private val RdProvidedMethodFlags = flags {
        +"None"
        +"IsGenericMethod"
        +"IsStatic"
        +"IsFamily"
        +"IsFamilyAndAssembly"
        +"IsFamilyOrAssembly"
        +"IsVirtual"
        +"IsFinal"
        +"IsPublic"
        +"IsAbstract"
        +"IsHideBySig"
        +"IsConstructor"
    }

    private val RdTypeName = enum {
        +"sbyte"
        +"short"
        +"int"
        +"long"
        +"byte"
        +"ushort"
        +"uint"
        +"ulong"
        +"decimal"
        +"float"
        +"double"
        +"char"
        +"bool"
        +"string"
        +"dbnull"
        +"unknown"
    }

    private val RdProvidedEntityType = flags {
        +"TypeProvider"
        +"TypeInfo"
        +"MethodInfo"
        +"ConstructorInfo"
        +"FieldInfo"
        +"PropertyInfo"
        +"EventInfo"
    }

    private val RdTestHost = aggregatedef("RdTestHost") {
        call("RuntimeVersion", void, string.nullable)
        call("Dump", void, string)
    }

    init {
        RdProvidedPropertyInfo = structdef extends RdProvidedMemberInfo {
            field("DeclaringType", int)
            field("PropertyType", int)
            field("GetMethod", int)
            field("SetMethod", int)
            field("CanRead", bool)
            field("CanWrite", bool)
            field("IndexParameters", array(RdProvidedParameterInfo))
        }

        RdProvidedParameterInfo = structdef {
            field("Name", string.nullable)
            field("IsIn", bool)
            field("IsOut", bool)
            field("IsOptional", bool)
            field("ParameterType", int)
            field("RawDefaultValue", RdStaticArg.nullable)
            field("HasDefaultValue", bool)
            field("CustomAttributes", array(RdCustomAttributeData).nullable)
        }

        RdProvidedMethodInfo = structdef extends RdProvidedMethodBase {
            field("ReturnType", int)
            field("MetadataToken", int)
        }

        field("RdTypeProviderProcessModel", RdTypeProviderProcessModel)
        field("RdProvidedTypeProcessModel", RdProvidedTypeProcessModel)
        field("RdProvidedMethodInfoProcessModel", RdProvidedMethodInfoProcessModel)
        field("RdProvidedConstructorInfoProcessModel", RdProvidedConstructorInfoProcessModel)
        field("RdProvidedAssemblyProcessModel", RdProvidedAssemblyProcessModel)
        field("RdTestHost", RdTestHost)
    }
}
