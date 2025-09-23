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
  testImplementation(libs.junit)
  testRuntimeOnly("org.junit.vintage:junit-vintage-engine:5.9.2")
  testRuntimeOnly("org.junit.platform:junit-platform-launcher:1.9.2")
  intellijPlatform {
    val dir = file("../build/rider")
    if (dir.exists()) {
      logger.lifecycle("*** Using Rider SDK from local path " + dir.absolutePath)
      local(dir)
    } else {
      logger.lifecycle("*** Using Rider SDK from intellij-snapshots repository")
      rider("$riderBaseVersion-SNAPSHOT", useInstaller = false)
    }
    jetbrainsRuntime()
    testFramework(TestFrameworkType.Bundled)
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
