using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bep.Modules.Audit.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.CreateTable(
                name: "audit_log",
                schema: "audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EventType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorSubjectId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_EventType",
                schema: "audit",
                table: "audit_log",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_OccurredOnUtc",
                schema: "audit",
                table: "audit_log",
                column: "OccurredOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_TenantId",
                schema: "audit",
                table: "audit_log",
                column: "TenantId");

            // Inmutabilidad reforzada en la base de datos (RF-08-007): un trigger
            // bloquea cualquier UPDATE o DELETE, incluso del propietario de la tabla.
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION audit.prevent_mutation() RETURNS trigger AS $$
                BEGIN
                    RAISE EXCEPTION 'Los registros de auditoría son inmutables (RF-08-007).';
                END;
                $$ LANGUAGE plpgsql;

                CREATE TRIGGER audit_log_no_update_delete
                    BEFORE UPDATE OR DELETE ON audit.audit_log
                    FOR EACH ROW EXECUTE FUNCTION audit.prevent_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS audit_log_no_update_delete ON audit.audit_log;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS audit.prevent_mutation();");

            migrationBuilder.DropTable(
                name: "audit_log",
                schema: "audit");
        }
    }
}
