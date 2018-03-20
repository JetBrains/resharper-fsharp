import com.intellij.openapi.diagnostic.Logger
import com.intellij.testFramework.LoggedErrorProcessor
import com.jetbrains.rider.test.LogErrorMessageException
import com.jetbrains.rider.test.TestCaseRunner
import com.jetbrains.rider.util.idea.ReSharperHostException
import org.testng.annotations.Test

class LogErrorTest : TestCaseRunner() {

    @Test(expectedExceptions = [(LogErrorMessageException::class)])
    fun testThrowEx() {
        Logger.getInstance("Projected Logger").error(ReSharperHostException("a", "b"))
        testCaseErrorsProcessor.throwIfNotEmpty()
    }
}