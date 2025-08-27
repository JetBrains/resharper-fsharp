import com.jetbrains.rd.generator.gradle.RdGenTask

plugins {
  // Version is configured in gradle.properties
  id("com.jetbrains.rdgen")
  id("org.jetbrains.kotlin.jvm")
}

repositories {
  maven("https://cache-redirector.jetbrains.com/intellij-dependencies")
  maven("https://cache-redirector.jetbrains.com/maven-central")
  maven("https://cache-redirector.jetbrains.com/plugins.gradle.org")
  val rd_version: String by project
  if (rd_version == "SNAPSHOT") {
    mavenLocal()
  }
}

val isMonorepo = rootProject.projectDir != projectDir.parentFile
val fsharpRepoRoot: File = projectDir.parentFile.parentFile

sourceSets {
  main {
    kotlin {
      srcDir(fsharpRepoRoot.resolve("rider-fsharp/protocol/src/kotlin/model"))
    }
  }
}

data class FsharpGeneratorSettings(
  val csOutput: File,
  val ktOutput: File,
  val typeProviderClientOutput: File,
  val typeProviderServerOutput: File,
  val fantomasServerOutput: File,
  val fantomasClientOutput: File,
  val suffix: String)

val ktOutputRelativePath = "src/generated/kotlin/com/jetbrains/rider/plugins/fsharp/protocol"

val fsharpGeneratorSettings = if (isMonorepo) {
  val monorepoRoot = buildscript.sourceFile?.parentFile?.parentFile?.parentFile?.parentFile?.parentFile?.parentFile ?: error("Cannot find products home")
  check(monorepoRoot.resolve(".ultimate.root.marker").isFile) {
    error("Incorrect location in monorepo: monorepoRoot='$monorepoRoot'")
  }
  val monorepoPreGeneratedRootDir = monorepoRoot.resolve("dotnet/Plugins/_ReSharperFSharp.Pregenerated")
  val monorepoPreGeneratedFrontendDir = monorepoPreGeneratedRootDir.resolve("Frontend")
  val monorepoPreGeneratedBackendDir = monorepoPreGeneratedRootDir.resolve("BackendModel")
  val ktOutputMonorepoRoot = monorepoPreGeneratedFrontendDir.resolve(ktOutputRelativePath)
  FsharpGeneratorSettings (
    monorepoPreGeneratedBackendDir.resolve("FSharp.ProjectModelBase/src/Protocol"),
    monorepoPreGeneratedFrontendDir.resolve(ktOutputRelativePath),
    monorepoPreGeneratedBackendDir.resolve("FSharp.TypeProviders.Protocol/src/Client"),
    monorepoPreGeneratedBackendDir.resolve("FSharp.TypeProviders.Protocol/src/Server"),
    monorepoPreGeneratedBackendDir.resolve("FSharp.Fantomas.Protocol/src/Server"),
    monorepoPreGeneratedBackendDir.resolve("FSharp.Fantomas.Protocol/src/Client"),
    ".Pregenerated"
  )
} else {
  FsharpGeneratorSettings (
    fsharpRepoRoot.resolve("ReSharper.FSharp/src/FSharp/FSharp.ProjectModelBase/src/Protocol"),
    fsharpRepoRoot.resolve("rider-fsharp/$ktOutputRelativePath"),
    fsharpRepoRoot.resolve("ReSharper.FSharp/src/FSharp/FSharp.TypeProviders.Protocol/src/Client"),
    fsharpRepoRoot.resolve("ReSharper.FSharp/src/FSharp/FSharp.TypeProviders.Protocol/src/Server"),
    fsharpRepoRoot.resolve("ReSharper.FSharp/src/FSharp/FSharp.Fantomas.Protocol/src/Server"),
    fsharpRepoRoot.resolve("ReSharper.FSharp/src/FSharp/FSharp.Fantomas.Protocol/src/Client"),
    ""
  )
}

rdgen {
  verbose = true
  packages = "model"

  generator {
    language = "kotlin"
    transform = "asis"
    root = "com.jetbrains.rider.model.nova.ide.IdeRoot"
    namespace = "com.jetbrains.rider.model"
    directory = fsharpGeneratorSettings.ktOutput.absolutePath
    generatedFileSuffix = fsharpGeneratorSettings.suffix
  }

  generator {
    language = "csharp"
    transform = "reversed"
    root = "com.jetbrains.rider.model.nova.ide.IdeRoot"
    namespace = "JetBrains.Rider.Model"
    directory = fsharpGeneratorSettings.csOutput.absolutePath
    generatedFileSuffix = fsharpGeneratorSettings.suffix
  }

  generator {
    language = "csharp"
    transform = "asis"
    root = "model.RdFSharpTypeProvidersModel"
    namespace = "JetBrains.Rider.FSharp.TypeProviders.Protocol.Client"
    directory = fsharpGeneratorSettings.typeProviderClientOutput.absolutePath
    generatedFileSuffix = fsharpGeneratorSettings.suffix
  }
  generator {
    language = "csharp"
    transform = "reversed"
    root = "model.RdFSharpTypeProvidersModel"
    namespace = "JetBrains.Rider.FSharp.TypeProviders.Protocol.Server"
    directory = fsharpGeneratorSettings.typeProviderServerOutput.absolutePath
    generatedFileSuffix = fsharpGeneratorSettings.suffix
  }

  generator {
    language = "csharp"
    transform = "asis"
    root = "model.RdFantomasModel"
    namespace = "JetBrains.ReSharper.Plugins.FSharp.Fantomas.Client"
    directory = fsharpGeneratorSettings.fantomasClientOutput.absolutePath
    generatedFileSuffix = fsharpGeneratorSettings.suffix
  }

  generator {
    language = "csharp"
    transform = "reversed"
    root = "model.RdFantomasModel"
    namespace = "JetBrains.ReSharper.Plugins.FSharp.Fantomas.Server"
    directory = fsharpGeneratorSettings.fantomasServerOutput.absolutePath
    generatedFileSuffix = fsharpGeneratorSettings.suffix
  }
}

tasks.withType<RdGenTask> {
  dependsOn(sourceSets["main"].runtimeClasspath)
  classpath(sourceSets["main"].runtimeClasspath)
}

dependencies {
  if (isMonorepo) {
    implementation(project(":rider-model"))
  } else {
    val rdVersion: String by project
    val rdKotlinVersion: String by project

    implementation("com.jetbrains.rd:rd-gen:$rdVersion")
    implementation("org.jetbrains.kotlin:kotlin-stdlib:$rdKotlinVersion")
    implementation(
      project(
        mapOf(
          "path" to ":",
          "configuration" to "riderModel"
        )
      )
    )
  }
}
