using System;
using Bep.Infrastructure.Common.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bep.Modules.Reporting.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reporting");

            migrationBuilder.CreateTable(
                name: "informe",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titulo = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    TipoEstudio = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    periodo_desde = table.Column<DateOnly>(type: "date", nullable: false),
                    periodo_hasta = table.Column<DateOnly>(type: "date", nullable: false),
                    campana_id = table.Column<Guid>(type: "uuid", nullable: true),
                    centro_id = table.Column<Guid>(type: "uuid", nullable: true),
                    autor_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreadoUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FechaAprobacionUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version_vigente_numero = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_informe", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "anexo",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    object_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FechaUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InformeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_anexo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_anexo_informe_InformeId",
                        column: x => x.InformeId,
                        principalSchema: "reporting",
                        principalTable: "informe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "comentario_interno",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    autor_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Texto = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    FechaUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InformeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_comentario_interno", x => x.Id);
                    table.ForeignKey(
                        name: "FK_comentario_interno_informe_InformeId",
                        column: x => x.InformeId,
                        principalSchema: "reporting",
                        principalTable: "informe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "version_informe",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    object_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FechaCargaUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    cargado_por_subject_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InformeId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_version_informe", x => x.Id);
                    table.ForeignKey(
                        name: "FK_version_informe_informe_InformeId",
                        column: x => x.InformeId,
                        principalSchema: "reporting",
                        principalTable: "informe",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_anexo_InformeId",
                schema: "reporting",
                table: "anexo",
                column: "InformeId");

            migrationBuilder.CreateIndex(
                name: "IX_comentario_interno_InformeId",
                schema: "reporting",
                table: "comentario_interno",
                column: "InformeId");

            migrationBuilder.CreateIndex(
                name: "IX_informe_campana_id",
                schema: "reporting",
                table: "informe",
                column: "campana_id");

            migrationBuilder.CreateIndex(
                name: "IX_informe_Estado",
                schema: "reporting",
                table: "informe",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_informe_tenant_id",
                schema: "reporting",
                table: "informe",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_version_informe_InformeId",
                schema: "reporting",
                table: "version_informe",
                column: "InformeId");

            // Aislamiento multi-tenant sobre la raíz del agregado (ADR-004). Las
            // tablas hijas se acceden siempre a través de 'informe' (FK + cascada).
            migrationBuilder.Sql(RlsPolicy.Enable("reporting.informe"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(RlsPolicy.Disable("reporting.informe"));

            migrationBuilder.DropTable(
                name: "anexo",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "comentario_interno",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "version_informe",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "informe",
                schema: "reporting");
        }
    }
}
