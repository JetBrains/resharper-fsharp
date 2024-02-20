import com.jetbrains.rd.generator.gradle.RdGenExtension
import org.gradle.api.tasks.testing.logging.TestExceptionFormat
import org.jetbrains.grammarkit.tasks.GenerateLexerTask
import org.jetbrains.intellij.IntelliJPluginConstants
import org.jetbrains.intellij.tasks.InstrumentCodeTask
import org.jetbrains.intellij.tasks.PrepareSandboxTask
import org.jetbrains.intellij.tasks.RunIdeTask
import org.jetbrains.kotlin.daemon.common.toHexString
import org.jetbrains.kotlin.gradle.tasks.KotlinCompile

plugins {
  id("com.jetbrains.rdgen") version "2023.2.2-preview1"
  id("org.jetbrains.intellij") version "1.13.3" // https://github.com/JetBrains/gradle-intellij-plugin/releases
  id("org.jetbrains.grammarkit") version "2021.2.2"
  id("me.filippov.gradle.jvm.wrapper") version "0.14.0"
  kotlin("jvm") version "1.8.0"
}

dependencies {
  testImplementation("junit:junit:4.13.2")
  testRuntimeOnly("org.junit.vintage:junit-vintage-engine:5.9.2")
  testRuntimeOnly("org.junit.platform:junit-platform-launcher:1.9.2")
}

apply {
  plugin("kotlin")
}

repositories {
  mavenCentral()
  maven("https://cache-redirector.jetbrains.com/repo1.maven.org/maven2")
  maven("https://cache-redirector.jetbrains.com/intellij-dependencies")
}

val baseVersion = "2024.1"
val buildCounter = ext.properties["build.number"] ?: "9999"
version = "$baseVersion.$buildCounter"

intellij {
  type.set("RD")

  // Download a version of Rider to compile and run with. Either set `version` to
  // 'LATEST-TRUNK-SNAPSHOT' or 'LATEST-EAP-SNAPSHOT' or a known version.
  // This will download from www.jetbrains.com/intellij-repository/snapshots or
  // www.jetbrains.com/intellij-repository/releases, respectively.
  // Note that there's no guarantee that these are kept up-to-date
  // version = 'LATEST-TRUNK-SNAPSHOT'
  // If the build isn't available in intellij-repository, use an installed version via `localPath`
  // localPath = '/Users/matt/Library/Application Support/JetBrains/Toolbox/apps/Rider/ch-1/171.4089.265/Rider EAP.app/Contents'
  // localPath = "C:\\Users\\Ivan.Shakhov\\AppData\\Local\\JetBrains\\Toolbox\\apps\\Rider\\ch-0\\171.4456.459"
  // localPath = "C:\\Users\\ivan.pashchenko\\AppData\\Local\\JetBrains\\Toolbox\\apps\\Rider\\ch-0\\dev"
  // localPath 'build/riderRD-173-SNAPSHOT'

  val dir = file("build/rider")
  if (dir.exists()) {
    logger.lifecycle("*** Using Rider SDK from local path " + dir.absolutePath)
    localPath.set(dir.absolutePath)
  } else {
    logger.lifecycle("*** Using Rider SDK from intellij-snapshots repository")
    version.set("$baseVersion-SNAPSHOT")
  }

  instrumentCode.set(false)
  downloadSources.set(false)

  // Uncomment when need to install plugin into a different IDE build.
  // updateSinceUntilBuild = false

  // rider-plugins-appender: workaround for https://youtrack.jetbrains.com/issue/IDEA-179607
  // org.intellij.intelliLang needed for tests with language injection marks
  plugins.set(
    listOf(
      "rider-plugins-appender",
      "org.intellij.intelliLang",
      "DatabaseTools",
      "css-impl",
      "javascript-impl",
      "com.intellij.ml.llm"
    )
  )
}

val repoRoot = projectDir.parentFile!!
val resharperPluginPath = File(repoRoot, "ReSharper.FSharp")

val rdLibDirectory: () -> File = { file("${tasks.setupDependencies.get().idea.get().classes}/lib/rd") }
extra["rdLibDirectory"] = rdLibDirectory

val buildConfiguration = ext.properties["BuildConfiguration"] ?: "Debug"
val primaryTargetFramework = "net472"
val outputRelativePath = "bin/$buildConfiguration/$primaryTargetFramework"
val ktOutputRelativePath = "src/main/java/com/jetbrains/rider/plugins/fsharp/protocol"

val productMonorepoDir = getProductMonorepoRoot()
val monorepoPreGeneratedRootDir by lazy { productMonorepoDir?.resolve("Plugins/_ReSharperFSharp.Pregenerated") ?: error("Building not in monorepo") }
val monorepoPreGeneratedFrontendDir by lazy {  monorepoPreGeneratedRootDir.resolve("Frontend") }
val monorepoPreGeneratedBackendDir by lazy {  monorepoPreGeneratedRootDir.resolve("BackendModel") }
val ktOutputMonorepoRoot by lazy { monorepoPreGeneratedFrontendDir.resolve(ktOutputRelativePath) }

val libFiles = listOf(
  "FSharp/FSharp.Common/$outputRelativePath/FSharp.Core.dll",
  "FSharp/FSharp.Common/$outputRelativePath/FSharp.Core.xml",
  "FSharp/FSharp.Common/$outputRelativePath/FSharp.Compiler.Service.dll", // todo: add pdb after next repack
  "FSharp/FSharp.Common/$outputRelativePath/FSharp.DependencyManager.Nuget.dll",
  "FSharp/FSharp.Common/$outputRelativePath/FSharp.Compiler.Interactive.Settings.dll"
)

val pluginFiles = listOf(
  "FSharp/FSharp.ProjectModelBase/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.ProjectModelBase",
  "FSharp/FSharp.Common/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.Common",
  "FSharp/FSharp.Psi/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.Psi",
  "FSharp/FSharp.Psi.Services/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.Psi.Services",
  "FSharp/FSharp.Psi.Daemon/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.Psi.Daemon",
  "FSharp/FSharp.Psi.Intentions/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.Psi.Intentions",
  "FSharp/FSharp.Psi.Features/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.Psi.Features",
  "FSharp/FSharp.Fantomas.Protocol/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.Fantomas.Protocol",
  "FSharp/FSharp.TypeProviders.Protocol/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Protocol"
)

val typeProvidersFiles = listOf(
  "FSharp/FSharp.Common/$outputRelativePath/FSharp.Core.dll",
  "FSharp/FSharp.Common/$outputRelativePath/FSharp.Core.xml",
  "FSharp.TypeProviders.Host/FSharp.TypeProviders.Host/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.exe",
  "FSharp.TypeProviders.Host/FSharp.TypeProviders.Host/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.pdb",
  "FSharp.TypeProviders.Host/FSharp.TypeProviders.Host/$outputRelativePath/JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.exe.config",
  "FSharp.TypeProviders.Host/FSharp.TypeProviders.Host.NetCore/bin/$buildConfiguration/netcoreapp3.1/JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.NetCore.dll",
  "FSharp.TypeProviders.Host/FSharp.TypeProviders.Host.NetCore/bin/$buildConfiguration/netcoreapp3.1/JetBrains.ReSharper.Plugins.FSharp.TypeProviders.Host.NetCore.pdb",
  "FSharp.TypeProviders.Host/FSharp.TypeProviders.Host.NetCore/bin/$buildConfiguration/netcoreapp3.1/tploader.win.runtimeconfig.json",
  "FSharp.TypeProviders.Host/FSharp.TypeProviders.Host.NetCore/bin/$buildConfiguration/netcoreapp3.1/tploader.unix.runtimeconfig.json"
)

val fantomasHostFiles = listOf(
  "FSharp.Fantomas.Host/bin/$buildConfiguration/net6.0/JetBrains.ReSharper.Plugins.FSharp.Fantomas.Host.dll",
  "FSharp.Fantomas.Host/bin/$buildConfiguration/net6.0/JetBrains.ReSharper.Plugins.FSharp.Fantomas.Host.pdb",
  "FSharp.Fantomas.Host/bin/$buildConfiguration/net6.0/JetBrains.ReSharper.Plugins.FSharp.Fantomas.Host.deps.json",
  "FSharp.Fantomas.Host/bin/$buildConfiguration/net6.0/Fantomas.Core.dll",
  "FSharp.Fantomas.Host/bin/$buildConfiguration/net6.0/Fantomas.FCS.dll",
  "FSharp.Fantomas.Host/bin/$buildConfiguration/net6.0/Fantomas.Host.win.runtimeconfig.json",
  "FSharp.Fantomas.Host/bin/$buildConfiguration/net6.0/Fantomas.Host.unix.runtimeconfig.json"
)

val externalAnnotationsDirectory = "$resharperPluginPath/src/FSharp/annotations"

val nugetConfigPath = File(repoRoot, "NuGet.Config")
val dotNetSdkPathPropsPath = File("build", "DotNetSdkPath.generated.props")
val backendLexerSources = "$repoRoot/rider-fsharp/build/backend-lexer-sources/"

val riderFSharpTargetsGroup = "rider-fsharp"

fun File.writeTextIfChanged(content: String) {
  val bytes = content.toByteArray()

  if (!exists() || readBytes().toHexString() != bytes.toHexString()) {
    println("Writing $path")
    writeBytes(bytes)
  }
}

tasks {
  val dotNetSdkPath by lazy {
    val sdkPath = setupDependencies.get().idea.get().classes.resolve("lib").resolve("DotNetSdkForRdPlugins")
    if (sdkPath.isDirectory.not()) error("$sdkPath does not exist or not a directory")

    println("SDK path: $sdkPath")
    return@lazy sdkPath
  }

  configure<RdGenExtension> {
    val inMonorepo = productMonorepoDir != null
    logger.info("Configuring rdgen with inMonorepo=$inMonorepo")

    val csOutput =
      if (inMonorepo) File(monorepoPreGeneratedBackendDir, "FSharp.ProjectModelBase/src/Protocol")
      else File(repoRoot, "ReSharper.FSharp/src/FSharp/FSharp.ProjectModelBase/src/Protocol")
    val ktOutput =
      if (inMonorepo) File(monorepoPreGeneratedFrontendDir, ktOutputRelativePath)
      else File(repoRoot, "rider-fsharp/$ktOutputRelativePath")

    val typeProviderClientOutput =
      if (inMonorepo) File(monorepoPreGeneratedBackendDir, "FSharp.TypeProviders.Protocol/src/Client")
      else File(repoRoot, "ReSharper.FSharp/src/FSharp/FSharp.TypeProviders.Protocol/src/Client")
    val typeProviderServerOutput =
      if (inMonorepo) File(monorepoPreGeneratedBackendDir, "FSharp.TypeProviders.Protocol/src/Server")
      else File(repoRoot, "ReSharper.FSharp/src/FSharp/FSharp.TypeProviders.Protocol/src/Server")

    val fantomasServerOutput =
      if (inMonorepo) File(monorepoPreGeneratedBackendDir, "FSharp.Fantomas.Protocol/src/Server")
      else File(repoRoot, "ReSharper.FSharp/src/FSharp/FSharp.Fantomas.Protocol/src/Server")
    val fantomasClientOutput =
      if (inMonorepo) File(monorepoPreGeneratedBackendDir, "FSharp.Fantomas.Protocol/src/Client")
      else File(repoRoot, "ReSharper.FSharp/src/FSharp/FSharp.Fantomas.Protocol/src/Client")

    verbose = true
    hashFolder = "build/rdgen"
    logger.info("Configuring rdgen params")
    // *** Classpath and sources ***
    if (inMonorepo) {
      sources(
        listOf(
          File("$productMonorepoDir/Rider/Frontend/rider/model/sources"),
          File("$productMonorepoDir/Rider/ultimate/remote-dev/rd-ide-model-sources"),
          File(repoRoot, "rider-fsharp/protocol/src/kotlin/model")
        )
      )
    }
    else {
      classpath({
        logger.info("Calculating classpath for rdgen, intellij.ideaDependency is ${rdLibDirectory().canonicalPath}")
        rdLibDirectory().resolve("rider-model.jar").canonicalPath
      })
      sources(File(repoRoot, "rider-fsharp/protocol/src/kotlin/model"))
    }
    packages = "model"

    generator {
      language = "kotlin"
      transform = "asis"
      root = "com.jetbrains.rider.model.nova.ide.IdeRoot"
      namespace = "com.jetbrains.rider.model"
      directory = "$ktOutput"
      if (inMonorepo) generatedFileSuffix = ".Pregenerated"
    }

    generator {
      language = "csharp"
      transform = "reversed"
      root = "com.jetbrains.rider.model.nova.ide.IdeRoot"
      namespace = "JetBrains.Rider.Model"
      directory = "$csOutput"
      if (inMonorepo) generatedFileSuffix = ".Pregenerated"
    }

    generator {
      language = "csharp"
      transform = "asis"
      root = "model.RdFSharpTypeProvidersModel"
      namespace = "JetBrains.Rider.FSharp.TypeProviders.Protocol.Client"
      directory = "$typeProviderClientOutput"
      if (inMonorepo) generatedFileSuffix = ".Pregenerated"
    }
    generator {
      language = "csharp"
      transform = "reversed"
      root = "model.RdFSharpTypeProvidersModel"
      namespace = "JetBrains.Rider.FSharp.TypeProviders.Protocol.Server"
      directory = "$typeProviderServerOutput"
      if (inMonorepo) generatedFileSuffix = ".Pregenerated"
    }

    generator {
      language = "csharp"
      transform = "asis"
      root = "model.RdFantomasModel"
      namespace = "JetBrains.ReSharper.Plugins.FSharp.Fantomas.Client"
      directory = "$fantomasClientOutput"
      if (inMonorepo) generatedFileSuffix = ".Pregenerated"
    }
    generator {
      language = "csharp"
      transform = "reversed"
      root = "model.RdFantomasModel"
      namespace = "JetBrains.ReSharper.Plugins.FSharp.Fantomas.Server"
      directory = "$fantomasServerOutput"
      if (inMonorepo) generatedFileSuffix = ".Pregenerated"
    }
  }

  withType<InstrumentCodeTask> {
    val bundledMavenArtifacts = file("build/maven-artifacts")
    if (bundledMavenArtifacts.exists()) {
      logger.lifecycle("Use ant compiler artifacts from local folder: $bundledMavenArtifacts")
      compilerClassPathFromMaven.set(
        bundledMavenArtifacts.walkTopDown()
          .filter { it.extension == "jar" && !it.name.endsWith("-sources.jar") }
          .toList() + File("${ideaDependency.get().classes}/lib/util.jar")
      )
    } else {
      logger.lifecycle("Use ant compiler artifacts from maven")
    }
  }

  withType<PrepareSandboxTask> {
    var files = libFiles + pluginFiles.map { "$it.dll" } + pluginFiles.map { "$it.pdb" }
    files = files.map { "$resharperPluginPath/src/$it" }
    val fantomasHostFiles = fantomasHostFiles.map { "$resharperPluginPath/src/$it" }
    val typeProvidersFiles = typeProvidersFiles.map { "$resharperPluginPath/src/$it" }

    if (name == IntelliJPluginConstants.PREPARE_TESTING_SANDBOX_TASK_NAME) {
      val testHostPath = "$resharperPluginPath/src/FSharp/FSharp.Tests.Host/$outputRelativePath"
      val testHostName = "$testHostPath/JetBrains.ReSharper.Plugins.FSharp.Tests.Host"
      files = files + listOf("$testHostName.dll", "$testHostName.pdb")
    }

    fun moveToPlugin(files: List<String>, destinationFolder: String) {
      files.forEach {
        from(it) { into("${intellij.pluginName.get()}/$destinationFolder") }
      }
    }

    moveToPlugin(files, "dotnet")
    moveToPlugin(fantomasHostFiles, "fantomas")
    moveToPlugin(typeProvidersFiles, "typeProviders")
    moveToPlugin(listOf("projectTemplates"), "projectTemplates")
    moveToPlugin(listOf(externalAnnotationsDirectory), "dotnet/Extensions/com.jetbrains.rider.fsharp/annotations")

    doLast {
      fun validateFiles(files: List<String>, destinationFolder: String) {
        files.forEach {
          val file = file(it)
          if (!file.exists()) throw RuntimeException("File $file does not exist")
          logger.warn("$name: ${file.name} -> $destinationDir/${intellij.pluginName.get()}/$destinationFolder")
        }
      }
      validateFiles(files, "dotnet")
      validateFiles(fantomasHostFiles, "fantomas")
      validateFiles(typeProvidersFiles, "typeProviders")
      validateFiles(listOf(externalAnnotationsDirectory), "dotnet/Extensions/com.jetbrains.rider.fsharp/annotations")
    }
  }

  // Initially introduced in:
  // https://github.com/JetBrains/ForTea/blob/master/Frontend/build.gradle.kts
  withType<RunIdeTask> {
    // Match Rider's default heap size of 1.5Gb (default for runIde is 512Mb)
    maxHeapSize = "1500m"
  }

  val resetLexerDirectory = create("resetLexerDirectory") {
    doFirst {
      File(backendLexerSources).deleteRecursively()
      File(backendLexerSources).mkdirs()
    }
  }

  // Cannot use ordinary copy here, because it requires eager evaluation of locations
  val copyUnicodeLex = create("copyUnicodeLex") {
    dependsOn(resetLexerDirectory)
    doFirst {
      val libPath = File("$dotNetSdkPath").parent
      File(libPath, "ReSharperHost/PsiTasks").listFiles { it -> it.extension == "lex" }!!.forEach {
        println(it)
        it.copyTo(File(backendLexerSources, it.name))
      }
    }
  }

  val copyBackendLexerSources = create<Copy>("copyBackendLexerSources") {
    dependsOn(resetLexerDirectory)
    from("$resharperPluginPath/src/FSharp/FSharp.Psi/src/Parsing/Lexing") {
      include("*.lex")
    }
    into(backendLexerSources)
  }

  fun generateLexer(inMonorepo: Boolean, taskName: String) = register<GenerateLexerTask>(taskName) {
    dependsOn(copyBackendLexerSources, copyUnicodeLex)
    source.set("src/main/java/com/jetbrains/rider/ideaInterop/fileTypes/fsharp/lexer/_FSharpLexer.flex")

    val relTargetDir = "src/main/java/com/jetbrains/rider/ideaInterop/fileTypes/fsharp/lexer"
    val targetName = "_FSharpLexer"
    val targetDirAbs =
      if (inMonorepo) monorepoPreGeneratedFrontendDir.resolve(relTargetDir)
      else File(repoRoot, "rider-fsharp/$relTargetDir")
    targetDir.set(targetDirAbs.toString())
    targetClass.set(targetName)
    purgeOldFiles.set(true)
    outputs.upToDateWhen { false }

    if (inMonorepo) {
      // The generated file contains not constant comment with a source file abs path
      // remove it to prevent changes on each generation
      // Also, line endings may depend on the platform, so unify it to LF
      doLast {
        val targetFile = targetDirAbs.resolve("$targetName.java")
        if (!targetFile.exists()) error("Lexer file $targetFile was not generated")

        removeFirstMatchLineByRegexAndNormalizeEndings(targetFile, Regex("^( \\* from the specification file .*)\$"))
      }
    }
  }

  val generateFSharpLexer = generateLexer(false, "generateFSharpLexer")
  val generateFSharpLexerMonorepo = generateLexer(true, "generateFSharpLexerMonorepo")

  withType<KotlinCompile> {
    kotlinOptions.jvmTarget = "17"
    dependsOn(generateFSharpLexer, rdgen)
  }

  val parserTest by register<Test>("parserTest") {
    useJUnitPlatform()
  }

  named<Test>("test") {
    dependsOn(parserTest)
    useTestNG {
      groupByInstances = true
    }
  }

  withType<Test> {
    testLogging {
      showStandardStreams = true
      exceptionFormat = TestExceptionFormat.FULL
    }
    val rerunSuccessfulTests = false
    outputs.upToDateWhen { !rerunSuccessfulTests }
    ignoreFailures = true
  }

  val writeDotNetSdkPathProps = create("writeDotNetSdkPathProps") {
    group = riderFSharpTargetsGroup
    doLast {
      dotNetSdkPathPropsPath.writeTextIfChanged(
        """<Project>
  <PropertyGroup>
    <DotNetSdkPath>$dotNetSdkPath</DotNetSdkPath>
  </PropertyGroup>
</Project>
"""
      )
    }

    getByName("buildSearchableOptions") {
      enabled = buildConfiguration == "Release"
    }
  }

  val writeNuGetConfig = create("writeNuGetConfig") {
    group = riderFSharpTargetsGroup
    doLast {
      nugetConfigPath.writeTextIfChanged(
        """<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="resharper-sdk" value="$dotNetSdkPath" />
  </packageSources>
</configuration>
"""
      )
    }
  }

  named("assemble") {
    doLast {
      logger.lifecycle("Plugin version: $version")
      logger.lifecycle("##teamcity[buildNumber '$version']")
    }
  }

  val prepare = create("prepare") {
    group = riderFSharpTargetsGroup
    dependsOn(rdgen, writeNuGetConfig, writeDotNetSdkPathProps, generateFSharpLexer)
  }

  create("buildReSharperPlugin") {
    group = riderFSharpTargetsGroup
    dependsOn(prepare)
    doLast {
      exec {
        executable = "msbuild"
        args = listOf("$resharperPluginPath/ReSharper.FSharp.sln")
      }
    }
  }

  create("prepareMonorepo") {
    dependsOn(generateFSharpLexerMonorepo, "rdgen")
  }

  defaultTasks(prepare)
}

fun getProductMonorepoRoot(): File? {
  var currentDir = repoRoot

  while (currentDir.parent != null) {
    if (currentDir.listFiles()?.any { it.name == ".dotnet-products.root.marker" } == true) {
      return currentDir
    }
    currentDir = currentDir.parentFile
  }

  return null
}

fun removeFirstMatchLineByRegexAndNormalizeEndings(file: File, removeRegex: Regex) {
  val tempFile = File.createTempFile("${file.name}.temp", null)
  var found = false
  file.useLines { lines ->
    tempFile.bufferedWriter().use { writer ->
      lines.forEach { line ->
        if (!found && removeRegex.matches(line)) {
          // do not write the line if it is matched with the regex
          found = true
        } else {
          // rewrite the line with LF
          writer.write(line + "\n")
        }
      }
    }
  }
  file.delete()
  tempFile.renameTo(file)
}
