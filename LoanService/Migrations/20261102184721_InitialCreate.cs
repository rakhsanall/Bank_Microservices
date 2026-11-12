using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoanService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoanDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    CustomerId = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    FileName = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    FilePath = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanDocuments", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoanDocuments");
        }
    }
}
