rootProject.name = "rider-fsharp"

pluginManagement {
  val rdVersion: String by settings
  val rdKotlinVersion: String by settings
  val intellijPlatformGradlePluginVersion: String by settings
  val grammarKitVersion: String by settings
  val gradleJvmWrapperVersion: String by settings

  repositories {
    maven("https://cache-redirector.jetbrains.com/intellij-dependencies")
    maven("https://cache-redirector.jetbrains.com/plugins.gradle.org")
    maven("https://cache-redirector.jetbrains.com/maven-central")

    if (rdVersion == "SNAPSHOT") {
      mavenLocal()
    }
  }

  plugins {
    id("com.jetbrains.rdgen") version rdVersion
    id("org.jetbrains.kotlin.jvm") version rdKotlinVersion
    id("org.jetbrains.intellij.platform") version intellijPlatformGradlePluginVersion
    id("org.jetbrains.intellij.platform.module") version intellijPlatformGradlePluginVersion
    id("org.jetbrains.grammarkit") version grammarKitVersion
    id("me.filippov.gradle.jvm.wrapper") version gradleJvmWrapperVersion
  }

  resolutionStrategy {
    eachPlugin {
      when (requested.id.name) {
        // This required to correctly rd-gen plugin resolution. May be we should switch our naming to match Gradle plugin naming convention.
        "rdgen" -> {
          useModule("com.jetbrains.rd:rd-gen:${rdVersion}")
        }
      }
    }
  }
}
dependencyResolutionManagement {
  repositories {
    maven("https://cache-redirector.jetbrains.com/intellij-dependencies")
    maven("https://cache-redirector.jetbrains.com/maven-central")
  }
}

include(":parserTest")
include(":protocol")
include(":lexer")
