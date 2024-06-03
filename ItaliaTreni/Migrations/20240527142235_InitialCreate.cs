using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ItaliaTreni.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Measurement",
                columns: table => new
                {
                    mmId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    p1 = table.Column<float>(type: "real", nullable: false),
                    p2 = table.Column<float>(type: "real", nullable: false),
                    p3 = table.Column<float>(type: "real", nullable: false),
                    p4 = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Measurement", x => x.mmId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Measurement");
        }
    }
}
