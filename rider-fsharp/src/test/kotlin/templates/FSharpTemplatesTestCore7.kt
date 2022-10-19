package templates

import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import org.testng.annotations.Test

@Test
@TestEnvironment(toolset = ToolsetVersion.TOOLSET_17_CORE, coreVersion = CoreVersion.DOT_NET_7)
class FSharpTemplatesTestCore7 : FSharpTemplatesTestCore()
{
    @Test
    fun classlibNetCoreAppTemplate() = super.classlibNetCoreAppTemplate(targetFramework = "net7.0")

    @Test
    fun consoleAppCoreTemplate() = super.consoleAppCoreTemplate(expectedOutput = "Hello from F#", breakpointLine = 2)
}