using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Components;
using JetBrains.DataFlow;
using JetBrains.Platform.MsBuildModel;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Impl.Build;
using JetBrains.ProjectModel.Model2.Assemblies.Interfaces;
using JetBrains.ProjectModel.ProjectsHost;
using JetBrains.ProjectModel.ProjectsHost.MsBuild;
using JetBrains.ProjectModel.ProjectsHost.SolutionHost;
using JetBrains.ReSharper.PsiGen.Util;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;
using Microsoft.FSharp.Compiler.SourceCodeServices;

namespace JetBrains.ReSharper.Psi.FSharp
{
  [SolutionComponent]
  public class FSharpProjectOptionsBuilder
  {
    private readonly MsBuildProjectHost myMsBuildHost;

    public FSharpProjectOptionsBuilder(Lifetime lifetime, ISolution solution)
    {
      myMsBuildHost = solution.ProjectsHostContainer().GetComponent<MsBuildProjectHost>();
    }

    /// <summary>
    /// Build project options for FSharp.Compiler.Service. Options for referenced projects are not created.
    /// </summary>
    public FSharpProjectOptions Build(IProject project)
    {
      using (ReadLockCookie.Create())
      {
        var targetFrameworkId = project.GetCurrentTargetFrameworkId();
        var projectOptions = new List<string>
        {
          "--out:" + project.GetOutputFilePath(targetFrameworkId),
          "--simpleresolution",
          "--noframework",
          "--debug:full",
          "--debug+",
          "--optimize-",
          "--tailcalls-",
          "--fullpaths",
          "--flaterrors",
          "--highentropyva+",
          "--target:library", // todo
//            "--subsystemversion:6.00",
//            "--warnon:",
          "--platform:anycpu"
        };

        // todo: Get all properties for compiler from proper IProject.Configurations
        projectOptions.addAll(GetDefines(project).Convert(s => "--define:" + s));
        projectOptions.addAll(GetReferencedPathsOptions(project));

        var projectFileNames = new List<string>();
        var projectMark = project.GetProjectMark().NotNull();
        myMsBuildHost.mySessionHolder.Execute(session =>
        {
          session.EditProject(projectMark, editor =>
          {
            // Obtains all items in a right order directly from msbuild
            foreach (var item in editor.Items)
              if (BuildAction.GetOrCreate(item.ItemType()).IsCompile())
                projectFileNames.Add(item.EvaluatedInclude); // todo: need full paths here
          });
        });

        return new FSharpProjectOptions(
          project.ProjectFileLocation.FullPath,
          projectFileNames.ToArray(),
          projectOptions.ToArray(),
          referencedProjects: EmptyArray<Tuple<string, FSharpProjectOptions>>.Instance,
          isIncompleteTypeCheckEnvironment: false,
          useScriptResolutionRules: false,
          loadTime: DateTime.Now,
          unresolvedReferences: null
        );
      }
    }

    /// <summary>
    /// Current configuration defines, e.g. TRACE or DEBUG. Used in FSharpLexer.
    /// </summary>
    [NotNull]
    public string[] GetDefines([NotNull] IProject project)
    {
      var configurations = (project as ProjectImpl)?.ProjectProperties.ActiveConfigurations.Configurations;
      var definesString = configurations?.OfType<ManagedProjectConfiguration>().FirstOrDefault()?.DefineConstants;
      return string.IsNullOrEmpty(definesString)
        ? EmptyArray<string>.Instance
        : definesString.Split(';', ',', ' ').Select(x => x.Trim()).ToArray();
    }

    [NotNull]
    private IEnumerable<string> GetReferencedPathsOptions(IProject project)
    {
      var framework = project.GetCurrentTargetFrameworkId();
      var refProjectsOutputs = project.GetReferencedProjects(framework)
        .Select(p => p.GetOutputFilePath(p.GetCurrentTargetFrameworkId()));
      var refAssembliesPaths = project.GetAssemblyReferences(framework)
        .Select(a => a.ResolveResultAssemblyFile().Location);
      return refProjectsOutputs.Concat(refAssembliesPaths).Select(a => "-r:" + a.FullPath);
    }
  }
}