using System.Collections.Generic;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Plugins.FSharp.Common.Util;

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
        {PredefinedType.OBJECT_FQN, new[] {"obj"}},
        {PredefinedType.EXCEPTION_FQN, new[] {"exn"}},
        {PredefinedType.INTPTR_FQN, new[] {"nativeint"}},
        {PredefinedType.UINTPTR_FQN, new[] {"unativeint"}},
        {PredefinedType.STRING_FQN, new[] {"string"}},
        {PredefinedType.FLOAT_FQN, new[] {"float32", "single"}},
        {PredefinedType.DOUBLE_FQN, new[] {"float", "double"}},
        {PredefinedType.SBYTE_FQN, new[] {"sbyte", "int8"}},
        {PredefinedType.BYTE_FQN, new[] {"byte", "uint8"}},
        {PredefinedType.SHORT_FQN, new[] {"int16"}},
        {PredefinedType.USHORT_FQN, new[] {"uint16"}},
        {PredefinedType.INT_FQN, new[] {"int", "int32"}},
        {PredefinedType.UINT_FQN, new[] {"uint32"}},
        {PredefinedType.LONG_FQN, new[] {"int64"}},
        {PredefinedType.ULONG_FQN, new[] {"uint64"}},
        {PredefinedType.CHAR_FQN, new[] {"char"}},
        {PredefinedType.BOOLEAN_FQN, new[] {"bool"}},
        {PredefinedType.DECIMAL_FQN, new[] {"decimal"}},

        // Collections and other types
        {FSharpPredefinedType.FSharpListTypeName, new[] {"list"}},
        {FSharpPredefinedType.FSharpOptionTypeName, new[] {"option"}},
        {FSharpPredefinedType.FSharpRefTypeName, new[] {"ref"}},
      };
  }
}
