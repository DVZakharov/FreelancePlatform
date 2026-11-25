using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FreelancePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeSkillAndCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NameNormalized",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "NameNormalized",
                table: "Categories");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Skills",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "citext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Skills",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AddColumn<string>(
                name: "NameNormalized",
                table: "Skills",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "citext");

            migrationBuilder.AddColumn<string>(
                name: "NameNormalized",
                table: "Categories",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
