plugins {
  id("org.jetbrains.grammarkit")
  id("org.jetbrains.intellij")
}

repositories {
  maven("https://cache-redirector.jetbrains.com/intellij-dependencies")
  maven("https://cache-redirector.jetbrains.com/maven-central")
}

intellij {
  version.set("2024.1")
  type.set("RD")
}

val isMonorepo = rootProject.projectDir != projectDir.parentFile
val fsharpRepoRoot: File = projectDir.parentFile.parentFile
val backendLexerSources = fsharpRepoRoot.resolve("rider-fsharp/build/backend-lexer-sources/")
val resharperPluginPath = fsharpRepoRoot.resolve("ReSharper.FSharp")

tasks {
  val fsharpLexerTargetDir = if (isMonorepo) {
    val monorepoRoot = buildscript.sourceFile?.parentFile?.parentFile?.parentFile?.parentFile?.parentFile?.parentFile
      ?: error("Monorepo root not found")
    check(monorepoRoot.resolve(".ultimate.root.marker").isFile) {
      error("Incorrect location in monorepo: monorepoRoot='$monorepoRoot'")
    }
    monorepoRoot.resolve("dotnet/Plugins/_ReSharperFSharp.Pregenerated/Frontend/src/main/java/com/jetbrains/rider/ideaInterop/fileTypes/fsharp/lexer/")
  } else {
    fsharpRepoRoot.resolve("rider-fsharp/src/generated/java/com/jetbrains/rider/ideaInterop/fileTypes/fsharp/lexer")
  }

  val resetLexerDirectory = create("resetLexerDirectory") {
    group = "grammarkit"
    doFirst {
      delete {
        delete(backendLexerSources)
      }
    }
  }

  val copyUnicodeLex = create("copyUnicodeLex") {
    group = "grammarkit"
    dependsOn(resetLexerDirectory)
    if (isMonorepo) {
      val monorepoRoot = buildscript.sourceFile?.parentFile?.parentFile?.parentFile?.parentFile?.parentFile?.parentFile
        ?: error("Monorepo root not found")
      val unicodeLexSrc = monorepoRoot.resolve("dotnet/Psi.Features/Tasks/CsLex/Resources/Unicode.lex")
      val unicodeLexDst = backendLexerSources.resolve("Unicode.lex")
      inputs.file(unicodeLexSrc)
      outputs.file(unicodeLexDst)
      doFirst {
        copy {
          from(unicodeLexSrc)
          into(backendLexerSources)
        }
      }
    } else {
      val dotNetSdkPath by lazy {
        val sdkPath = setupDependencies.get().idea.get().classes.resolve("lib").resolve("DotNetSdkForRdPlugins")
        if (sdkPath.isDirectory.not()) error("$sdkPath does not exist or not a directory")

        return@lazy sdkPath
      }
      val unicodeLexSrc = dotNetSdkPath.resolve("../ReSharperHost/PsiTasks/Unicode.lex")
      val unicodeLexDst = backendLexerSources.resolve("Unicode.lex")
      inputs.file(unicodeLexSrc)
      outputs.file(unicodeLexDst)
      doFirst {
        copy {
          from(unicodeLexSrc)
          into(backendLexerSources)
        }
      }
    }
  }

  val copyBackendLexerSources = create<Copy>("copyBackendLexerSources") {
    group = "grammarkit"
    dependsOn(resetLexerDirectory)
    from("$resharperPluginPath/src/FSharp/FSharp.Psi/src/Parsing/Lexing") {
      include("*.lex")
    }
    into(backendLexerSources)
  }

  generateLexer.configure {
    dependsOn(copyBackendLexerSources, copyUnicodeLex)

    sourceFile.set(fsharpRepoRoot.resolve("rider-fsharp/lexer/src/_FSharpLexer.flex"))
    purgeOldFiles.set(true)
    outputs.upToDateWhen { false }
    targetDir.set(fsharpLexerTargetDir.absolutePath)
    val targetName = "_FSharpLexer"
    targetClass.set(targetName)
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
  tempFile.renameTo(file)
}
