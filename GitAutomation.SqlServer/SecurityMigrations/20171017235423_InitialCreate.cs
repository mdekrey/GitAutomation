using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace GitAutomation.SqlServer.SecurityMigrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    claimname = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.claimname);
                });

            migrationBuilder.CreateTable(
                name: "userrole",
                columns: table => new
                {
                    claimname = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    role = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_userrole", x => new { x.claimname, x.role });
                    table.ForeignKey(
                        name: "FK_UserRole_ToUser",
                        column: x => x.claimname,
                        principalTable: "user",
                        principalColumn: "claimname",
                        onDelete: ReferentialAction.Restrict);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "userrole");

            migrationBuilder.DropTable(
                name: "user");
        }
    }
}
