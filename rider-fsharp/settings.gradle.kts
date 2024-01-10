pluginManagement {
  val rdVersion: String by settings
  repositories {
    if (rdVersion == "SNAPSHOT")
      mavenLocal()
    maven { setUrl("https://packages.jetbrains.team/maven/p/ij/intellij-dependencies") }
    maven { setUrl("https://cache-redirector.jetbrains.com/plugins.gradle.org") }
    gradlePluginPortal()
    // This is for snapshot version of 'org.jetbrains.intellij' plugin
    maven { setUrl("https://oss.sonatype.org/content/repositories/snapshots/") }
  }
  plugins {
    id ("com.jetbrains.rdgen") version rdVersion
  }
  resolutionStrategy {
    eachPlugin {
      when (requested.id.name) {
        // This required to correctly rd-gen plugin resolution. May be we should switch our naming to match Gradle plugin naming convention.
        "rdgen" -> {
          useModule("com.jetbrains.rd:rd-gen:$rdVersion")
        }
      }
    }
  }
}

rootProject.name = "rider-fsharp"

include("protocol")