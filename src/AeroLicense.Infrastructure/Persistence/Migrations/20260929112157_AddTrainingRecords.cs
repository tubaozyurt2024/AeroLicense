using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroLicense.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "training_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainingOrgId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LicenseType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExamScore = table.Column<int>(type: "integer", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_records", x => x.Id);
                    table.CheckConstraint("ck_training_records_exam_score", "\"ExamScore\" BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_training_records_organizations_TrainingOrgId",
                        column: x => x.TrainingOrgId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_training_records_users_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_training_records_users_RecordedByUserId",
                        column: x => x.RecordedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_training_records_ApplicantId_LicenseType",
                table: "training_records",
                columns: new[] { "ApplicantId", "LicenseType" });

            migrationBuilder.CreateIndex(
                name: "IX_training_records_RecordedByUserId",
                table: "training_records",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_training_records_TrainingOrgId",
                table: "training_records",
                column: "TrainingOrgId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "training_records");
        }
    }
}
