import org.jetbrains.intellij.platform.gradle.TestFrameworkType

plugins {
  id("org.jetbrains.intellij.platform.module")
  kotlin("jvm")
}

val riderBaseVersion: String by project

repositories {
  maven("https://cache-redirector.jetbrains.com/maven-central")
  intellijPlatform {
    defaultRepositories()
  }
}

dependencies {
  implementation(project(":"))
  testImplementation(libs.junit4)
  testRuntimeOnly(libs.junit.vintage)
  testRuntimeOnly(libs.junit.engine)
  testRuntimeOnly(libs.junit.launcher)

  intellijPlatform {
    val dir = file("../build/rider")
    if (dir.exists()) {
      logger.lifecycle("*** Using Rider SDK from local path " + dir.absolutePath)
      logger.lifecycle("*** (already imported from the main module)")
    } else {
      logger.lifecycle("*** Using Rider SDK from intellij-snapshots repository")
      rider("$riderBaseVersion-SNAPSHOT") { useInstaller = false }
    }
    jetbrainsRuntime()
    testFramework(TestFrameworkType.Bundled)
    bundledModule("intellij.rider.rdclient.dotnet")
  }
}

tasks {
  instrumentTestCode {
    enabled = false
  }
  instrumentCode {
    enabled = false
  }

  test {
    useJUnitPlatform()
  }
}
