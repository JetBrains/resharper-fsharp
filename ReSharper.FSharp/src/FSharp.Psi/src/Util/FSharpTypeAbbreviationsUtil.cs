using System.Collections.Generic;
using JetBrains.Metadata.Reader.API;
using JetBrains.Metadata.Reader.Impl;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Util
{
  public static class FSharpTypeAbbreviationsUtil
  {
    // todo: get abbreviations from assemblies
    public static bool TryGetAbbreviations(this IClrTypeName clrTypeName, out string[] names) =>
      AbbreviatedTypes.TryGetValue(clrTypeName, out names);

    /// <summary>
    /// Used during Find Usages to get display name when searching element without having FSharpSymbol element.
    /// This class should be removed when a better approach is introduced. 
    /// </summary>
    public static readonly Dictionary<IClrTypeName, string[]> AbbreviatedTypes =
      new Dictionary<IClrTypeName, string[]>
      {
        // Basic type abbreviations
        {new ClrTypeName("System.Object"), new[] {"obj"}},
        {new ClrTypeName("System.Exception"), new[] {"exn"}},
        {new ClrTypeName("System.IntPtr"), new[] {"nativeint"}},
        {new ClrTypeName("System.UIntPtr"), new[] {"unativeint"}},
        {new ClrTypeName("System.String"), new[] {"string"}},
        {new ClrTypeName("System.Single"), new[] {"float32", "single"}},
        {new ClrTypeName("System.Double"), new[] {"float", "double"}},
        {new ClrTypeName("System.SByte"), new[] {"sbyte", "int8"}},
        {new ClrTypeName("System.Byte"), new[] {"byte", "uint8"}},
        {new ClrTypeName("System.Int16"), new[] {"int16"}},
        {new ClrTypeName("System.UInt16"), new[] {"uint16"}},
        {new ClrTypeName("System.Int32"), new[] {"int", "int32"}},
        {new ClrTypeName("System.UInt32"), new[] {"uint32"}},
        {new ClrTypeName("System.Int64"), new[] {"int64"}},
        {new ClrTypeName("System.UInt64"), new[] {"uint64"}},
        {new ClrTypeName("System.Char"), new[] {"char"}},
        {new ClrTypeName("System.Boolean"), new[] {"bool"}},
        {new ClrTypeName("System.Decimal"), new[] {"decimal"}},

        // Collections and other types
        {new ClrTypeName("Microsoft.FSharp.Collections.FSharpList`1"), new[] {"list"}},
        {new ClrTypeName("Microsoft.FSharp.Collections.FSharpOption`1"), new[] {"option"}},
        {new ClrTypeName("Microsoft.FSharp.Core.FSharpRef`1"), new[] {"ref"}},
      };
  }
}
