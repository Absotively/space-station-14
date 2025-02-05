using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class MoveRoleOptionsFromProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "pref_unavailable",
                table: "preference",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            /* Keep pref_unavailable setting from each player's currently selected profile */
            migrationBuilder.Sql(@"
UPDATE preference
  SET preference.pref_unavailable = profile.pref_unavailable
  FROM profile
  WHERE profile.preference_id = preference.preference_id
    AND profile.slot = preference.selected_character_slot;
");

            migrationBuilder.DropColumn(
                name: "pref_unavailable",
                table: "profile");

            migrationBuilder.DropForeignKey(
                name: "FK_job_profile_profile_id",
                table: "job");

            /* For each player, keep the highest priority from any profile for each job,
             * treating High priority as Medium */
            migrationBuilder.Sql(@"
CREATE TEMP TABLE preference_job_temp
AS SELECT preference_id, job_name, min(2, max(priority)) priority
  FROM job
    JOIN profile ON job.profile_id = profile.profile_id
  GROUP BY preference_id, job_name;
");

            /* Keep the High priority from each player's currently selected profile */
            migrationBuilder.Sql(@"
UPDATE preference_job_temp
  SET priority = 3
  FROM job, profile, preference
  WHERE preference_job_temp.preference_id = profile.preference_id
    AND profile.profile_id = job.profile_id
    AND preference_job_temp.job_name = job.job_name
    AND profile.preference_id = preference.preference_id
    AND profile.slot = preference.selected_character_slot
    AND job.priority = 3;
");

            migrationBuilder.Sql("DELETE FROM job;");

            migrationBuilder.RenameColumn(
                name: "profile_id",
                table: "job",
                newName: "preference_id");

            migrationBuilder.RenameIndex(
                name: "IX_job_profile_id_job_name",
                table: "job",
                newName: "IX_job_preference_id_job_name");

            migrationBuilder.RenameIndex(
                name: "IX_job_profile_id",
                table: "job",
                newName: "IX_job_preference_id");

            migrationBuilder.AddForeignKey(
                name: "FK_job_preference_preference_id",
                table: "job",
                column: "preference_id",
                principalTable: "preference",
                principalColumn: "preference_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql(@"
INSERT INTO job(preference_id, job_name, priority)
SELECT preference_id, job_name, priority FROM preference_job_temp;
");

            migrationBuilder.Sql("DROP TABLE preference_job_temp;");

            migrationBuilder.DropForeignKey(
                name: "FK_antag_profile_profile_id",
                table: "antag");

            /* Combine each player's antag preferences across all their profiles */
            migrationBuilder.Sql(@"
CREATE TEMP TABLE preference_antag_temp
AS SELECT DISTINCT preference_id, antag_name
  FROM antag
    JOIN profile ON antag.profile_id = profile.profile_id;
");

            migrationBuilder.Sql("DELETE FROM antag;");

            migrationBuilder.RenameColumn(
                name: "profile_id",
                table: "antag",
                newName: "preference_id");

            migrationBuilder.RenameIndex(
                name: "IX_antag_profile_id_antag_name",
                table: "antag",
                newName: "IX_antag_preference_id_antag_name");

            migrationBuilder.AddForeignKey(
                name: "FK_antag_preference_preference_id",
                table: "antag",
                column: "preference_id",
                principalTable: "preference",
                principalColumn: "preference_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql(@"
INSERT INTO antag(preference_id, antag_name)
SELECT preference_id, antag_name, priority FROM preference_antag_temp;
");

            migrationBuilder.Sql("DROP TABLE preference_antag_temp;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            /* For lack of better ideas, copy each preference's role options to each
             * of its profiles */

            migrationBuilder.AddColumn<int>(
                name: "pref_unavailable",
                table: "profile",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
UPDATE profile
  SET pref_unavailable = preference.pref_unavailable
  FROM preference
  WHERE preference.preference_id = profile.preference_id;
");

            migrationBuilder.DropColumn(
                name: "pref_unavailable",
                table: "preference");

            migrationBuilder.DropForeignKey(
                name: "FK_job_preference_preference_id",
                table: "job");

            migrationBuilder.Sql(@"
CREATE TEMP TABLE profile_job_temp
AS SELECT profile_id, job_name, min(2, max(priority)) priority
  FROM job
    JOIN preference ON job.preference_id = preference.preference_id
    JOIN profile ON profile.preference_id = preference.preference_id;
");

            migrationBuilder.Sql("DELETE FROM job;");

            migrationBuilder.RenameColumn(
                name: "preference_id",
                table: "job",
                newName: "profile_id");

            migrationBuilder.RenameIndex(
                name: "IX_job_preference_id_job_name",
                table: "job",
                newName: "IX_job_profile_id_job_name");

            migrationBuilder.RenameIndex(
                name: "IX_job_preference_id",
                table: "job",
                newName: "IX_job_profile_id");

            migrationBuilder.AddForeignKey(
                name: "FK_job_profile_profile_id",
                table: "job",
                column: "profile_id",
                principalTable: "profile",
                principalColumn: "profile_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql(@"
INSERT INTO job(profile_id, job_name, priority)
SELECT profile_id, job_name, priority FROM profile_job_temp;
");

            migrationBuilder.Sql("DROP TABLE profile_job_temp;");

            migrationBuilder.DropForeignKey(
                name: "FK_antag_preference_preference_id",
                table: "antag");

            migrationBuilder.Sql(@"
CREATE TEMP TABLE profile_antag_temp
AS SELECT profile_id, antag_name
  FROM antag
    JOIN preference ON antag.preference_id = preference.preference_id
    JOIN profile ON profile.preference_id = preference.preference_id;
");

            migrationBuilder.Sql("DELETE FROM antag;");

            migrationBuilder.RenameColumn(
                name: "preference_id",
                table: "antag",
                newName: "profile_id");

            migrationBuilder.RenameIndex(
                name: "IX_antag_preference_id_antag_name",
                table: "antag",
                newName: "IX_antag_profile_id_antag_name");

            migrationBuilder.AddForeignKey(
                name: "FK_antag_profile_profile_id",
                table: "antag",
                column: "profile_id",
                principalTable: "profile",
                principalColumn: "profile_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.Sql(@"
INSERT INTO antag(profile_id, antag_name)
SELECT profile_id, antag_name, priority FROM profile_antag_temp;
");

            migrationBuilder.Sql("DROP TABLE profile_antag_temp;");
        }
    }
}
