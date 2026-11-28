using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PeopleHub.Infrastructure.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class LeaveDecimalAndPartDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "Delta",
                table: "LeaveTransaction",
                type: "REAL",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<double>(
                name: "Days",
                table: "LeaveRequest",
                type: "REAL",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<double>(
                name: "Hours",
                table: "LeaveRequest",
                type: "REAL",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartOfDay",
                table: "LeaveRequest",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Used",
                table: "LeaveBalance",
                type: "REAL",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<double>(
                name: "Entitled",
                table: "LeaveBalance",
                type: "REAL",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<double>(
                name: "CarriedOver",
                table: "LeaveBalance",
                type: "REAL",
                precision: 18,
                scale: 4,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hours",
                table: "LeaveRequest");

            migrationBuilder.DropColumn(
                name: "PartOfDay",
                table: "LeaveRequest");

            migrationBuilder.AlterColumn<int>(
                name: "Delta",
                table: "LeaveTransaction",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<int>(
                name: "Days",
                table: "LeaveRequest",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<int>(
                name: "Used",
                table: "LeaveBalance",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<int>(
                name: "Entitled",
                table: "LeaveBalance",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldPrecision: 18,
                oldScale: 4);

            migrationBuilder.AlterColumn<int>(
                name: "CarriedOver",
                table: "LeaveBalance",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldPrecision: 18,
                oldScale: 4);
        }
    }
}
