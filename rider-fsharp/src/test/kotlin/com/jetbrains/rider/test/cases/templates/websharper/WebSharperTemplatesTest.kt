package com.jetbrains.rider.test.cases.templates.websharper

import com.jetbrains.rider.projectView.actions.projectTemplating.backend.ReSharperProjectTemplateProvider
import com.jetbrains.rider.test.annotations.TestEnvironment
import com.jetbrains.rider.test.base.RiderTemplatesTestBase
import com.jetbrains.rider.test.enums.CoreVersion
import com.jetbrains.rider.test.enums.ToolsetVersion
import com.jetbrains.rider.test.framework.downloadAndExtractArchiveArtifactIntoPersistentCache
import com.jetbrains.rider.test.framework.riderTestDataRepositoryUrl
import com.jetbrains.rider.test.scriptingApi.TemplateIdWithVersion
import com.jetbrains.rider.test.scriptingApi.checkSwea
import com.jetbrains.rider.test.scriptingApi.div
import org.testng.annotations.AfterClass
import org.testng.annotations.BeforeClass
import org.testng.annotations.Test
import java.io.File

@TestEnvironment(coreVersion = CoreVersion.DOT_NET_5, toolset = ToolsetVersion.TOOLSET_16_CORE)
class WebSharperTemplatesTest : RiderTemplatesTestBase() {
    private val webSharperTemplatesArchive: String = "WebSharper.Templates.20211103.zip"

    private val webSharperTemplatesVersion: String = "5.0.0.120"

    private lateinit var webSharperTemplatesNupkg: File

    object WebSharperTemplateIds {
        val fsharp_clientServer = TemplateIdWithVersion("WebSharper.ClientServer.FSharp.Template")
        val fsharp_extensions = TemplateIdWithVersion("WebSharper.Extension.FSharp.Template")
        val fsharp_library = TemplateIdWithVersion("WebSharper.Library.FSharp.Template")
        val fsharp_min = TemplateIdWithVersion("WebSharper.Min.FSharp.Template")
        
        val fsharp_offline = TemplateIdWithVersion("WebSharper.Offline.FSharp.Template")
        val fsharp_proxy = TemplateIdWithVersion("WebSharper.Proxy.FSharp.Template")
        val fsharp_spa = TemplateIdWithVersion("WebSharper.SPA.FSharp.Template")
    }

    @BeforeClass
    fun installWebSharperTemplates() {
        webSharperTemplatesNupkg = downloadAndExtractArchiveArtifactIntoPersistentCache(
            "$riderTestDataRepositoryUrl/$webSharperTemplatesArchive") /
                "WebSharper.Templates" /
                "websharper.templates.$webSharperTemplatesVersion.nupkg"
        ReSharperProjectTemplateProvider.addUserTemplateSource(webSharperTemplatesNupkg)
    }
    
    @Test(enabled = false) // wsfscservice.exe hangs and holds sdk directory
    fun library() {
        val templateId = WebSharperTemplateIds.fsharp_library
        doCoreTest(templateId, "Library") { project ->
            checkSwea(project)
        }
    }

    @AfterClass(alwaysRun = true)
    fun uninstallWebSharperTemplates() {
        ReSharperProjectTemplateProvider.removeUserTemplateSource(webSharperTemplatesNupkg)
    }
}