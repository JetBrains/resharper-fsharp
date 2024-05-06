import org.jetbrains.grammarkit.GrammarKitConstants
import org.jetbrains.grammarkit.tasks.GenerateLexerTask
import java.nio.file.Files
import java.nio.file.StandardCopyOption

plugins {
  id("org.jetbrains.grammarkit")
}

repositories {
  maven("https://cache-redirector.jetbrains.com/intellij-dependencies")
  maven("https://cache-redirector.jetbrains.com/maven-central")
}

val isMonorepo = rootProject.projectDir != projectDir.parentFile
val fsharpRepoRoot: File = projectDir.parentFile.parentFile
val backendLexerSources = fsharpRepoRoot.resolve("rider-fsharp/build/backend-lexer-sources/")
val resharperPluginPath = fsharpRepoRoot.resolve("ReSharper.FSharp")

val platformLibConfiguration by configurations.registering
val flexConfiguration by configurations.registering

dependencies {
  @Suppress("UnstableApiUsage")
  flexConfiguration("org.jetbrains.intellij.deps.jflex:jflex:${GrammarKitConstants.JFLEX_DEFAULT_VERSION}")
  @Suppress("UnstableApiUsage")
  platformLibConfiguration(project(
    mapOf(
      "path" to ":",
      "configuration" to "platformLibConfiguration"
    )
  ))
}

tasks {
  val fsharpLexerTargetDir = if (isMonorepo) {
    val monorepoRoot = buildscript.sourceFile?.parentFile?.parentFile?.parentFile?.parentFile?.parentFile?.parentFile
                       ?: error("Monorepo root not found")
    check(monorepoRoot.resolve(".ultimate.root.marker").isFile) {
      error("Incorrect location in monorepo: monorepoRoot='$monorepoRoot'")
    }
    monorepoRoot.resolve("dotnet/Plugins/_ReSharperFSharp.Pregenerated/Frontend/src/main/java/com/jetbrains/rider/ideaInterop/fileTypes/fsharp/lexer")
  } else {
    fsharpRepoRoot.resolve("rider-fsharp/src/generated/java/com/jetbrains/rider/ideaInterop/fileTypes/fsharp/lexer")
  }

  val unicodeLexDst = backendLexerSources.resolve("Unicode.lex")

  val copyUnicodeLex = create("copyUnicodeLex") {
    group = "grammarkit"
    outputs.file(unicodeLexDst)
    if (isMonorepo) {
      val monorepoRoot = buildscript.sourceFile?.parentFile?.parentFile?.parentFile?.parentFile?.parentFile?.parentFile
                         ?: error("Monorepo root not found")
      val unicodeLexSrc = monorepoRoot.resolve("dotnet/Psi.Features/Tasks/CsLex/Resources/Unicode.lex")
      inputs.file(unicodeLexSrc)
      doFirst {
        unicodeLexDst.writeBytes(unicodeLexSrc.readBytes())
      }
    } else {
      inputs.files(platformLibConfiguration)
      doFirst {
        val libFile = platformLibConfiguration.get().singleFile
        val libPath = File(libFile.readText().trim())
        val unicodeLexSrc = libPath.resolve("ReSharperHost/PsiTasks/Unicode.lex")
        unicodeLexDst.writeBytes(unicodeLexSrc.readBytes())
      }
    }
  }

  val copyBackendLexerSources = create<Copy>("copyBackendLexerSources") {
    group = "grammarkit"
    from("$resharperPluginPath/src/FSharp/FSharp.Psi/src/Parsing/Lexing") {
      include("*.lex")
    }
    into(backendLexerSources)
  }

  create<GenerateLexerTask>("generateLexer") {
    dependsOn(copyBackendLexerSources, copyUnicodeLex)

    inputs.file(unicodeLexDst)
    sourceFile.set(fsharpRepoRoot.resolve("rider-fsharp/lexer/src/_FSharpLexer.flex"))
    purgeOldFiles.set(true)
    targetDir.set(fsharpLexerTargetDir.absolutePath)
    val targetName = "_FSharpLexer"
    targetClass.set(targetName)
    classpath(flexConfiguration)

    doLast {
      val targetFile = fsharpLexerTargetDir.resolve("$targetName.java")
      if (!targetFile.exists()) error("Lexer file $targetFile was not generated")
      removeFirstMatchLineByRegexAndNormalizeEndings(targetFile, Regex("^( \\* from the specification file .*)\$"))
    }
  }
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
  Files.move(tempFile.toPath(), file.toPath(), StandardCopyOption.REPLACE_EXISTING)
}
