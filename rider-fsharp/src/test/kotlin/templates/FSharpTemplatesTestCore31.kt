package templates

import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import org.testng.annotations.Test

@Test
@TestEnvironment(toolset = ToolsetVersion.TOOLSET_16_CORE, coreVersion = CoreVersion.DOT_NET_CORE_3_1)
class FSharpTemplatesTestCore31 : FSharpTemplatesTestCore()
{
    @Test
    fun classlibNetCoreAppTemplate() = super.classlibNetCoreAppTemplate(targetFramework = "netcoreapp3.1")

    @Test
    fun consoleAppCoreTemplate() = super.consoleAppCoreTemplate(expectedOutput = "Hello World from F#!", 7)
}