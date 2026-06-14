using System;
using Bep.Infrastructure.Common.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bep.Modules.Sampling.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSampling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sampling");

            migrationBuilder.CreateTable(
                name: "muestra",
                schema: "sampling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    campana_id = table.Column<Guid>(type: "uuid", nullable: false),
                    centro_id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo_unico = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    codigo_qr = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    latitud = table.Column<double>(type: "double precision", nullable: false),
                    longitud = table.Column<double>(type: "double precision", nullable: false),
                    precision_metros = table.Column<double>(type: "double precision", nullable: true),
                    Estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FechaRegistroUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    parametros_solicitados = table.Column<string[]>(type: "text[]", nullable: false),
                    fotos = table.Column<string[]>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_muestra", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "evento_muestra",
                schema: "sampling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FechaUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UsuarioSubjectId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    MuestraId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evento_muestra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evento_muestra_muestra_MuestraId",
                        column: x => x.MuestraId,
                        principalSchema: "sampling",
                        principalTable: "muestra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "registro_custodia",
                schema: "sampling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeSubjectId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ParaSubjectId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FechaTransferenciaUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Aceptada = table.Column<bool>(type: "boolean", nullable: false),
                    FechaAceptacionUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MuestraId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registro_custodia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_registro_custodia_muestra_MuestraId",
                        column: x => x.MuestraId,
                        principalSchema: "sampling",
                        principalTable: "muestra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_evento_muestra_MuestraId",
                schema: "sampling",
                table: "evento_muestra",
                column: "MuestraId");

            migrationBuilder.CreateIndex(
                name: "IX_muestra_campana_id",
                schema: "sampling",
                table: "muestra",
                column: "campana_id");

            migrationBuilder.CreateIndex(
                name: "IX_muestra_codigo_qr",
                schema: "sampling",
                table: "muestra",
                column: "codigo_qr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_muestra_codigo_unico",
                schema: "sampling",
                table: "muestra",
                column: "codigo_unico",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_muestra_tenant_id",
                schema: "sampling",
                table: "muestra",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_registro_custodia_MuestraId",
                schema: "sampling",
                table: "registro_custodia",
                column: "MuestraId");

            // Aislamiento multi-tenant sobre la raíz del agregado (ADR-004). Las
            // tablas hijas se acceden siempre a través de 'muestra' (FK + cascada).
            migrationBuilder.Sql(RlsPolicy.Enable("sampling.muestra"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(RlsPolicy.Disable("sampling.muestra"));

            migrationBuilder.DropTable(
                name: "evento_muestra",
                schema: "sampling");

            migrationBuilder.DropTable(
                name: "registro_custodia",
                schema: "sampling");

            migrationBuilder.DropTable(
                name: "muestra",
                schema: "sampling");
        }
    }
}
