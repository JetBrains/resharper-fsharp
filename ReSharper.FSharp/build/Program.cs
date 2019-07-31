using System;
using JetBrains.Util;

namespace ProcessSandbox
{
  public static class Program
  {
    public static void Main(string[] args)
    {
      foreach (var arg in args)
      {
        var path = FileSystemPath.TryParse(arg);
        if (!path.IsEmpty)
          Console.WriteLine(path.FullPath);
      }
    }
  }
}
