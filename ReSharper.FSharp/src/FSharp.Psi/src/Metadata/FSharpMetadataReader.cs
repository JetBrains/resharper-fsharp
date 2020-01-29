using System;
using System.IO;
using System.Text;
using FSharp.Compiler;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.Util;
using Microsoft.FSharp.Core;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
{
  public class FSharpMetadataReader : BinaryReader
  {
    public FSharpMetadataReader([NotNull] Stream input, [NotNull] Encoding encoding) : base(input, encoding)
    {
    }

    private static readonly Func<FSharpMetadataReader, int> ReadIntFunc = reader => reader.ReadPackedInt();
    private static readonly Func<FSharpMetadataReader, bool> ReadBoolFunc = reader => reader.ReadBoolean();
    private static readonly Func<FSharpMetadataReader, string> ReadStringFunc = reader => reader.ReadString();
    private static readonly Func<FSharpMetadataReader, object> ReadTypeFunc = reader => reader.ReadType();
    private static readonly Func<FSharpMetadataReader, object> ReadIlTypeFunc = reader => reader.ReadIlType();
    private static readonly Func<FSharpMetadataReader, object> ReadExpressionFunc = reader => reader.ReadExpression();
    private static readonly Func<FSharpMetadataReader, object> ReadValueRefFunc = reader => reader.ReadValueRef();
    private static readonly Func<FSharpMetadataReader, Range.range> ReadRangeFunc = reader => reader.ReadRange();

    private static readonly Func<FSharpMetadataReader, string> ReadUniqueStringFunc =
      reader => reader.ReadUniqueString();

    private static readonly Func<bool, object> IgnoreBoolFunc = _ => null;

    private string[] myStrings;
    private int[][] myPublicPaths;

    private Tuple<T1, T2> ReadTuple2<T1, T2>(Func<FSharpMetadataReader, T1> reader1,
      Func<FSharpMetadataReader, T2> reader2)
    {
      var v1 = reader1(this);
      var v2 = reader2(this);
      return Tuple.Create(v1, v2);
    }

    private Tuple<T1, T2, T3> ReadTuple3<T1, T2, T3>(Func<FSharpMetadataReader, T1> reader1,
      Func<FSharpMetadataReader, T2> reader2, Func<FSharpMetadataReader, T3> reader3)
    {
      var v1 = reader1(this);
      var v2 = reader2(this);
      var v3 = reader3(this);
      return Tuple.Create(v1, v2, v3);
    }

    private T[] ReadArray<T>(Func<FSharpMetadataReader, T> reader)
    {
      var arrayLength = ReadPackedInt();
      if (arrayLength == 0)
        return EmptyArray<T>.Instance;

      return ReadArray(reader, arrayLength);
    }

    private T[] ReadArray<T>(Func<FSharpMetadataReader, T> reader, int arrayLength)
    {
      var array = new T[arrayLength];
      for (var i = 0; i < arrayLength; i++)
        array[i] = reader(this);
      return array;
    }

    private void SkipArray(Action<FSharpMetadataReader> reader)
    {
      var arrayLength = ReadPackedInt();
      for (var i = 0; i < arrayLength; i++)
        reader(this);
    }

    private FSharpOption<T> ReadOption<T>(Func<FSharpMetadataReader, T> reader)
    {
      var tag = ReadByte();
      Assertion.Assert(tag <= 1, "ReadOption: tag <= 1, {0}", tag);

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

      Assertion.Assert(b0 == 0xFF, "b0 == 0xFF");
      return ReadInt32();
    }

    public override long ReadInt64()
    {
      var i1 = ReadPackedInt();
      var i2 = ReadPackedInt();
      return i1 | ((long) i2 << 32);
    }

    public static void ReadMetadata(FSharpAssemblyUtil.FSharpSignatureDataResource resource)
    {
      using var resourceReader = resource.MetadataResource.CreateResourceReader();
      using var reader = new FSharpMetadataReader(resourceReader, Encoding.UTF8);
      reader.ReadMetadata();
    }

    public static void ReadMetadata(IPsiModule psiModule)
    {
      var metadataResources = FSharpAssemblyUtil.GetFSharpMetadataResources(psiModule);
      foreach (var metadataResource in metadataResources)
        ReadMetadata(metadataResource);
    }

    private void ReadMetadata()
    {
      // Initial reading inside unpickleObjWithDanglingCcus.

      var ccuRefNames = ReadCcuRefNames();
      var typeDeclCount = ReadTypeDeclarationsCount(out var hasAnonRecords);
      var typeParameterDeclCount = ReadPackedInt();
      var valueDeclCount = ReadPackedInt();
      var anonRecordDeclCount = hasAnonRecords ? ReadPackedInt() : 0;

      myStrings = ReadArray(ReadStringFunc);

      // u_encoded_pubpath
      myPublicPaths = ReadArray(reader => reader.ReadArray(ReadIntFunc));

      // u_encoded_nleref
      ReadArray(reader =>
        reader.ReadTuple2(ReadIntFunc, reader => reader.ReadArray(ReadIntFunc)));

      // u_encoded_simpletyp
      ReadArray(ReadIntFunc);

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
      var index = ReadPackedInt();

      var typeParameters = ReadArray(reader => reader.ReadTypeParameterSpec());
      var logicalName = ReadUniqueString();
      var compiledName = ReadOption(ReadUniqueStringFunc);
      var range = ReadRange();
      var publicPath = ReadOption(reader => reader.ReadPublicPath());
      var accessibility = ReadAccessibility();
      var representationAccessibility = ReadAccessibility();
      var attributes = ReadAttributes();
      var typeRepresentation = ReadTypeRepresentation();
      var typeAbbreviation = ReadOption(ReadTypeFunc);
      var typeAugmentation = ReadTypeAugmentation();
      var xmlDocId = ReadUniqueString(); // Should be empty string.
      var typeKind = ReadTypeKind();
      var typeRepresentationFlag = ReadInt64();
      var compilationPath = ReadOption(reader => reader.ReadCompilationPath());
      var moduleType = ReadModuleType();
      var exceptionRepresentation = ReadExceptionRepresentation();
      var possibleXmlDoc = ReadPossibleXmlDoc();

      return null;
    }

    private object ReadUnionCaseSpec()
    {
      var fields = ReadFieldsTable();
      ;
      var returnType = ReadType();
      var ignoredCaseCompiledName = ReadUniqueString();
      var name = ReadIdent();
      var attributes = ReadAttributesAndXmlDoc();
      var xmlDocId = ReadUniqueString();
      var accessibility = ReadAccessibility();

      return null;
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
      var tag = ReadByte();
      Assertion.Assert(tag <= 4, "ReadTypeObjectModelKind: tag <= 4, {0}", tag);

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

      return null;
    }

    private object ReadCompilationPath()
    {
      ReadIlScopeRef();
      ReadArray(reader =>
        reader.ReadTuple2(
          ReadUniqueStringFunc,
          reader => reader.ReadModuleOrNamespaceKind()));
      return null;
    }

    private object ReadModuleOrNamespaceKind()
    {
      var tag = ReadByte();
      Assertion.Assert(tag <= 2, "tag <= 2, {0}", tag);
      return null;
    }

    private object ReadIlScopeRef()
    {
      var tag = ReadByte();
      Assertion.Assert(tag <= 2, "ReadIlScopeRef: tag <= 2, {0}", tag);
      if (tag == 0)
        return null;

      if (tag == 1)
        ReadIlModuleRef();

      if (tag == 2)
        ReadIlAssemblyRef();

      return null;
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
      var tag = ReadByte();
      Assertion.Assert(tag == 0, "ReadIlAssemblyRef: tag == 0, {0}", tag);

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
      var tag = ReadByte();
      Assertion.Assert(tag <= 2, "ReadIlPublicKey: tag <= 2, {0}", tag);
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

    private object ReadValue()
    {
      var index = ReadPackedInt();

      var logicalName = ReadUniqueString();
      var compiledName = ReadOption(ReadUniqueStringFunc);
      var ranges = ReadOption(reader => reader.ReadTuple2(ReadRangeFunc, ReadRangeFunc));
      var type = ReadType();
      var flags = ReadInt64();
      var memberInfo = ReadOption(reader => reader.ReadMemberInfo());
      var attributes = ReadAttributes();
      var methodRepresentationInfo = ReadOption(reader => reader.ReadValueRepresentationInfo());
      var xmlDocId = ReadUniqueString();
      var accessibility = ReadAccessibility();
      var declaringEntity = ReadParentRef();
      var constValue = ReadOption(reader => reader.ReadConst());
      var xmlDoc = ReadPossibleXmlDoc();

      return logicalName;
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

    private object ReadMemberInfo()
    {
      var apparentEnclosingEntity = ReadTypeRef();
      var memberFlags = ReadMemberFlags();
      var implementedSlotSigs = ReadArray(reader => reader.ReadAbstractSlotSignature());
      var isImplemented = ReadBoolean();

      return null;
    }

    private object ReadMemberFlags()
    {
      var isInstance = ReadBoolean();
      ReadBoolean();
      var isDispatchSlot = ReadBoolean();
      var isOverrideOrExplicitImpl = ReadBoolean();
      var isFinal = ReadBoolean();

      var tag = ReadByte();
      Assertion.Assert(tag <= 4, "ReadByte: tag <= 4, {0}", tag);

      return null;
    }

    private object ReadParentRef()
    {
      var tag = ReadByte();
      Assertion.Assert(tag <= 1, "ReadParentRef: tag <= 1, {0}", tag);
      var parentRef = ReadTypeRef();

      return null;
    }

    private object ReadModuleType()
    {
      // from u_lazy:
      var chunkLength = ReadInt32();

      var otyconsIdx1 = ReadInt32();
      var otyconsIdx2 = ReadInt32();
      var otyparsIdx1 = ReadInt32();
      var otyparsIdx2 = ReadInt32();
      var ovalsIdx1 = ReadInt32();
      var ovalsIdx2 = ReadInt32();

      ReadModuleOrNamespaceKind();
      ReadArray(reader => reader.ReadValue());
      ReadArray(reader => reader.ReadEntitySpec());

      return null;
    }

    private object ReadExceptionRepresentation()
    {
      var tag = ReadByte();
      Assertion.Assert(tag <= 3, "ReadExceptionRepresentation: tag <= 3, {0}", tag);
      if (tag == 0)
        ReadTypeRef();

      if (tag == 1)
        ReadIlTypeRef();

      if (tag == 2)
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
    private Func<bool, object> ReadTypeRepresentation()
    {
      var tag1 = ReadByte();
      Assertion.Assert(tag1 <= 1, "tag2 <= 1, {0}", tag1);

      if (tag1 == 0)
        return IgnoreBoolFunc;

      if (tag1 == 1)
      {
        var tag2 = ReadByte();
        Assertion.Assert(tag2 <= 4, "tag2 <= 4, {0}", tag2);
        if (tag2 == 0)
        {
          ReadFieldsTable();
          return IgnoreBoolFunc;
        }

        if (tag2 == 1)
        {
          ReadArray(reader => reader.ReadUnionCaseSpec());
          return IgnoreBoolFunc;
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
          return IgnoreBoolFunc;
        }

        if (tag2 == 4)
        {
          ReadType();
          return IgnoreBoolFunc;
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
      var tag = ReadByte();
      Assertion.Assert(tag <= 8, "ReadIlType: tag <= 8, {0}", tag);

      if (tag == 0)
        return null;

      if (tag == 1)
      {
        ReadArray(reader =>
          reader.ReadTuple2(
            reader => reader.ReadOption(ReadIntFunc),
            reader => reader.ReadOption(ReadIntFunc)));

        ReadIlType();
      }

      if (tag == 2 || tag == 3)
        ReadIlTypeSpec();

      if (tag == 4 || tag == 5)
        ReadIlType();

      if (tag == 6)
      {
        ReadCallingConvention();
        ReadArray(ReadIlTypeFunc);
        ReadIlType();
      }

      if (tag == 7)
        ReadPackedInt();

      if (tag == 8)
      {
        ReadBoolean();
        ReadIlTypeRef();
        ReadIlType();
      }

      return null;
    }

    private object ReadIlTypeSpec()
    {
      var typeRef = ReadIlTypeRef();
      var substitution = ReadArray(reader => reader.ReadIlType());

      return null;
    }

    private object ReadType()
    {
      var tag = ReadByte();
      Assertion.Assert(tag <= 9, "ReadType: tag <= 9, {0}", tag);

      if (tag == 0)
        ReadArray(ReadTypeFunc);

      if (tag == 1)
        ReadPackedInt();

      if (tag == 2)
      {
        ReadTypeRef();
        ReadArray(ReadTypeFunc);
      }

      if (tag == 3)
      {
        ReadType();
        ReadType();
      }

      if (tag == 4)
        ReadTypeParameterRef();

      if (tag == 5)
      {
        ReadArray(reader => reader.ReadTypeParameterSpec());
        ReadType();
      }

      if (tag == 6)
      {
        throw new NotImplementedException();
      }

      if (tag == 7)
      {
        var unionCase = ReadUnionCaseRef();
        var substitution = ReadArray(ReadTypeFunc);
      }

      if (tag == 8)
        ReadArray(ReadTypeFunc);

      if (tag == 9)
      {
        var anonRecord = ReadAnonRecord();
        var substitution = ReadTypes();
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

    private Range.range ReadRange()
    {
      var filePath = ReadUniqueString();
      var startLine = ReadPackedInt();
      var startColumn = ReadPackedInt();
      var endLine = ReadPackedInt();
      var endColumn = ReadPackedInt();
      return Range.mkRange(filePath, Range.mkPos(startLine, startColumn), Range.mkPos(endLine, endColumn));
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
      var tag = ReadByte();
      Assertion.Assert(tag <= 1, "ReadAttributeKind: tag <= 1, {0}", tag);
      if (tag == 0)
        return ReadIlMethodRef();
      if (tag == 1)
        return ReadValueRef();

      throw new InvalidOperationException();
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
      var substitution = ReadArray(reader => reader.ReadIlType());
      var returnType = ReadIlType();

      return enclosingTypeRef + "." + name;
    }

    private object ReadCallingConvention()
    {
      var tag1 = ReadByte();
      Assertion.Assert(tag1 <= 2, "ReadCallConv: tag1 <= 2, {0}", tag1);
      var tag2 = ReadByte();
      Assertion.Assert(tag2 <= 5, "ReadCallConv: tag1 <= 5, {0}", tag2);
      return null;
    }

    private object ReadValueRef()
    {
      var tag = ReadByte();
      Assertion.Assert(tag <= 1, "ReadValueRef: tag <= 1, {0}", tag);
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
      var tag = ReadByte();
      Assertion.Assert(tag <= 4, "ReadValueRefFlags: tag <= 4, {0}", tag);

      if (tag == 3)
        ReadType();

      return null;
    }

    private object ReadTypeRef()
    {
      var tag = ReadByte();
      Assertion.Assert(tag <= 1, "ReadTypeRef: tag <= 1, {0}", tag);
      var index = ReadPackedInt();

      return null;
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
      var tag = ReadByte();
      Assertion.Assert(tag <= 12, "ReadTypeParameterConstraint: tag <= 12, {0}", tag);

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
      var tag = ReadByte();
      Assertion.Assert(tag <= 5, "ReadMemberConstraintSolution: tag <= 5, {0}", tag);

      if (tag == 0)
      {
        ReadType();
        ReadOption(reader => reader.ReadIlTypeRef());
        ReadIlMethodRef();
        ReadTypes();
      }

      if (tag == 1)
      {
        ReadType();
        ReadValueRef();
        ReadTypes();
      }

      if (tag == 3)
        throw new NotImplementedException();

      if (tag == 4)
      {
        ReadTypes();
        ReadFieldRef();
        ReadBoolean();
      }

      if (tag == 5)
      {
        var anonRecord = ReadAnonRecord();
        var substitution = ReadTypes();
        var fieldIndex = ReadPackedInt();
      }

      throw new NotImplementedException();
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
      var tag = ReadByte();
      Assertion.Assert(tag <= 13, "ReadExpression: tag <= 13, {0}", tag);

      if (tag == 0)
      {
        ReadConst();
        ReadType();

        return null;
      }

      if (tag == 1)
      {
        ReadValueRef();
        ReadValueRefFlags();
      }

      if (tag == 2)
        throw new NotImplementedException();

      if (tag == 3)
      {
        ReadExpression();
        ReadExpression();
        ReadPackedInt();
      }

      if (tag == 4)
      {
        ReadOption(reader => reader.ReadValue());
        ReadOption(reader => reader.ReadValue());
        ReadArray(reader => reader.ReadValue());
        ReadExpression();
        ReadType();
      }

      if (tag == 5)
      {
        ReadArray(reader => reader.ReadTypeParameterSpec());
        ReadExpression();
        ReadType();
      }

      if (tag == 6)
      {
        ReadExpression();
        ReadType();
        ReadTypes();
        ReadArray(reader => reader.ReadExpression());
      }

      if (tag == 7)
      {
        ReadArray(reader => reader.ReadBinding());
        ReadExpression();
      }

      if (tag == 8)
      {
        ReadBinding();
        ReadExpression();
      }

      if (tag == 9)
      {
        throw new NotImplementedException();
      }

      if (tag == 10)
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

      if (tag == 11)
      {
        ReadArray(reader => reader.ReadStaticOptimizationConstraint());
        ReadExpression();
        ReadExpression();
      }

      if (tag == 12)
      {
        ReadArray(reader => reader.ReadTypeParameterSpec());
        ReadExpression();
      }

      if (tag == 13)
      {
        ReadExpression();
        ReadType();
      }

      return null;
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
      var tag = ReadByte();
      Assertion.Assert(tag <= 1, "ReadStaticOptimizationConstraint: tag <= 1, {0}", tag);

      if (tag == 0)
      {
        ReadType();
        ReadType();
      }

      if (tag == 1)
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
      var tag = ReadByte();
      Assertion.Assert(tag <= 17, "ReadConst: tag <= 17, {0}", tag);
      return tag switch
      {
        0 => ReadBoolean(),
        1 => ReadPackedInt(),
        2 => ReadByte(),
        var x when x >= 3 && x <= 6 => ReadPackedInt(),
        var x when x >= 7 && x <= 10 => ReadPackedInt(),
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

      var tag = ReadByte();
      Assertion.Assert(tag <= 1, "ReadPossibleXmlDoc: tag <= 1, {0}", tag);

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

    private object ReadAccessibility()
    {
      return ReadArray(reader => reader.ReadCompilationPath());
    }

    private object ReadTypeKind()
    {
      var tag = ReadByte();
      Assertion.Assert(tag <= 1, "ReadTypeKind: tag <= 1, {0}", tag);
      return null;
    }

    private string[] ReadCcuRefNames()
    {
      var ccuRefNamesCount = ReadPackedInt();
      var names = new string[ccuRefNamesCount];
      for (var i = 0; i < ccuRefNamesCount; i++)
      {
        SkipSeparator();
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

    private void SkipSeparator()
    {
      var separator = ReadPackedInt();
      Assertion.Assert(separator == 0, "SkipSeparator: separator == 0, {0}", separator);
    }

    private void SkipBytes(int bytes)
    {
      for (var i = 0; i < bytes; i++)
      {
        var b = ReadByte();
        Assertion.Assert(b == 0, "SkipBytes: b == 0, {0}", b);
      }
    }

    private void SkipInt()
    {
      ReadPackedInt();
    }
  }
}
