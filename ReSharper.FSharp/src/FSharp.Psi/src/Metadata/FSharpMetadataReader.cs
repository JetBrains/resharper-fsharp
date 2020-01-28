using System.IO;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.FSharp.Util;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Metadata
{
  public class FSharpMetadataReader : BinaryReader
  {
    public FSharpMetadataReader([NotNull] Stream input, [NotNull] Encoding encoding) : base(input, encoding)
    {
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

      var b20 = (int) ReadByte();
      var b21 = (int) ReadByte();
      var b22 = (int) ReadByte();
      var b23 = (int) ReadByte();
      return b20 | (b21 << 8) | (b22 << 16) | (b23 << 24);
    }

    public static void ReadMetadata(FSharpAssemblyUtil.FSharpSignatureDataResource resource)
    {
      using var resourceReader = resource.MetadataResource.CreateResourceReader();
      var reader = new FSharpMetadataReader(resourceReader, Encoding.UTF8);
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
      var ccuRefNames = ReadCcuRefNames();
      var typeDeclCount = ReadTypeDeclarationsCount(out var hasAnonRecords);
      var typeParameterDeclCount = ReadPackedInt();
      var valueDeclCount = ReadPackedInt();
      var anonRecordDeclCount = hasAnonRecords ? ReadPackedInt() : 0;
      var strings = ReadStringLiterals();
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

    private string[] ReadStringLiterals()
    {
      var stringCount = ReadPackedInt();
      var strings = new string[stringCount];
      for (var i = 0; i < stringCount; i++) 
        strings[i] = ReadString();
      return strings;
    }
    
    private void SkipSeparator()
    {
      var separator = ReadPackedInt();
      Assertion.Assert(separator == 0, "separator == 0");
    }
  }
}
