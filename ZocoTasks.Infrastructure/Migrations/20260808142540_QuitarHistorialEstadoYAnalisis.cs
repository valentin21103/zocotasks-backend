using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ZocoTasks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class QuitarHistorialEstadoYAnalisis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analisis_oportunidad");

            migrationBuilder.DropTable(
                name: "historial_estado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "analisis_oportunidad",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    comercio_id = table.Column<int>(type: "integer", nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: true),
                    datos_faltantes = table.Column<string>(type: "jsonb", nullable: false),
                    es_degradado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    fecha_generacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    hash_contexto = table.Column<string>(type: "char(64)", nullable: false),
                    modelo_utilizado = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    nivel_interes = table.Column<short>(type: "smallint", nullable: false),
                    preguntas_sugeridas = table.Column<string>(type: "jsonb", nullable: false),
                    proximo_paso = table.Column<string>(type: "text", nullable: false),
                    resumen = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_analisis_oportunidad", x => x.id);
                    table.ForeignKey(
                        name: "fk_analisis_comercio",
                        column: x => x.comercio_id,
                        principalTable: "comercio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_analisis_usuario",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "historial_estado",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    comercio_id = table.Column<int>(type: "integer", nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: true),
                    estado_anterior_id = table.Column<short>(type: "smallint", nullable: true),
                    estado_nuevo_id = table.Column<short>(type: "smallint", nullable: false),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_historial_estado", x => x.id);
                    table.ForeignKey(
                        name: "fk_historial_comercio",
                        column: x => x.comercio_id,
                        principalTable: "comercio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_historial_estado_anterior",
                        column: x => x.estado_anterior_id,
                        principalTable: "estado_comercio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_historial_estado_nuevo",
                        column: x => x.estado_nuevo_id,
                        principalTable: "estado_comercio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_historial_usuario",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_analisis_comercio_fecha",
                table: "analisis_oportunidad",
                columns: new[] { "comercio_id", "fecha_generacion" });

            migrationBuilder.CreateIndex(
                name: "ix_analisis_comercio_hash",
                table: "analisis_oportunidad",
                columns: new[] { "comercio_id", "hash_contexto" });

            migrationBuilder.CreateIndex(
                name: "ix_analisis_oportunidad_usuario_id",
                table: "analisis_oportunidad",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_historial_comercio_fecha",
                table: "historial_estado",
                columns: new[] { "comercio_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_historial_estado_estado_anterior_id",
                table: "historial_estado",
                column: "estado_anterior_id");

            migrationBuilder.CreateIndex(
                name: "ix_historial_estado_estado_nuevo_id",
                table: "historial_estado",
                column: "estado_nuevo_id");

            migrationBuilder.CreateIndex(
                name: "ix_historial_estado_usuario_id",
                table: "historial_estado",
                column: "usuario_id");
        }
    }
}
