using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using FSharp.Compiler.Text;
using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Diagnostics;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Plugins.FSharp.Metadata;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;
using Microsoft.FSharp.Core;
using Range = FSharp.Compiler.Text.Range;

// ReSharper disable UnusedVariable
// ReSharper disable VariableHidesOuterVariable
// ReSharper disable UnusedMethodReturnValue.Local

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
{
  public class FSharpMetadataReader
  {
    public static FSharpMetadata ReadMetadata(IPsiModule psiModule, IMetadataAssembly assembly)
    {
      var metadata = new FSharpMetadata();

      var manifestResources = assembly.GetManifestResources();
      var metadataResources = 
        manifestResources
          .Where(resource => resource.IsFSharpMetadataResource(out _))
          .SelectNotNull(resource => resource.GetDisposition())
          .AsIList();

      if (metadataResources.IsEmpty() && FSharpAssemblyUtil.IsFSharpCore(assembly.AssemblyName))
      {
        using var stream = FSharpAssemblyUtil.GetFSharpCoreSigdataPath(assembly).OpenFileForReading();
        ReadMetadata(stream, metadata, assembly);
      }
      else
      {
        foreach (var metadataResource in metadataResources)
        {
          Interruption.Current.CheckAndThrow();

          var isCompressed =
            metadataResource.ResourceName
              .StartsWith(FSharpAssemblyUtil.CompressedSignatureInfoResourceName, StringComparison.Ordinal);

          using var stream = metadataResource.CreateResourceReader();
          using var decompressed = new MemoryStream();

          var decompressedStream = isCompressed ? new DeflateStream(stream, CompressionMode.Decompress) : stream;
          decompressedStream.CopyTo(decompressed);
          decompressedStream.Close();

          decompressed.Position = 0;
          ReadMetadata(decompressed, metadata, assembly);
        }
      }

      return metadata;
    }

    private static void ReadMetadata(Stream stream, FSharpMetadata metadata, IMetadataAssembly metadataAssembly)
    {
      using var reader = new FSharpMetadataStreamReader(stream, Encoding.UTF8, metadataAssembly) { Metadata = metadata };
      reader.ReadMetadata();
    }
  }

  internal delegate T Reader<out T>(FSharpMetadataStreamReader reader);

  internal class FSharpMetadataStreamReader : BinaryReader
  {
    private readonly IMetadataAssembly myMetadataAssembly;

    public FSharpMetadataStreamReader([NotNull] Stream input, [NotNull] Encoding encoding,
      IMetadataAssembly metadataAssembly) : base(input, encoding)
    {
      myMetadataAssembly = metadataAssembly;
    }

    internal FSharpMetadata Metadata { get; set; }

    private readonly Stack<FSharpMetadataEntity> myState = new();

    private static readonly Reader<int> ReadIntFunc = reader => reader.ReadPackedInt();
    private static readonly Reader<bool> ReadBoolFunc = reader => reader.ReadBoolean();
    private static readonly Reader<string> ReadStringFunc = reader => reader.ReadString();
    private static readonly Reader<object> ReadTypeFunc = reader => reader.ReadType();
    private static readonly Reader<object> ReadIlTypeFunc = reader => reader.ReadIlType();
    private static readonly Reader<object> ReadExpressionFunc = reader => reader.ReadExpression();
    private static readonly Reader<object> ReadValueRefFunc = reader => reader.ReadValueRef();
    private static readonly Reader<Range> ReadRangeFunc = reader => reader.ReadRange();
    private static readonly Reader<string> ReadUniqueStringFunc = reader => reader.ReadUniqueString();

    private string[] myStrings;
    private int[][] myPublicPaths;

    private byte ReadByteTagValue(int maxValue)
    {
      var tag = ReadByte();
      CheckTagValue(tag, maxValue);
      return tag;
    }

    private byte ReadByteTagValue(string tagName, int maxValue)
    {
      var tag = ReadByte();
      CheckTagValue(tagName, tag, maxValue);
      return tag;
    }

    private int ReadPackedIntTagValue(string tagName, int maxValue)
    {
      var tag = ReadPackedInt();
      CheckTagValue(tagName, tag, maxValue);
      return tag;
    }

    [Conditional("JET_MODE_ASSERT")]
    private void CheckTagValue(int value, int maxValue, [CallerMemberName] string methodName = "") =>
      CheckTagValue(value, maxValue, methodName, "tag");

    [Conditional("JET_MODE_ASSERT")]
    private void CheckTagValue(string tagName, int value, int maxValue, [CallerMemberName] string methodName = "") =>
      CheckTagValue(value, maxValue, methodName, tagName);

    [Conditional("JET_MODE_ASSERT")]
    private void CheckTagValue(int value, int maxValue, string methodName, string tagName)
    {
      if (value > maxValue)
        Assertion.Fail($"{methodName}: {tagName} <= {maxValue}, actual: {value}");
    }

    public T ReadByteAsEnum<T>(string tagName = "tag") where T : struct, Enum
    {
      var value = ReadByte();
      // Assertion.Assert(value < Enum.GetValues(typeof(T)).Length, $"{tagName}: {value}");
      return Unsafe.As<byte, T>(ref value);
    }

    private Tuple<T1, T2> ReadTuple2<T1, T2>(Reader<T1> reader1, Reader<T2> reader2)
    {
      var v1 = reader1(this);
      var v2 = reader2(this);
      return Tuple.Create(v1, v2);
    }

    private Tuple<T1, T2, T3> ReadTuple3<T1, T2, T3>(Reader<T1> reader1, Reader<T2> reader2, Reader<T3> reader3)
    {
      var v1 = reader1(this);
      var v2 = reader2(this);
      var v3 = reader3(this);
      return Tuple.Create(v1, v2, v3);
    }

    private T[] ReadArray<T>(Reader<T> reader)
    {
      var arrayLength = ReadPackedInt();
      if (arrayLength == 0)
        return EmptyArray<T>.Instance;

      return ReadArray(reader, arrayLength);
    }

    private T[] ReadArray<T>(Reader<T> reader, int arrayLength)
    {
      var array = new T[arrayLength];
      for (var i = 0; i < arrayLength; i++)
        array[i] = reader(this);
      return array;
    }

    private FSharpOption<T> ReadOption<T>(Reader<T> reader)
    {
      var tag = ReadByteTagValue(1);
      if (tag == 0)
        return FSharpOption<T>.None;

      return reader(this);
    }

    public override string ReadString()
    {
      var stringLength = ReadPackedInt();
      var bytes = ReadBytes(stringLength);
      return Encoding.UTF8.GetString(bytes);
    }

    public int ReadPackedInt()
    {
      var b0 = ReadByte();
      if (b0 <= 0x7F)
        return b0;

      if (b0 <= 0xBF)
      {
        var b10 = b0 & 0x7F;
        var b11 = (int) ReadByte();
        return (b10 << 8) | b11;
      }

      Assertion.Assert(b0 == 0xFF);
      return ReadInt32();
    }

    public override long ReadInt64()
    {
      var i1 = ReadPackedInt();
      var i2 = ReadPackedInt();
      return i1 | ((long) i2 << 32);
    }

    internal void ReadMetadata()
    {
      // Initial reading inside unpickleObjWithDanglingCcus.

      var ccuRefNames = ReadCcuRefNames();
      var typeDeclCount = ReadTypeDeclarationsCount(out var hasAnonRecords);
      Metadata.Entities = new FSharpMetadataEntity[typeDeclCount];
      var typeParameterDeclCount = ReadPackedInt();
      var valueDeclCount = ReadPackedInt();
      var anonRecordDeclCount = hasAnonRecords ? ReadPackedInt() : 0;

      myStrings = ReadArray(ReadStringFunc);

      // u_encoded_pubpath
      myPublicPaths = ReadArray(reader => reader.ReadArray(ReadIntFunc));

      // u_encoded_nleref
      Metadata.NonLocalTypeReferences = 
        ReadArray(reader =>
        {
          var (ccuIndex, typeNameIndices) = reader.ReadTuple2(ReadIntFunc, reader => reader.ReadArray(ReadIntFunc));
          var ccu = ccuRefNames[ccuIndex];
          var typeNames = typeNameIndices.Select(i => myStrings[i]).AsArray();
          return FSharpMetadataTypeReference.NewNonLocal(ccu, typeNames);
        });

      // u_encoded_simpletyp
      Metadata.SimpleTypes = ReadArray(reader =>
      {
        var index = reader.ReadPackedInt();
        return FSharpMetadataType.NewTypeRef(Metadata.NonLocalTypeReferences[index]);
      });

      // FCS reads chunk of bytes into a separate object and reads data from it.
      // The chunk size is encoded as int, which we skip here.
      ReadPackedInt();

      ReadCompilationUnit();
      var compileTimeWorkingDir = ReadUniqueString();
      var usesQuotations = ReadBoolean();
      SkipBytes(3);
    }

    private void ReadCompilationUnit()
    {
      // Reading inside unpickleCcuInfo passed as an argument to unpickleObjWithDanglingCcus.

      ReadEntitySpec();
    }

    private object ReadEntitySpec()
    {
      Interruption.Current.CheckAndThrow();

      var index = ReadPackedInt();
      var typeParameters = ReadArray(reader => reader.ReadTypeParameterSpec());
      var logicalName = ReadUniqueString();
      var compiledName = ReadOption(ReadUniqueStringFunc);
      var range = ReadRange();
      var publicPath = ReadOption(reader => reader.ReadPublicPath());
      var accessibility = ReadAccessibility();
      var representationAccessibility = ReadAccessibility();
      var attributes = ReadAttributes();
      var typeRepresentationFunc = ReadTypeRepresentation();
      var typeAbbreviation = ReadOption(ReadTypeFunc);
      var typeAugmentation = ReadTypeAugmentation();
      var xmlDocId = ReadUniqueString(); // Should be an empty string.
      var typeKind = ReadTypeKind();
      var flags = (EntityFlags) ReadInt64();

      var entityFlags = flags & ~EntityFlags.ReservedBit;
      var isModuleOrNamespace = (entityFlags & EntityFlags.IsModuleOrNamespace) != 0;
      var reprIsProvidedIlType = flags & EntityFlags.ReservedBit;

      var compilationPathWithScopeRef = ReadOption(reader => reader.ReadCompilationPath());
      var compilationPath = compilationPathWithScopeRef?.Value is var (_, path) ? path : [];

      var accessRights = GetAccessRights(accessibility);

      var entity = FSharpMetadataEntityModule.create(index, logicalName, compiledName, typeParameters.Length, compilationPath, accessRights);
      myState.Push(entity);
      Metadata.Entities[index] = entity;

      var metadataValues = ReadModuleType(entity);
      var exceptionRepresentation = ReadExceptionRepresentation();
      var possibleXmlDoc = ReadPossibleXmlDoc();

      var typeRepresentation = typeRepresentationFunc(reprIsProvidedIlType != 0);

      if (isModuleOrNamespace)
      {
        if (entity.EntityKind != EntityKind.Namespace)
        {
          var nameKind = GetModuleNameKind(entity.EntityKind, range);
          entity.Representation = FSharpCompiledTypeRepresentation.NewModule(nameKind, metadataValues);
          Metadata.AddEntity(entity, myMetadataAssembly);
        }
      }
      else
      {
        if (typeAbbreviation == null)
        {
          entity.Representation = typeRepresentation;
          Metadata.AddEntity(entity, myMetadataAssembly);
        }
      }

      myState.Pop();

      return null;
    }

    private static AccessRights GetAccessRights((bool, Tuple<string, EntityKind>[])[] accessibility)
    {
      if (accessibility.Length == 0)
        return AccessRights.PUBLIC;

      var (_, privateOwnerPath) = accessibility[0];
      return privateOwnerPath.IsEmpty() ? AccessRights.INTERNAL : AccessRights.PRIVATE;
    }

    private FSharpMetadataModuleNameKind GetModuleNameKind(EntityKind entityKind, Range range)
    {
      if (entityKind == EntityKind.ModuleWithSuffix)
        return FSharpMetadataModuleNameKind.HasModuleSuffix;

      var isAnonModule =
        PositionModule.posEq(range.Start, PositionModule.pos0) &&
        PositionModule.posEq(range.End, PositionModule.pos0);

      return isAnonModule ? FSharpMetadataModuleNameKind.Anon : FSharpMetadataModuleNameKind.Normal;
    }

    private string ReadUnionCaseSpec()
    {
      var fields = ReadFieldsTable();
      var returnType = ReadType();
      var ignoredCaseCompiledName = ReadUniqueString();
      var name = ReadIdent();
      var attributes = ReadAttributesAndXmlDoc();
      var xmlDocId = ReadUniqueString();
      var accessibility = ReadAccessibility();

      return name;
    }

    private object ReadTypeObjectModelData()
    {
      ReadTypeObjectModelKind();
      ReadArray(ReadValueRefFunc);
      ReadFieldsTable();

      return null;
    }

    private object ReadTypeObjectModelKind()
    {
      var tag = ReadByteTagValue(4);

      if (tag == 3)
        ReadAbstractSlotSignature();

      return null;
    }

    private object ReadAbstractSlotSignature()
    {
      var name = ReadUniqueString();
      var containingType = ReadType();
      var containingTypeTypeParameters = ReadArray(reader => reader.ReadTypeParameterSpec());
      var typeParameters = ReadArray(reader => reader.ReadTypeParameterSpec());
      var parameters = ReadArray(reader => reader.ReadArray(reader => reader.ReadAbstractSlotParameter()));
      var returnType = ReadOption(ReadTypeFunc);

      return name;
    }

    private object ReadAbstractSlotParameter()
    {
      var name = ReadOption(ReadUniqueStringFunc);
      var type = ReadType();
      var isIn = ReadBoolean();
      var isOut = ReadBoolean();
      var isOptional = ReadBoolean();
      var attributes = ReadAttributes();

      return name;
    }

    private (bool, Tuple<string, EntityKind>[]) ReadCompilationPath()
    {
      var isInternal = ReadIlScopeRef();
      var accessPath = ReadArray(reader =>
        reader.ReadTuple2(
          ReadUniqueStringFunc,
          reader => reader.ReadEntityKind()));
      return (isInternal, accessPath);
    }

    private EntityKind ReadEntityKind() =>
      ReadByteAsEnum<EntityKind>();

    private bool ReadIlScopeRef()
    {
      var tag = ReadByteTagValue(2);
      if (tag == 0)
        return true;

      if (tag == 1)
        ReadIlModuleRef();

      else if (tag == 2)
        ReadIlAssemblyRef();

      return false;
    }

    private object ReadIlModuleRef()
    {
      var name = ReadUniqueString();
      var hasMetadata = ReadBoolean();
      var hash = ReadOption(reader => reader.ReadBytes());

      return name;
    }

    private object ReadIlAssemblyRef()
    {
      ReadByteTagValue(0);

      var name = ReadUniqueString();
      var hash = ReadOption(reader => reader.ReadBytes());
      var publicKey = ReadOption(reader => reader.ReadIlPublicKey());
      var retargetable = ReadBoolean();

      var version = ReadOption(reader => reader.ReadIlVersion());
      var locale = ReadOption(ReadUniqueStringFunc);

      return name;
    }

    private object ReadIlPublicKey()
    {
      ReadByteTagValue(2);
      ReadBytes();
      return null;
    }

    private object ReadIlVersion()
    {
      var major = ReadPackedInt();
      var minor = ReadPackedInt();
      var build = ReadPackedInt();
      var revision = ReadPackedInt();

      return new Version(major, minor, build, revision);
    }

    private object ReadBytes()
    {
      var bytes = ReadPackedInt();
      ReadBytes(bytes);

      return null;
    }

    private FSharpMetadataValue ReadValue()
    {
      var index = ReadPackedInt();

      var logicalName = ReadUniqueString();
      var compiledName = ReadOption(ReadUniqueStringFunc);
      var ranges = ReadOption(reader => reader.ReadTuple2(ReadRangeFunc, ReadRangeFunc));
      var type = ReadType();
      var flags = (ValueFlags) ReadInt64();
      var memberInfo = ReadOption(reader => reader.ReadMemberInfo());
      var attributes = ReadAttributes();
      var methodRepresentationInfo = ReadOption(reader => reader.ReadValueRepresentationInfo());
      var xmlDocId = ReadUniqueString();
      var accessibility = ReadAccessibility();
      var declaringEntity = ReadParentRef();
      var constValue = ReadOption(reader => reader.ReadConst());
      var xmlDoc = ReadPossibleXmlDoc();

      var isPublic = accessibility.Length == 0;
      var isLiteral = constValue != null;
      var isFunction = type?.IsFunction ?? false;
      var isExtensionMember = (flags & ValueFlags.IsExtensionMember) != 0;

      var apparentEnclosingEntity = memberInfo?.Value.ApparentEnclosingEntity;
      return new FSharpMetadataValue(logicalName, compiledName, isExtensionMember, apparentEnclosingEntity, isPublic,
        isLiteral, isFunction);
    }

    private object ReadValueRepresentationInfo()
    {
      // Values compiled as methods infos

      var typeParameters = ReadArray(reader => reader.ReadTypeParameterRepresentationInfo());
      var parameters = ReadArray(reader => reader.ReadArray(reader => reader.ReadArgumentRepresentationInfo()));
      var result = ReadArgumentRepresentationInfo();

      return null;
    }

    private object ReadTypeParameterRepresentationInfo()
    {
      var name = ReadIdent();
      var kind = ReadTypeKind();
      return name;
    }

    private object ReadArgumentRepresentationInfo()
    {
      ReadAttributes();
      ReadOption(reader => reader.ReadIdent());

      return null;
    }

    private FSharpMetadataMemberInfo ReadMemberInfo()
    {
      var apparentEnclosingEntity = ReadTypeRef();
      var memberFlags = ReadMemberFlags();
      var implementedSlotSigs = ReadArray(reader => reader.ReadAbstractSlotSignature());
      var isImplemented = ReadBoolean();

      return new FSharpMetadataMemberInfo(apparentEnclosingEntity);
    }

    private object ReadMemberFlags()
    {
      var isInstance = ReadBoolean();
      ReadBoolean();
      var isDispatchSlot = ReadBoolean();
      var isOverrideOrExplicitImpl = ReadBoolean();
      var isFinal = ReadBoolean();

      ReadByteTagValue(4);

      return null;
    }

    private object ReadParentRef()
    {
      var tag = ReadByteTagValue(1);
      if (tag == 1)
        ReadTypeRef();

      return null;
    }

    private FSharpMetadataValue[] ReadModuleType(FSharpMetadataEntity metadataEntity)
    {
      // from u_lazy:
      var chunkLength = ReadInt32();

      var otyconsIdx1 = ReadInt32();
      var otyconsIdx2 = ReadInt32();
      var otyparsIdx1 = ReadInt32();
      var otyparsIdx2 = ReadInt32();
      var ovalsIdx1 = ReadInt32();
      var ovalsIdx2 = ReadInt32();

      metadataEntity.EntityKind = ReadEntityKind();

      var metadataValues = ReadArray(reader => reader.ReadValue());
      ReadArray(reader => reader.ReadEntitySpec());

      return metadataValues;
    }

    private object ReadExceptionRepresentation()
    {
      var tag = ReadByteTagValue(3);
      if (tag == 0)
        ReadTypeRef();

      else if (tag == 1)
        ReadIlTypeRef();

      else if (tag == 2)
        ReadFieldsTable();

      return null;
    }

    private object ReadTypeParameterSpec()
    {
      var index = ReadPackedInt();

      var ident = ReadIdent();
      var attributes = ReadAttributes();
      var flags = ReadInt64();
      var constraints = ReadArray(reader => reader.ReadTypeParameterConstraint());
      var xmlDoc = ReadXmlDoc();

      return null;
    }

    [NotNull]
    private Func<bool, FSharpCompiledTypeRepresentation> ReadTypeRepresentation()
    {
      var tag1 = ReadByteTagValue("tag1", 1);
      if (tag1 == 0)
        return _ => FSharpCompiledTypeRepresentation.Other;

      if (tag1 == 1)
      {
        var tag2 = ReadByteTagValue("tag2", 4);
        if (tag2 == 0)
        {
          ReadFieldsTable();
          return _ => FSharpCompiledTypeRepresentation.Other;
        }

        if (tag2 == 1)
        {
          var caseNames = ReadArray(reader => reader.ReadUnionCaseSpec());
          return _ => FSharpCompiledTypeRepresentation.NewUnion(caseNames);
        }

        if (tag2 == 2)
        {
          ReadIlType();
          return flag =>
          {
            if (!flag)
              return null;

            return null;
          };
        }

        if (tag2 == 3)
        {
          ReadTypeObjectModelData();
          return _ => FSharpCompiledTypeRepresentation.Other;
        }

        if (tag2 == 4)
        {
          ReadType();
          return _ => FSharpCompiledTypeRepresentation.Other;
        }

        throw new InvalidOperationException();
      }

      throw new InvalidOperationException();
    }

    private object ReadFieldsTable()
    {
      return ReadArray(reader => reader.ReadRecordFieldSpec());
    }

    private object ReadRecordFieldSpec()
    {
      var isMutable = ReadBoolean();
      var isVolatile = ReadBoolean();
      var type = ReadType();
      var isStatic = ReadBoolean();
      var isCompilerGenerated = ReadBoolean();
      var constantValue = ReadOption(reader => reader.ReadConst());
      var name = ReadIdent();
      var propertyAttributes = ReadAttributesAndXmlDoc();
      var fieldAttributes = ReadAttributes();
      var xmlDocId = ReadUniqueString();
      var accessibility = ReadAccessibility();

      return name;
    }

    private object ReadAttributesAndXmlDoc()
    {
      var b = ReadPackedInt();
      if ((b & 0x80000000) == 0x80000000)
        ReadXmlDoc();

      return ReadArray(reader => reader.ReadAttribute(), b & 0x7FFFFFFF);
    }

    private string ReadUniqueString()
    {
      var index = ReadPackedInt();
      var encodedString = myStrings[index];
      return encodedString;
    }

    private object ReadIlType()
    {
      var tag = ReadByteTagValue(8);
      if (tag == 0)
        return null;

      if (tag == 1)
      {
        ReadIlArrayShape();
        ReadIlType();
      }

      else if (tag is 2 or 3)
        ReadIlTypeSpec();

      else if (tag is 4 or 5)
        ReadIlType();

      else if (tag == 6)
      {
        ReadCallingConvention();
        ReadArray(ReadIlTypeFunc);
        ReadIlType();
      }

      else if (tag == 7)
        ReadPackedInt();

      else if (tag == 8)
      {
        ReadBoolean();
        ReadIlTypeRef();
        ReadIlType();
      }

      return null;
    }

    private object ReadIlArrayShape()
    {
      return ReadArray(reader =>
        reader.ReadTuple2(
          reader => reader.ReadOption(ReadIntFunc),
          reader => reader.ReadOption(ReadIntFunc)));
    }

    private object ReadIlTypes()
    {
      return ReadArray(ReadIlTypeFunc);
    }

    private object ReadIlTypeSpec()
    {
      var typeRef = ReadIlTypeRef();
      var substitution = ReadIlTypes();

      return null;
    }

    private FSharpMetadataType ReadType()
    {
      var tag = ReadByteTagValue(9);
      if (tag == 0)
        ReadArray(ReadTypeFunc);

      else if (tag == 1)
      {
        var simpleTypeIndex = ReadPackedInt();
        return simpleTypeIndex < Metadata.SimpleTypes.Length ? Metadata.SimpleTypes[simpleTypeIndex] : null;
      }

      else if (tag == 2)
      {
        ReadTypeRef();
        ReadArray(ReadTypeFunc);
      }

      else if (tag == 3)
      {
        ReadType();
        ReadType();
        return FSharpMetadataType.Function;
      }

      else if (tag == 4)
        ReadTypeParameterRef();

      else if (tag == 5)
      {
        ReadArray(reader => reader.ReadTypeParameterSpec());
        return ReadType();
      }

      else if (tag == 6)
        ReadMeasureExpression();

      else if (tag == 7)
      {
        var unionCase = ReadUnionCaseRef();
        var substitution = ReadArray(ReadTypeFunc);
      }

      else if (tag == 8)
        ReadArray(ReadTypeFunc);

      else if (tag == 9)
      {
        var anonRecord = ReadAnonRecord();
        var substitution = ReadTypes();
      }

      return null;
    }

    private object ReadMeasureExpression()
    {
      var tag = ReadByteTagValue(5);
      if (tag == 0)
        ReadTypeRef();

      else if (tag == 1)
        ReadMeasureExpression();

      else if (tag == 2)
      {
        ReadMeasureExpression();
        ReadMeasureExpression();
      }

      else if (tag == 3)
        ReadTypeParameterRef();

      else if (tag == 5)
      {
        ReadMeasureExpression();
        ReadPackedInt();
        ReadPackedInt();
      }

      return null;
    }

    private object ReadTypes()
    {
      return ReadArray(reader => reader.ReadType());
    }

    private object ReadTypeAugmentation()
    {
      ReadOption(reader => reader.ReadTuple2(ReadValueRefFunc, ReadValueRefFunc));
      ReadOption(ReadValueRefFunc);
      ReadOption(reader => reader.ReadTuple3(ReadValueRefFunc, ReadValueRefFunc, ReadValueRefFunc));
      ReadOption(reader => reader.ReadTuple2(ReadValueRefFunc, ReadValueRefFunc));
      ReadArray(reader => reader.ReadTuple2(ReadUniqueStringFunc, ReadValueRefFunc));
      ReadArray(reader => reader.ReadTuple2(ReadTypeFunc, ReadBoolFunc));
      ReadOption(ReadTypeFunc);
      ReadBoolean();
      SkipBytes(1);

      return null;
    }

    private string ReadIdent()
    {
      var name = ReadUniqueString();
      var range = ReadRange();
      return name;
    }

    private Range ReadRange()
    {
      var filePath = ReadUniqueString();
      var startLine = ReadPackedInt();
      var startColumn = ReadPackedInt();
      var endLine = ReadPackedInt();
      var endColumn = ReadPackedInt();
      return RangeModule.mkRange(filePath, PositionModule.mkPos(startLine, startColumn), PositionModule.mkPos(endLine, endColumn));
    }

    private object[] ReadAttributes()
    {
      return ReadArray(reader => reader.ReadAttribute());
    }

    private object ReadAttribute()
    {
      var typeRef = ReadTypeRef();
      var attributeKind = ReadAttributeKind();
      var positionalArgs = ReadArray(reader => reader.ReadAttributeExpression());
      var namedArgs = ReadArray(reader => reader.ReadAttributeNamedArg());
      var appliedToGetterOrSetter = ReadBoolean();
      return attributeKind;
    }

    private object ReadAttributeKind()
    {
      var tag = ReadByteTagValue(1);
      return tag switch
      {
        0 => ReadIlMethodRef(),
        1 => ReadValueRef(),
        _ => throw new InvalidOperationException()
      };
    }

    private object ReadAttributeExpression()
    {
      var source = ReadExpression();
      var evaluated = ReadExpression();
      return null;
    }

    private object ReadAttributeNamedArg()
    {
      var name = ReadUniqueString();
      var type = ReadType();
      var isField = ReadBoolean();
      var value = ReadAttributeExpression();
      return null;
    }

    private object ReadIlMethodRef()
    {
      var enclosingTypeRef = ReadIlTypeRef();
      var callingConvention = ReadCallingConvention();
      var typeParametersCount = ReadPackedInt();
      var name = ReadUniqueString();
      var substitution = ReadIlTypes();
      var returnType = ReadIlType();

      return enclosingTypeRef + "." + name;
    }

    private object ReadCallingConvention()
    {
      var tag1 = ReadByteTagValue("tag1", 2);
      var tag2 = ReadByteTagValue("tag2", 5);
      return null;
    }

    private object ReadValueRef()
    {
      var tag = ReadByteTagValue(1);
      if (tag == 0)
      {
        var index = ReadPackedInt();
        return null;
      }

      if (tag == 1)
      {
        var enclosingEntity = ReadTypeRef();
        var parentMangledNameOption = ReadOption(ReadUniqueStringFunc);
        var isOverride = ReadBoolean();
        var logicalName = ReadUniqueString();
        var typeParametersCount = ReadPackedInt();
        var typeForLinkage = ReadOption(reader => reader.ReadType());

        return logicalName;
      }

      throw new InvalidOperationException();
    }

    private object ReadValueRefFlags()
    {
      var tag = ReadByteTagValue(4);
      if (tag == 3)
        ReadType();

      return null;
    }

    private FSharpMetadataTypeReference ReadTypeRef()
    {
      var tag = ReadByteTagValue(1);
      var index = ReadPackedInt();

      return tag == 0
        ? FSharpMetadataTypeReference.NewLocal(index)
        : Metadata.NonLocalTypeReferences[index];
    }

    private object ReadTypeParameterRef()
    {
      var index = ReadPackedInt();

      return null;
    }

    private object ReadUnionCaseRef()
    {
      var unionType = ReadTypeRef();
      var caseName = ReadUniqueString();
      return null;
    }

    private object ReadIlTypeRef()
    {
      var scope = ReadIlScopeRef();
      var enclosingTypes = ReadArray(ReadUniqueStringFunc);
      var name = ReadUniqueString();

      return name;
    }

    private object ReadTypeParameterConstraint()
    {
      var tag = ReadByteTagValue(12);

      if (tag == 0)
        ReadType();

      if (tag == 1)
        ReadMemberConstraint();

      if (tag == 2)
        ReadType();

      if (tag == 7)
        ReadTypes();

      if (tag == 8)
        ReadType();

      if (tag == 9)
      {
        ReadType();
        ReadType();
      }

      return null;
    }

    private object ReadMemberConstraint()
    {
      var types = ReadTypes();
      var name = ReadUniqueString();
      var memberFlags = ReadMemberFlags();
      var substitution = ReadTypes();
      var returnType = ReadOption(ReadTypeFunc);
      var solution = ReadOption(reader => reader.ReadMemberConstraintSolution());

      return name;
    }

    private object ReadMemberConstraintSolution()
    {
      var tag = ReadByteTagValue(5);
      if (tag == 0)
      {
        ReadType();
        ReadOption(reader => reader.ReadIlTypeRef());
        ReadIlMethodRef();
        ReadTypes();
      }

      else if (tag == 1)
      {
        ReadType();
        ReadValueRef();
        ReadTypes();
      }

      else if (tag == 3)
        ReadExpression();

      else if (tag == 4)
      {
        ReadTypes();
        ReadFieldRef();
        ReadBoolean();
      }

      else if (tag == 5)
      {
        var anonRecord = ReadAnonRecord();
        var substitution = ReadTypes();
        var fieldIndex = ReadPackedInt();
      }

      return null;
    }

    private object ReadFieldRef()
    {
      var typeRef = ReadTypeRef();
      var name = ReadUniqueString();

      return name;
    }

    private object ReadAnonRecord()
    {
      var index = ReadPackedInt();

      var ccuRefIndex = ReadPackedInt();
      ReadBoolean();
      var fieldNames = ReadArray(reader => reader.ReadIdent());

      return fieldNames;
    }

    private object ReadExpression()
    {
      var tag = ReadByteTagValue(13);
      if (tag == 0)
      {
        ReadConst();
        ReadType();
      }

      else if (tag == 1)
      {
        ReadValueRef();
        ReadValueRefFlags();
      }

      else if (tag == 2)
      {
        ReadOperation();
        ReadTypes();
        ReadArray(ReadExpressionFunc);
      }

      else if (tag == 3)
      {
        ReadExpression();
        ReadExpression();
        ReadPackedInt();
      }

      else if (tag == 4)
      {
        ReadOption(reader => reader.ReadValue());
        ReadOption(reader => reader.ReadValue());
        ReadArray(reader => reader.ReadValue());
        ReadExpression();
        ReadType();
      }

      else if (tag == 5)
      {
        ReadArray(reader => reader.ReadTypeParameterSpec());
        ReadExpression();
        ReadType();
      }

      else if (tag == 6)
      {
        ReadExpression();
        ReadType();
        ReadTypes();
        ReadArray(ReadExpressionFunc);
      }

      else if (tag == 7)
      {
        ReadArray(reader => reader.ReadBinding());
        ReadExpression();
      }

      else if (tag == 8)
      {
        ReadBinding();
        ReadExpression();
      }

      else if (tag == 9)
      {
        ReadDecisionTree();
        ReadArray(reader => reader.ReadTuple2(reader => reader.ReadValue(), ReadExpressionFunc));
        ReadType();
      }

      else if (tag == 10)
      {
        ReadType();
        ReadOption(reader => reader.ReadValue());
        ReadExpression();
        ReadArray(reader => reader.ReadObjectExpressionMethod());
        ReadArray(reader =>
          reader.ReadTuple2(
            ReadTypeFunc,
            reader => reader.ReadArray(reader => reader.ReadObjectExpressionMethod())));
      }

      else if (tag == 11)
      {
        ReadArray(reader => reader.ReadStaticOptimizationConstraint());
        ReadExpression();
        ReadExpression();
      }

      else if (tag == 12)
      {
        ReadArray(reader => reader.ReadTypeParameterSpec());
        ReadExpression();
      }

      else if (tag == 13)
      {
        ReadExpression();
        ReadType();
      }

      return null;
    }

    private object ReadDecisionTree()
    {
      var tag = ReadByteTagValue(2);
      if (tag == 0)
      {
        ReadExpression();
        ReadArray(reader => reader.DecisionTreeCase());
        ReadOption(reader => reader.ReadDecisionTree());
      }

      else if (tag == 1)
      {
        ReadArray(ReadExpressionFunc);
        ReadPackedInt();
      }

      else if (tag == 3)
      {
        ReadType();
        ReadType();
      }

      else if (tag == 4)
      {
        ReadPackedInt();
        ReadType();
      }

      return null;
    }

    private object DecisionTreeCase()
    {
      ReadDecisionTreeDiscriminator();
      ReadDecisionTree();

      return null;
    }

    private object ReadDecisionTreeDiscriminator()
    {
      var tag = ReadByteTagValue(4);
      if (tag == 0)
      {
        ReadUnionCaseRef();
        ReadTypes();
      }

      else if (tag == 1)
        ReadConst();

      else if (tag == 3)
      {
        ReadType();
        ReadType();
      }

      else if (tag == 4)
      {
        ReadPackedInt();
        ReadType();
      }

      return null;
    }

    private object ReadOperation()
    {
      var tag = ReadByteTagValue(32);
      if (tag == 0)
        ReadUnionCaseRef();

      else if (tag is 1 or 3)
        ReadTypeRef();

      else if (tag is 4 or 5)
        ReadFieldRef();

      else if (tag == 6)
        ReadTypeRef();

      else if (tag is 7 or 8)
      {
        ReadUnionCaseRef();
        ReadPackedInt();
      }

      else if (tag is 9 or 10)
      {
        ReadTypeRef();
        ReadPackedInt();
      }

      else if (tag == 11)
        ReadPackedInt();

      else if (tag == 12)
      {
        ReadArray(reader => reader.ReadIlInstruction());
        ReadTypes();
      }

      else if (tag == 14)
        ReadUnionCaseRef();

      else if (tag == 16)
        ReadMemberConstraint();

      else if (tag == 17)
      {
        ReadByteTagValue("tag17", 3);
        ReadValueRef();
      }

      else if (tag == 18)
      {
        ReadBoolean();
        ReadBoolean();
        ReadBoolean();
        ReadBoolean();
        ReadValueRefFlags();
        ReadBoolean();
        ReadBoolean();
        ReadIlMethodRef();
        ReadTypes();
        ReadTypes();
        ReadTypes();
      }

      else if (tag == 21)
        ReadPackedInt();

      else if (tag == 22)
        ReadBytes();

      else if (tag == 25)
        ReadFieldRef();

      else if (tag == 26)
        ReadArray(ReadIntFunc);

      else if (tag == 28)
      {
        ReadUnionCaseRef();
        ReadPackedInt();
      }

      else if (tag == 30)
        ReadPackedInt();

      else if (tag == 31)
        ReadAnonRecord();

      else if (tag == 32)
      {
        ReadAnonRecord();
        ReadPackedInt();
      }

      return null;
    }

    private object ReadIlInstruction()
    {
      var tag = ReadByteTagValue(66);
      if (tag == 1)
        ReadPackedInt();

      else if (tag is 4 or 24 or 55)
      {
        ReadIlMethodRef();
        ReadIlType();
        ReadIlTypes();
      }

      else if (tag is 20 or 22 or 23)
        ReadPackedIntTagValue("basicTypeTag", 13);

      if (tag == 31 || tag == 33 | tag == 34 || tag == 36)
      {
        ReadPackedIntTagValue("volatilityTag", 1);
        ReadIlFieldSpec();
      }

      if (tag == 32 || tag == 35)
        ReadIlFieldSpec();

      if (tag == 43 || tag == 38 || tag == 29 || tag == 61 || tag == 27 || tag == 28 || tag == 25 || tag == 37 ||
          tag == 58 || tag == 3 || tag == 63 || tag == 65)
        ReadIlType();

      if (tag == 26)
        ReadUniqueString();

      if (tag == 39 || tag == 60 || tag == 59)
      {
        ReadIlArrayShape();
        ReadIlType();
      }

      if (tag == 41)
      {
        ReadPackedIntTagValue("readonlyTag", 1);
        ReadIlArrayShape();
        ReadIlType();
      }

      if (tag == 62)
      {
        ReadPackedInt();
        ReadPackedInt();
      }

      return null;
    }

    private object ReadIlFieldSpec()
    {
      var fieldRef = ReadIlFieldRef();
      var containingType = ReadIlType();

      return null;
    }

    private object ReadIlFieldRef()
    {
      ReadIlTypeRef();
      var name = ReadUniqueString();
      ReadIlType();

      return name;
    }

    private object ReadObjectExpressionMethod()
    {
      var signature = ReadAbstractSlotSignature();
      ReadAttributes();
      ReadArray(reader => reader.ReadTypeParameterSpec());
      ReadArray(reader => reader.ReadValue());
      ReadExpression();

      return signature;
    }

    private object ReadStaticOptimizationConstraint()
    {
      var tag = ReadByteTagValue(1);
      if (tag == 0)
      {
        ReadType();
        ReadType();
      }

      else if (tag == 1)
        ReadType();

      return null;
    }

    private object ReadBinding()
    {
      ReadValue();
      ReadExpression();

      return null;
    }

    private object ReadConst()
    {
      var tag = ReadByteTagValue(17);
      return tag switch
      {
        0 => ReadBoolean(),
        1 => ReadPackedInt(),
        2 => ReadByte(),
        <= 6 => ReadPackedInt(),
        <= 10 => ReadInt64(),
        11 => ReadPackedInt(),
        12 => ReadInt64(),
        13 => ReadPackedInt(),
        14 => ReadUniqueString(),
        17 => ReadArray(ReadIntFunc),
        _ => null
      };
    }

    private string[] ReadXmlDoc()
    {
      return ReadArray(ReadUniqueStringFunc);
    }

    private object ReadPossibleXmlDoc()
    {
      // Converted from
      // u_used_space1 u_xmldoc

      var tag = ReadByteTagValue(1);
      if (tag == 0)
        return null;

      if (tag == 1)
      {
        ReadXmlDoc();
        SkipBytes(1);
        return null;
      }

      return null;
    }

    private object ReadPublicPath()
    {
      var index = ReadPackedInt();

      var pathPartIndexes = myPublicPaths[index];
      var path = new string[pathPartIndexes.Length];
      for (var i = 0; i < path.Length; i++)
        path[i] = myStrings[pathPartIndexes[i]];

      return path;
    }

    private (bool, Tuple<string, EntityKind>[])[] ReadAccessibility()
    {
      return ReadArray(reader => reader.ReadCompilationPath());
    }

    private object ReadTypeKind()
    {
      ReadByteTagValue(1);
      return null;
    }

    private string[] ReadCcuRefNames()
    {
      var ccuRefNamesCount = ReadPackedInt();
      var names = new string[ccuRefNamesCount];
      for (var i = 0; i < ccuRefNamesCount; i++)
      {
        ReadPackedIntTagValue("separator", 0);
        names[i] = ReadString();
      }
      return names;
    }

    private int ReadTypeDeclarationsCount(out bool hasAnonRecords)
    {
      var encodedTypeDeclsNumber = ReadPackedInt();
      hasAnonRecords = encodedTypeDeclsNumber < 0;
      return hasAnonRecords
        ? -encodedTypeDeclsNumber - 1
        : encodedTypeDeclsNumber;
    }

    private void SkipBytes(int bytes)
    {
      for (var i = 0; i < bytes; i++)
      {
        ReadByteTagValue(0);
      }
    }
  }
}
