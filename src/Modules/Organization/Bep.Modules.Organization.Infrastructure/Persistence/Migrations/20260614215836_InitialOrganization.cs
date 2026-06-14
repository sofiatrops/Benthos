using System;
using Bep.Infrastructure.Common.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bep.Modules.Organization.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialOrganization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "organization");

            migrationBuilder.CreateTable(
                name: "empresa",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RazonSocial = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    rut = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Rubro = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    CreadaUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_empresa", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "centro",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CodigoInterno = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    latitud = table.Column<double>(type: "double precision", nullable: false),
                    longitud = table.Column<double>(type: "double precision", nullable: false),
                    Region = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_centro", x => x.Id);
                    table.ForeignKey(
                        name: "FK_centro_empresa_tenant_id",
                        column: x => x.tenant_id,
                        principalSchema: "organization",
                        principalTable: "empresa",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_centro_tenant_id_CodigoInterno",
                schema: "organization",
                table: "centro",
                columns: new[] { "tenant_id", "CodigoInterno" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_empresa_rut",
                schema: "organization",
                table: "empresa",
                column: "rut",
                unique: true);

            // Row-Level Security sobre la tabla tenant-scoped 'centro' (ADR-004).
            // 'empresa' es el registro global de tenants (lo administra Benthos) y
            // por tanto NO va bajo RLS.
            migrationBuilder.Sql(RlsPolicy.Enable("organization.centro"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(RlsPolicy.Disable("organization.centro"));

            migrationBuilder.DropTable(
                name: "centro",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "empresa",
                schema: "organization");
        }
    }
}
