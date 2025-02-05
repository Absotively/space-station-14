using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class MoveRoleOptionsFromProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

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

            /* Manually drop & recreate 'job' instead of letting EF do it automatically.
             * 
             * SQLite does not support renaming columns or modifying foreign key constraints on
             * existing columns. EF apparently handles this by dropping & recreating tables
             * automatically, if necessary, **after the rest of the migration is done.**
             * 
             * The updated data needs to go into the table **during** the migration. It will almost
             * certainly fail if the old foreign key is secretly still active, despite
             * DropForeignKey having already been called, because the table rebuild hasn't actually
             * happened yet.
             * 
             * There is probably a better workaround than this but I haven't found it. */

            migrationBuilder.DropTable(name: "job");

            migrationBuilder.CreateTable(
                name: "job",
                columns: table => new
                {
                    job_id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    preference_id = table.Column<int>(nullable: false),
                    job_name = table.Column<string>(nullable: false),
                    priority = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job", x => x.job_id);
                    table.ForeignKey(
                        name: "FK_job_preference_preference_id",
                        column: x => x.preference_id,
                        principalTable: "preference",
                        principalColumn: "preference_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_preference_id",
                table: "job",
                column: "preference_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_one_high_priority",
                table: "job",
                column: "preference_id",
                unique: true,
                filter: "priority = 3");

            migrationBuilder.CreateIndex(
                name: "IX_job_preference_id_job_name",
                table: "job",
                columns: new[] { "preference_id", "job_name" },
                unique: true);

            migrationBuilder.Sql(@"
INSERT INTO job(preference_id, job_name, priority)
SELECT preference_id, job_name, priority FROM preference_job_temp;
");

            migrationBuilder.Sql("DROP TABLE preference_job_temp;");

            /* Combine each player's antag preferences across all their profiles */
            migrationBuilder.Sql(@"
CREATE TEMP TABLE preference_antag_temp
AS SELECT DISTINCT preference_id, antag_name
  FROM antag
    JOIN profile ON antag.profile_id = profile.profile_id;
");

            /* Manually drop & recreate 'antag' for the same reason as 'job' */

            migrationBuilder.DropTable(name: "antag");

            migrationBuilder.CreateTable(
                name: "antag",
                columns: table => new
                {
                    antag_id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    preference_id = table.Column<int>(nullable: false),
                    antag_name = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_antag", x => x.antag_id);
                    table.ForeignKey(
                        name: "FK_antag_preference_preference_id",
                        column: x => x.preference_id,
                        principalTable: "preference",
                        principalColumn: "preference_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(@"
INSERT INTO antag(preference_id, antag_name)
SELECT preference_id, antag_name FROM preference_antag_temp;
");

            migrationBuilder.Sql("DROP TABLE preference_antag_temp;");

            migrationBuilder.AddColumn<int>(
                name: "pref_unavailable",
                table: "preference",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            /* Keep pref_unavailable setting from each player's currently selected profile */
            migrationBuilder.Sql(@"
UPDATE preference
  SET pref_unavailable = profile.pref_unavailable
  FROM profile
  WHERE profile.preference_id = preference.preference_id
    AND profile.slot = preference.selected_character_slot;
");

            migrationBuilder.DropColumn(
                name: "pref_unavailable",
                table: "profile");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            /* For lack of better ideas, copy each preference's role options to each
             * of its profiles */

            migrationBuilder.AddColumn<int>(
                name: "pref_unavailable",
                table: "profile",
                type: "INTEGER",
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
