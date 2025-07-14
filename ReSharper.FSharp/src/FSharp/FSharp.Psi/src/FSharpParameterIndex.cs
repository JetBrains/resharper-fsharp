namespace JetBrains.ReSharper.Plugins.FSharp.Psi;

/// Index of the parameter in F# curried parameter groups.
/// The compiled parameters may be inferred by the compiler, so a semantic parameter may differ from the syntax one.
/// When `ParameterIndex` is `null`, it means it's the only parameter in the group.
/// This is done to help recognizing cases when one syntax parameter produces multiple semantic ones. 
public record struct FSharpParameterIndex(int GroupIndex, int? ParameterIndex)
{
  public static readonly FSharpParameterIndex Zero = new(0, null);
}
