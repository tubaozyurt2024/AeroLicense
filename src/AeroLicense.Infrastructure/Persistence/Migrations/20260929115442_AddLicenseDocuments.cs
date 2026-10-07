using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroLicense.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLicenseDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RevocationReason",
                table: "licenses",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "license_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LicenseId = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationCode = table.Column<string>(type: "character varying(26)", maxLength: 26, nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Signature = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    KeyId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_license_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_license_documents_licenses_LicenseId",
                        column: x => x.LicenseId,
                        principalTable: "licenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_license_documents_LicenseId_IssuedAtUtc",
                table: "license_documents",
                columns: new[] { "LicenseId", "IssuedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_license_documents_VerificationCode",
                table: "license_documents",
                column: "VerificationCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "license_documents");

            migrationBuilder.DropColumn(
                name: "RevocationReason",
                table: "licenses");
        }
    }
}
