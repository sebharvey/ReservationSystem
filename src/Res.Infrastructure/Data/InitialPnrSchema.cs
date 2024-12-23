using Microsoft.EntityFrameworkCore.Migrations;

namespace Res.Infrastructure.Data;

public class InitialPnrSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "res");

        migrationBuilder.CreateTable(
            name: "Pnrs",
            schema: "res",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                RecordLocator = table.Column<string>(maxLength: 6, nullable: false),
                JsonData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                UpdatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Pnrs", x => x.RecordLocator);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Pnrs_CreatedAt",
            schema: "res",
            table: "Pnrs",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_Pnrs_UpdatedAt",
            schema: "res",
            table: "Pnrs",
            column: "UpdatedAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Pnrs", schema: "res");
    }
}