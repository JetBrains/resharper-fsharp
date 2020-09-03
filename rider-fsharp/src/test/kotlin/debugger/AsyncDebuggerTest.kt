package debugger

import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.DebuggerTestBase
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.scriptingApi.toggleBreakpoint
import com.jetbrains.rider.test.scriptingApi.waitForPause
import org.testng.annotations.Test

@Test
@TestEnvironment(coreVersion = CoreVersion.DEFAULT)
class AsyncDebuggerTest : DebuggerTestBase() {
    override val projectName = "AsyncProgram"
    override fun getSolutionDirectoryName() = projectName

    override val waitForCaches = true
    override val restoreNuGetPackages = true

    @Test(description = "RIDER-27263")
    fun testAsyncBreakpoint() {
        // Note that this test doesn't checks the behavior of FSharpBreakpointVariantsProvider, since it's never called
        // in tests. But the test breakpoints are multi-method by default, and we should just check that multi-method
        // breakpoints work well in F# code.
        testDebugProgram({
            toggleBreakpoint("Program.fs", 9)
        }, {
            waitForPause()
            dumpLocals()
        }, true)
    }
}