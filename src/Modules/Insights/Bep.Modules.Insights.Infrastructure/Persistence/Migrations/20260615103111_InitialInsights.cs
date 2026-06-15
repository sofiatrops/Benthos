using System;
using Bep.Infrastructure.Common.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bep.Modules.Insights.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialInsights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "insights");

            migrationBuilder.CreateTable(
                name: "analisis_ambiental",
                schema: "insights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campana_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Resumen = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    Modelo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    generado_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    validado_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    validado_por_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    motivo_descarte = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analisis_ambiental", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "hallazgo",
                schema: "insights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Parametro = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Severidad = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Detalle = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AnalisisAmbientalId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hallazgo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hallazgo_analisis_ambiental_AnalisisAmbientalId",
                        column: x => x.AnalisisAmbientalId,
                        principalSchema: "insights",
                        principalTable: "analisis_ambiental",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_analisis_ambiental_campana_id",
                schema: "insights",
                table: "analisis_ambiental",
                column: "campana_id");

            migrationBuilder.CreateIndex(
                name: "IX_analisis_ambiental_Estado",
                schema: "insights",
                table: "analisis_ambiental",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_analisis_ambiental_tenant_id",
                schema: "insights",
                table: "analisis_ambiental",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_hallazgo_AnalisisAmbientalId",
                schema: "insights",
                table: "hallazgo",
                column: "AnalisisAmbientalId");

            // Aislamiento multi-tenant sobre la raíz del agregado (ADR-004).
            migrationBuilder.Sql(RlsPolicy.Enable("insights.analisis_ambiental"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(RlsPolicy.Disable("insights.analisis_ambiental"));

            migrationBuilder.DropTable(
                name: "hallazgo",
                schema: "insights");

            migrationBuilder.DropTable(
                name: "analisis_ambiental",
                schema: "insights");
        }
    }
}
