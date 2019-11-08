package templates

import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import org.testng.annotations.Test

@Test
@TestEnvironment(toolset = ToolsetVersion.TOOLSET_16_CORE, coreVersion = CoreVersion.DOT_NET_CORE_2_1)
class FSharpTemplatesTestCore21 : FSharpTemplatesTestCore()
