using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ItaliaTreni.Migrations
{
    /// <inheritdoc />
    public partial class AddOutOfScale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutOfScaleMeasurement",
                columns: table => new
                {
                    mmId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    mm_fine = table.Column<int>(type: "int", nullable: false),
                    e1 = table.Column<string>(type: "real", nullable: false),
                    s1 = table.Column<string>(type: "real", nullable: false),
                    e2 = table.Column<string>(type: "real", nullable: false),
                    s2 = table.Column<string>(type: "real", nullable: false),
                    e3 = table.Column<string>(type: "real", nullable: false),
                    s3 = table.Column<string>(type: "real", nullable: false),
                    e4 = table.Column<string>(type: "real", nullable: false),
                    s4 = table.Column<string>(type: "real", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutOfScaleMeasurement", x => x.mmId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutOfScaleMeasurement");
        }
    }
}
