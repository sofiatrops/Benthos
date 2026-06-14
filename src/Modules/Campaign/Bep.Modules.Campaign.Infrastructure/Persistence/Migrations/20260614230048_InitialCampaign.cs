using System;
using Bep.Infrastructure.Common.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bep.Modules.Campaign.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCampaign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "campaign");

            migrationBuilder.CreateTable(
                name: "campania",
                schema: "campaign",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    fecha_inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_fin = table.Column<DateOnly>(type: "date", nullable: false),
                    Estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    centro_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    responsables = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_campania", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_campania_Estado",
                schema: "campaign",
                table: "campania",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_campania_tenant_id",
                schema: "campaign",
                table: "campania",
                column: "tenant_id");

            // Aislamiento multi-tenant a nivel de base de datos (ADR-004).
            migrationBuilder.Sql(RlsPolicy.Enable("campaign.campania"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(RlsPolicy.Disable("campaign.campania"));

            migrationBuilder.DropTable(
                name: "campania",
                schema: "campaign");
        }
    }
}
