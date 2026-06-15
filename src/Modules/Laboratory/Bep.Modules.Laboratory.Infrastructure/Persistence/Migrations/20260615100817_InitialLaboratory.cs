using System;
using Bep.Infrastructure.Common.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bep.Modules.Laboratory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialLaboratory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "laboratory");

            migrationBuilder.CreateTable(
                name: "lote_resultados",
                schema: "laboratory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campana_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Laboratorio = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    archivo_object_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    recibido_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    validado_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    motivo_rechazo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lote_resultados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "resultado_parametro",
                schema: "laboratory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo_muestra = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Parametro = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Valor = table.Column<double>(type: "double precision", nullable: false),
                    Unidad = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Metodo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    LoteResultadosId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resultado_parametro", x => x.Id);
                    table.ForeignKey(
                        name: "FK_resultado_parametro_lote_resultados_LoteResultadosId",
                        column: x => x.LoteResultadosId,
                        principalSchema: "laboratory",
                        principalTable: "lote_resultados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_lote_resultados_campana_id",
                schema: "laboratory",
                table: "lote_resultados",
                column: "campana_id");

            migrationBuilder.CreateIndex(
                name: "IX_lote_resultados_Estado",
                schema: "laboratory",
                table: "lote_resultados",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_lote_resultados_tenant_id",
                schema: "laboratory",
                table: "lote_resultados",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_resultado_parametro_codigo_muestra",
                schema: "laboratory",
                table: "resultado_parametro",
                column: "codigo_muestra");

            migrationBuilder.CreateIndex(
                name: "IX_resultado_parametro_LoteResultadosId",
                schema: "laboratory",
                table: "resultado_parametro",
                column: "LoteResultadosId");

            // Aislamiento multi-tenant sobre la raíz del agregado (ADR-004). La tabla
            // hija 'resultado_parametro' se accede siempre a través de 'lote_resultados'.
            migrationBuilder.Sql(RlsPolicy.Enable("laboratory.lote_resultados"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(RlsPolicy.Disable("laboratory.lote_resultados"));

            migrationBuilder.DropTable(
                name: "resultado_parametro",
                schema: "laboratory");

            migrationBuilder.DropTable(
                name: "lote_resultados",
                schema: "laboratory");
        }
    }
}
