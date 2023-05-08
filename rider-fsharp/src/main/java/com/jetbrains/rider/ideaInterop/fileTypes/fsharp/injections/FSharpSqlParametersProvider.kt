package com.jetbrains.rider.ideaInterop.fileTypes.fsharp.injections

import com.intellij.database.DbmsExtension
import com.intellij.database.settings.DatabaseParameterPatternProvider
import com.intellij.database.settings.UserPatterns.ParameterPattern
import com.intellij.sql.dialects.SqlLanguageDialect
import com.intellij.sql.dialects.cockroach.CRoachDialect
import com.intellij.sql.dialects.greenplum.GPlumDialect
import com.intellij.sql.dialects.postgres.PgDialect
import com.intellij.sql.dialects.redshift.RsDialect
import com.intellij.sql.dialects.snowflake.SFlakeDialect
import com.intellij.sql.dialects.vertica.VertDialect

class FSharpSqlParametersProvider : DatabaseParameterPatternProvider {
  private val ourPatterns = arrayOf(
    ParameterPattern("(?<![@<])@[a-zA-Z0-9_]+", generateScope(), "@name")
  )

  override fun getPatterns() = ourPatterns

  companion object {
    fun generateScope(): String {
      // "F#, Redshift, Cockroach, Greenplum, PostgreSQL, Snowflake, Vertica"
      val builder = StringBuilder("F#")
      SqlLanguageDialect.EP
        .allExtensions()
        .map(DbmsExtension.Bean<SqlLanguageDialect>::getInstance)
        .forEach {
          builder.append(", ")
          if (it !in languagesWithoutVariableParsing) {
            builder.append('-')
          }
          builder.append(it.id)
        }
      return builder.toString()
    }

    private val languagesWithoutVariableParsing = listOf(
      RsDialect.INSTANCE,
      CRoachDialect.INSTANCE,
      GPlumDialect.INSTANCE,
      PgDialect.INSTANCE,
      SFlakeDialect.INSTANCE,
      VertDialect.INSTANCE
    )
  }
}
