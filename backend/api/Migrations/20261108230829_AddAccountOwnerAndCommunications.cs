using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ClientPulse.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountOwnerAndCommunications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountOwner",
                table: "Clients",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Communications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClientId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Communications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Communications_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: 1,
                column: "AccountOwner",
                value: "Priya Raman");

            migrationBuilder.UpdateData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: 2,
                column: "AccountOwner",
                value: "Marcus Ellery");

            migrationBuilder.UpdateData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: 3,
                column: "AccountOwner",
                value: "Dana Okafor");

            migrationBuilder.UpdateData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: 4,
                column: "AccountOwner",
                value: "Priya Raman");

            migrationBuilder.UpdateData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: 5,
                column: "AccountOwner",
                value: "Tomasz Weller");

            migrationBuilder.InsertData(
                table: "Communications",
                columns: new[] { "Id", "Body", "ClientId", "CreatedAt", "OccurredAt", "Subject", "Type" },
                values: new object[,]
                {
                    { 1, "Thanks for the forecast. We expect volumes to rise about 15% next quarter and would like to confirm capacity on the northern routes.", 1, new DateTimeOffset(new DateTime(2026, 10, 6, 14, 20, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 10, 6, 14, 20, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Q4 shipment volumes", "Email" },
                    { 2, "Client raised three late deliveries in two weeks. They said their operations lead is frustrated and asked for a written remediation plan by Friday.", 1, new DateTimeOffset(new DateTime(2026, 10, 21, 16, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 10, 21, 16, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Late deliveries follow-up", "Call" },
                    { 3, "Sent the remediation plan. No reply yet; will check in early next week.", 1, new DateTimeOffset(new DateTime(2026, 10, 28, 9, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 10, 28, 9, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Remediation plan sent", "Note" },
                    { 4, "Reviewed onboarding metrics. Client is happy with response times and wants to discuss expanding to two more clinics.", 2, new DateTimeOffset(new DateTime(2026, 9, 30, 15, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 9, 30, 15, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Quarterly business review", "Meeting" },
                    { 5, "Client requested revised pricing for the two additional clinics before their budget meeting at the end of the month.", 2, new DateTimeOffset(new DateTime(2026, 10, 14, 11, 45, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 10, 14, 11, 45, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Expansion pricing", "Email" },
                    { 6, "Client flagged that the September invoice does not match the agreed rate card. Finance is reviewing.", 3, new DateTimeOffset(new DateTime(2026, 10, 2, 13, 10, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 10, 2, 13, 10, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Invoice discrepancy", "Email" },
                    { 7, "Procurement mentioned they are evaluating other vendors ahead of renewal and asked for a competitive proposal.", 3, new DateTimeOffset(new DateTime(2026, 10, 19, 17, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 10, 19, 17, 30, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Contract renewal", "Call" },
                    { 8, "Client asked to pause the phase two rollout until the invoice issue is resolved and renewal terms are agreed.", 3, new DateTimeOffset(new DateTime(2026, 11, 3, 10, 5, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 11, 3, 10, 5, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Request to pause project", "Email" },
                    { 9, "Client confirmed staffing plans for the holiday peak. No open issues.", 4, new DateTimeOffset(new DateTime(2026, 10, 9, 8, 50, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 10, 9, 8, 50, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Holiday readiness", "Note" },
                    { 10, "Kickoff went well. Client praised the dashboard improvements and asked for weekly summaries through January.", 4, new DateTimeOffset(new DateTime(2026, 10, 23, 14, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 10, 23, 14, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Holiday campaign kickoff", "Meeting" },
                    { 11, "Client requested updated compliance documentation for their annual audit.", 5, new DateTimeOffset(new DateTime(2026, 10, 7, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 10, 7, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Compliance documentation", "Email" },
                    { 12, "Client said the delay on the compliance documents could put their audit at risk and asked for escalation to a senior contact.", 5, new DateTimeOffset(new DateTime(2026, 10, 27, 15, 15, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 10, 27, 15, 15, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Audit timeline concern", "Call" },
                    { 13, "Delivered the compliance package. Client acknowledged receipt and will confirm once their auditor has reviewed it.", 5, new DateTimeOffset(new DateTime(2026, 11, 4, 9, 40, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 11, 4, 9, 40, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Documents delivered", "Email" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Communications_ClientId_OccurredAt",
                table: "Communications",
                columns: new[] { "ClientId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Communications");

            migrationBuilder.DropColumn(
                name: "AccountOwner",
                table: "Clients");
        }
    }
}
