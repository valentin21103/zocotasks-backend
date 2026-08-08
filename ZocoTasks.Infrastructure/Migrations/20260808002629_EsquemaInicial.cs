using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ZocoTasks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EsquemaInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "estado_comercio",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false),
                    codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    orden = table.Column<short>(type: "smallint", nullable: false),
                    es_final = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_estado_comercio", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rol",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rol", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rubro",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rubro", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tipo_interaccion",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false),
                    codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tipo_interaccion", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuario",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "citext", nullable: false),
                    password_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    nombre_completo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    fecha_actualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuario", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    entidad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entidad_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    accion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: true),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    cambios = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_log", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_log_usuario",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "comercio",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre_comercial = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    cuit = table.Column<string>(type: "char(11)", nullable: false),
                    nombre_contacto = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    telefono = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    email = table.Column<string>(type: "citext", nullable: true),
                    rubro_id = table.Column<int>(type: "integer", nullable: false),
                    estado_id = table.Column<short>(type: "smallint", nullable: false),
                    usuario_asignado_id = table.Column<int>(type: "integer", nullable: true),
                    notas = table.Column<string>(type: "text", nullable: true),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    fecha_actualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fecha_eliminacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    search_vector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true, computedColumnSql: "to_tsvector('spanish',\n    coalesce(nombre_comercial, '') || ' ' ||\n    coalesce(nombre_contacto, '')  || ' ' ||\n    coalesce(notas, ''))", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_comercio", x => x.id);
                    table.ForeignKey(
                        name: "fk_comercio_estado",
                        column: x => x.estado_id,
                        principalTable: "estado_comercio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_comercio_rubro",
                        column: x => x.rubro_id,
                        principalTable: "rubro",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_comercio_usuario_asignado",
                        column: x => x.usuario_asignado_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "usuario_rol",
                columns: table => new
                {
                    usuario_id = table.Column<int>(type: "integer", nullable: false),
                    rol_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuario_rol", x => new { x.usuario_id, x.rol_id });
                    table.ForeignKey(
                        name: "fk_usuario_rol_rol",
                        column: x => x.rol_id,
                        principalTable: "rol",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_usuario_rol_usuario",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "analisis_oportunidad",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    comercio_id = table.Column<int>(type: "integer", nullable: false),
                    nivel_interes = table.Column<short>(type: "smallint", nullable: false),
                    resumen = table.Column<string>(type: "text", nullable: false),
                    proximo_paso = table.Column<string>(type: "text", nullable: false),
                    preguntas_sugeridas = table.Column<string>(type: "jsonb", nullable: false),
                    datos_faltantes = table.Column<string>(type: "jsonb", nullable: false),
                    modelo_utilizado = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    hash_contexto = table.Column<string>(type: "char(64)", nullable: false),
                    fecha_generacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    usuario_id = table.Column<int>(type: "integer", nullable: true),
                    es_degradado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
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
                    estado_anterior_id = table.Column<short>(type: "smallint", nullable: true),
                    estado_nuevo_id = table.Column<short>(type: "smallint", nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: true),
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

            migrationBuilder.CreateTable(
                name: "interaccion",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    comercio_id = table.Column<int>(type: "integer", nullable: false),
                    tipo_id = table.Column<short>(type: "smallint", nullable: false),
                    usuario_id = table.Column<int>(type: "integer", nullable: true),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    detalle = table.Column<string>(type: "text", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interaccion", x => x.id);
                    table.ForeignKey(
                        name: "fk_interaccion_comercio",
                        column: x => x.comercio_id,
                        principalTable: "comercio",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_interaccion_tipo",
                        column: x => x.tipo_id,
                        principalTable: "tipo_interaccion",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_interaccion_usuario",
                        column: x => x.usuario_id,
                        principalTable: "usuario",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "estado_comercio",
                columns: new[] { "id", "codigo", "es_final", "nombre", "orden" },
                values: new object[,]
                {
                    { (short)1, "Nuevo", false, "Nuevo", (short)1 },
                    { (short)2, "Contactado", false, "Contactado", (short)2 },
                    { (short)3, "Interesado", false, "Interesado", (short)3 },
                    { (short)4, "Documentacion", false, "Documentación", (short)4 },
                    { (short)5, "Aprobado", true, "Aprobado", (short)5 },
                    { (short)6, "Rechazado", true, "Rechazado", (short)6 }
                });

            migrationBuilder.InsertData(
                table: "rol",
                columns: new[] { "id", "nombre" },
                values: new object[,]
                {
                    { 1, "Admin" },
                    { 2, "Vendedor" }
                });

            migrationBuilder.InsertData(
                table: "rubro",
                columns: new[] { "id", "activo", "nombre" },
                values: new object[,]
                {
                    { 1, true, "Gastronomía" },
                    { 2, true, "Indumentaria" },
                    { 3, true, "Kiosco y autoservicio" },
                    { 4, true, "Salud y estética" },
                    { 5, true, "Servicios profesionales" },
                    { 6, true, "Tecnología" },
                    { 7, true, "Transporte y logística" },
                    { 8, true, "Educación" },
                    { 9, true, "Otros" }
                });

            migrationBuilder.InsertData(
                table: "tipo_interaccion",
                columns: new[] { "id", "codigo", "nombre" },
                values: new object[,]
                {
                    { (short)1, "Llamada", "Llamada" },
                    { (short)2, "WhatsApp", "WhatsApp" },
                    { (short)3, "Reunion", "Reunión" },
                    { (short)4, "Email", "Email" },
                    { (short)5, "NotaInterna", "Nota interna" }
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
                name: "ix_audit_log_entidad",
                table: "audit_log",
                columns: new[] { "entidad", "entidad_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_fecha",
                table: "audit_log",
                column: "fecha");

            migrationBuilder.CreateIndex(
                name: "ix_audit_log_usuario_id",
                table: "audit_log",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_comercio_estado",
                table: "comercio",
                column: "estado_id");

            migrationBuilder.CreateIndex(
                name: "ix_comercio_fecha_creacion",
                table: "comercio",
                column: "fecha_creacion");

            migrationBuilder.CreateIndex(
                name: "ix_comercio_rubro",
                table: "comercio",
                column: "rubro_id");

            migrationBuilder.CreateIndex(
                name: "ix_comercio_search_vector",
                table: "comercio",
                column: "search_vector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "ix_comercio_usuario_asignado_id",
                table: "comercio",
                column: "usuario_asignado_id");

            migrationBuilder.CreateIndex(
                name: "ux_comercio_cuit",
                table: "comercio",
                column: "cuit",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_estado_comercio_codigo",
                table: "estado_comercio",
                column: "codigo",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "ix_interaccion_comercio_fecha",
                table: "interaccion",
                columns: new[] { "comercio_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_interaccion_tipo_id",
                table: "interaccion",
                column: "tipo_id");

            migrationBuilder.CreateIndex(
                name: "ix_interaccion_usuario_id",
                table: "interaccion",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ux_rol_nombre",
                table: "rol",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_rubro_nombre",
                table: "rubro",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_tipo_interaccion_codigo",
                table: "tipo_interaccion",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_usuario_email",
                table: "usuario",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuario_rol_rol_id",
                table: "usuario_rol",
                column: "rol_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analisis_oportunidad");

            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "historial_estado");

            migrationBuilder.DropTable(
                name: "interaccion");

            migrationBuilder.DropTable(
                name: "usuario_rol");

            migrationBuilder.DropTable(
                name: "comercio");

            migrationBuilder.DropTable(
                name: "tipo_interaccion");

            migrationBuilder.DropTable(
                name: "rol");

            migrationBuilder.DropTable(
                name: "estado_comercio");

            migrationBuilder.DropTable(
                name: "rubro");

            migrationBuilder.DropTable(
                name: "usuario");
        }
    }
}
