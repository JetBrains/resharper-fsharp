#nullable enable

using System.ComponentModel;

using JetBrains.Annotations;

// ReSharper disable CheckNamespace
namespace System.Runtime.CompilerServices
{
  /// <summary>
  /// Reserved to be used by the compiler for tracking metadata.
  /// This class should not be used by developers in source code.
  /// </summary>
  /// <remarks>
  /// TODO: remove this polyfill when the stale NJsonSchema copy of the marker is addressed.
  /// Two assemblies in the standalone build of this plugin declare a public <c>IsExternalInit</c>.
  /// They are <c>JetBrains.NetFxBridge</c> and <c>NJsonSchema</c>.
  /// The Roslyn lookup for a predefined type accepts one candidate only.
  /// Two candidates give CS0518 on every <c>init</c> accessor, and a declaration here wins over both.
  /// The monorepo build hides the collision.
  /// There <c>Forwards.cs</c> puts <c>JetBrains.NetFxBridge</c> behind an extern alias.
  /// </remarks>
  [EditorBrowsable(EditorBrowsableState.Never)]
  [UsedImplicitly]
  internal static class IsExternalInit;
}
