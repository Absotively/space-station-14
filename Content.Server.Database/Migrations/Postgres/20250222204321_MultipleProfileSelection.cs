using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class MultipleProfileSelection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "round_start_candidate",
                table: "profile",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
UPDATE profile
SET round_start_candidate = true
FROM preference
  WHERE preference.preference_id = profile.preference_id
    AND preference.selected_character_slot = profile.slot;
");

            migrationBuilder.AddColumn<string>(
                name: "highest_priority_job",
                table: "preference",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "round_start_candidate",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "highest_priority_job",
                table: "preference");
        }
    }
}
