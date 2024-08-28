using System;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Properties;
using JetBrains.RdBackend.Common.Features.ProjectModel.View;
using JetBrains.ReSharper.Plugins.FSharp.Util;

namespace JetBrains.ReSharper.Plugins.FSharp.Psi;

[ShellComponent(Instantiation.DemandAnyThreadSafe)]
public class FSharpViewPresenterExtension : ProjectModelViewPresenterExtension
{
    public override bool TryAddUserData(IProjectFile projectFile, out string name, out string value)
    {
        var buildAction = projectFile.Properties.TryGetUniqueBuildAction()?.Value;
        if (string.Equals(buildAction, FSharpMsBuildUtils.ItemTypes.compileAfterItemType,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(buildAction, FSharpMsBuildUtils.ItemTypes.compileBeforeItemType,
                StringComparison.OrdinalIgnoreCase))
        {
            name = "CompileOrder";
            value = buildAction;
            return true;
        }

        if (projectFile.Properties.PropertiesCollection.TryGetValue(FSharpMsBuildUtils.ProjectItemMetadata.compileOrder, out var order))
        {
            name = "CompileOrder";
            value = order;
            return true;
        }

        return base.TryAddUserData(projectFile, out name, out value);
    }
}
