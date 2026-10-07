using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroLicense.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditCorrelationAndImmutability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "audit_logs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            // Derinlemesine savunma: uygulamayı atlayıp doğrudan SQL ile bağlanan biri de (DBA, sızmış
            // bağlantı bilgisi) audit kaydını değiştiremesin/silemesin. Elle eklendi; EF trigger üretmez.
            migrationBuilder.Sql("""
                CREATE FUNCTION audit_logs_append_only() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'audit_logs append-only: % izin verilmez', TG_OP;
                END;
                $$;

                CREATE TRIGGER trg_audit_logs_append_only
                    BEFORE UPDATE OR DELETE ON audit_logs
                    FOR EACH ROW EXECUTE FUNCTION audit_logs_append_only();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER trg_audit_logs_append_only ON audit_logs;
                DROP FUNCTION audit_logs_append_only();
                """);

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "audit_logs");
        }
    }
}
