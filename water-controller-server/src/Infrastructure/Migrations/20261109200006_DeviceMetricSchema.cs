using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DeviceMetricSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "device_types",
                columns: table => new
                {
                    DeviceTypeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_types", x => x.DeviceTypeId);
                });

            migrationBuilder.CreateTable(
                name: "metrics",
                columns: table => new
                {
                    MetricId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metrics", x => x.MetricId);
                });

            migrationBuilder.CreateTable(
                name: "devices",
                columns: table => new
                {
                    DeviceId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DeviceTypeId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_devices", x => x.DeviceId);
                    table.ForeignKey(
                        name: "FK_devices_device_types_DeviceTypeId",
                        column: x => x.DeviceTypeId,
                        principalTable: "device_types",
                        principalColumn: "DeviceTypeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "device_metrics",
                columns: table => new
                {
                    DeviceId = table.Column<string>(type: "character varying(100)", nullable: false),
                    MetricId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_metrics", x => new { x.DeviceId, x.MetricId });
                    table.ForeignKey(
                        name: "FK_device_metrics_devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "devices",
                        principalColumn: "DeviceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_device_metrics_metrics_MetricId",
                        column: x => x.MetricId,
                        principalTable: "metrics",
                        principalColumn: "MetricId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "measurements",
                newName: "MeasurementId");

            migrationBuilder.AddColumn<int>(
                name: "MetricId",
                table: "measurements",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Value",
                table: "measurements",
                type: "double precision",
                nullable: true);

            migrationBuilder.Sql(
                """
                INSERT INTO device_types ("Name")
                SELECT 'Flow Sensor'
                WHERE NOT EXISTS (SELECT 1 FROM device_types WHERE "Name" = 'Flow Sensor');
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO metrics ("Code", "Name", "Unit")
                SELECT 'flow-pulses', 'Flow pulses', 'pulses'
                WHERE NOT EXISTS (SELECT 1 FROM metrics WHERE "Code" = 'flow-pulses');
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO devices ("DeviceId", "DeviceTypeId", "Name")
                SELECT m."DeviceId", dt."DeviceTypeId", m."DeviceId"
                FROM (SELECT DISTINCT "DeviceId" FROM measurements) AS m
                CROSS JOIN (SELECT "DeviceTypeId" FROM device_types WHERE "Name" = 'Flow Sensor') AS dt
                WHERE NOT EXISTS (SELECT 1 FROM devices d WHERE d."DeviceId" = m."DeviceId");
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO device_metrics ("DeviceId", "MetricId")
                SELECT d."DeviceId", mt."MetricId"
                FROM devices AS d
                CROSS JOIN (SELECT "MetricId" FROM metrics WHERE "Code" = 'flow-pulses') AS mt
                WHERE NOT EXISTS (
                    SELECT 1 FROM device_metrics dm
                    WHERE dm."DeviceId" = d."DeviceId" AND dm."MetricId" = mt."MetricId"
                );
                """);

            migrationBuilder.Sql(
                """
                UPDATE measurements
                SET
                    "MetricId" = (SELECT "MetricId" FROM metrics WHERE "Code" = 'flow-pulses'),
                    "Value" = "Pulses";
                """);

            migrationBuilder.AlterColumn<int>(
                name: "MetricId",
                table: "measurements",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Value",
                table: "measurements",
                type: "double precision",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DeviceId",
                table: "measurements",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.DropColumn(
                name: "IntervalSeconds",
                table: "measurements");

            migrationBuilder.DropColumn(
                name: "Pulses",
                table: "measurements");

            migrationBuilder.CreateIndex(
                name: "IX_measurements_DeviceId_MetricId_Timestamp",
                table: "measurements",
                columns: new[] { "DeviceId", "MetricId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_measurements_MetricId",
                table: "measurements",
                column: "MetricId");

            migrationBuilder.CreateIndex(
                name: "IX_device_metrics_MetricId",
                table: "device_metrics",
                column: "MetricId");

            migrationBuilder.CreateIndex(
                name: "IX_device_types_Name",
                table: "device_types",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_devices_DeviceTypeId",
                table: "devices",
                column: "DeviceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_metrics_Code",
                table: "metrics",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_measurements_devices_DeviceId",
                table: "measurements",
                column: "DeviceId",
                principalTable: "devices",
                principalColumn: "DeviceId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_measurements_metrics_MetricId",
                table: "measurements",
                column: "MetricId",
                principalTable: "metrics",
                principalColumn: "MetricId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_measurements_devices_DeviceId",
                table: "measurements");

            migrationBuilder.DropForeignKey(
                name: "FK_measurements_metrics_MetricId",
                table: "measurements");

            migrationBuilder.DropIndex(
                name: "IX_measurements_DeviceId_MetricId_Timestamp",
                table: "measurements");

            migrationBuilder.DropIndex(
                name: "IX_measurements_MetricId",
                table: "measurements");

            migrationBuilder.AddColumn<int>(
                name: "IntervalSeconds",
                table: "measurements",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Pulses",
                table: "measurements",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE measurements
                SET
                    "Pulses" = "Value"::integer,
                    "IntervalSeconds" = 1;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "IntervalSeconds",
                table: "measurements",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Pulses",
                table: "measurements",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "Value",
                table: "measurements");

            migrationBuilder.DropColumn(
                name: "MetricId",
                table: "measurements");

            migrationBuilder.RenameColumn(
                name: "MeasurementId",
                table: "measurements",
                newName: "Id");

            migrationBuilder.AlterColumn<string>(
                name: "DeviceId",
                table: "measurements",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)");

            migrationBuilder.DropTable(
                name: "device_metrics");

            migrationBuilder.DropTable(
                name: "devices");

            migrationBuilder.DropTable(
                name: "metrics");

            migrationBuilder.DropTable(
                name: "device_types");
        }
    }
}
