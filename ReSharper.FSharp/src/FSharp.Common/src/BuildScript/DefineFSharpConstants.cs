using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.BuildScript.PreCompile.Autofix;
using JetBrains.Application.BuildScript.Solution;
using JetBrains.Build;

namespace JetBrains.ReSharper.Plugins.FSharp.BuildScript
{
	public static class DefineFSharpConstants
	{
		[BuildStep]
		public static IEnumerable<AutofixAllowedDefineConstant> YieldAllowedDefineConstantsForFSharp()
		{
			var constants = new List<string>();

			constants.AddRange(new[] {"$(DefineConstants)", "NETFRAMEWORK"});

			return constants.SelectMany(s => new []
			{
                new AutofixAllowedDefineConstant(new SubplatformName("Plugins\\ReSharper.FSharp\\src"), s),
			});
		}
	}
}