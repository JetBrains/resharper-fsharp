plugins {
  id("org.jetbrains.kotlin.jvm")
}

val rdLibDirectory: () -> File by rootProject.extra

repositories {
  mavenCentral()
  maven { setUrl("https://cache-redirector.jetbrains.com/maven-central") }
  flatDir {
    dir(rdLibDirectory())
  }
}

tasks {
  withType<JavaCompile> {
    sourceSets {
      main {
        java {
          srcDir("src/kotlin/model")
        }
      }
    }
  }
}

dependencies {
  implementation("org.jetbrains.kotlin:kotlin-stdlib")
  implementation(group = "", name = "rd-gen")
  implementation(group = "", name = "rider-model")
}