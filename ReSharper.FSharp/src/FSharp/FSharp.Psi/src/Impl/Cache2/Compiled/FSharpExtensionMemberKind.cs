using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi.Impl.Cache2.Compiled;

/// <summary>
/// Represents a kind of F#-specific extension member.
/// </summary>
public sealed class FSharpExtensionMemberKind : ExtensionMemberKind
{
  private FSharpExtensionMemberKind([NotNull] string name) : base(name)
  {
  }

  /// <summary>
  /// F# type extension member.
  /// </summary>
  public static readonly FSharpExtensionMemberKind INSTANCE = new(nameof(INSTANCE));
}
