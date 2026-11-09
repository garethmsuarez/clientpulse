using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClientPulse.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAssessmentProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Effort",
                table: "Assessments",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestedModel",
                table: "Assessments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseModel",
                table: "Assessments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Effort",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "RequestedModel",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "ResponseModel",
                table: "Assessments");
        }
    }
}
