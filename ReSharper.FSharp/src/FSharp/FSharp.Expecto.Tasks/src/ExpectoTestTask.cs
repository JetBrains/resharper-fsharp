using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.TestRunner.Abstractions.Objects;

namespace JetBrains.ReSharper.Plugins.FSharp.Expecto.Tasks
{
  [Serializable]
  public sealed class ExpectoTestTask : RemoteTask
  {
    [NotNull] public string[] Path { get; }
    public bool IsGroup { get; }
    [CanBeNull] public string TypeName { get; }
    [CanBeNull] public string SourceFilePath { get; }
    public int Line { get; }

    private ExpectoTestTask(
      [NotNull] string[] path, bool isGroup, [CanBeNull] string typeName, [CanBeNull] string sourceFilePath, int line)
    {
      Path = path;
      IsGroup = isGroup;
      TypeName = typeName;
      SourceFilePath = sourceFilePath;
      Line = line;
    }

    [NotNull]
    public static string GetFullName([NotNull] IEnumerable<string> path) => string.Join(".", path);

    [NotNull]
    public static ExpectoTestTask ToClient([NotNull] string[] path, bool isGroup) => new(path, isGroup, null, null, 0);

    [NotNull]
    public static ExpectoTestTask ToServer(
      [NotNull] string[] path, [CanBeNull] string typeName, [CanBeNull] string sourceFilePath, int line) =>
      new(path, false, typeName, sourceFilePath, line);
  }
}
