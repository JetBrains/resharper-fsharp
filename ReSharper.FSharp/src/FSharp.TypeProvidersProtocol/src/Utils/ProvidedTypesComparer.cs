using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using static FSharp.Compiler.ExtensionTyping;

namespace JetBrains.ReSharper.Plugins.FSharp.TypeProvidersProtocol.Utils
{
  public class ProvidedTypesComparer : IEqualityComparer<ProvidedType>
  {
    private ProvidedTypesComparer()
    {
    }

    private static bool GetFullNameFast(ProvidedType x, out string fullName)
    {
      try
      {
        fullName = x.FullName;
        return true;
      }
      catch
      {
        fullName = null;
        return false;
      }
    }

    private static string GetAssemblyNameSafe(ProvidedType x)
    {
      try
      {
        return x.Assembly.FullName;
      }
      catch
      {
        return null;
      }
    }

    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    public bool Equals(ProvidedType x, ProvidedType y)
    {
      var xResult = GetFullNameFast(x, out var xFullName);
      var yResult = GetFullNameFast(y, out var yFullName);

      if (xResult != yResult) return false;
      if (GetAssemblyNameSafe(x) != GetAssemblyNameSafe(y)) return false;
      if (xResult) return xFullName == yFullName;

      if (x.Name != y.Name) return false;
      if (x.Namespace != y.Namespace) return false;

      var xIsGenericType = x.IsGenericType;
      if (xIsGenericType != y.IsGenericType) return false;

      if (xIsGenericType)
      {
        var xGenericArgs = x.GetGenericArguments();
        var yGenericArgs = y.GetGenericArguments();

        if (xGenericArgs.Length != yGenericArgs.Length) return false;

        for (var i = 0; i < xGenericArgs.Length; i++)
          if (!Equals(xGenericArgs[i], yGenericArgs[i]))
            return false;
      }

      return true;
    }

    public int GetHashCode(ProvidedType x) => x.Name.GetHashCode();

    public static readonly ProvidedTypesComparer Instance = new ProvidedTypesComparer();
  }
}
