using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace GitAutomation.SqlServer.BranchingMigrations
{
    public partial class RemoveRecreateFlag : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "recreatefromupstream",
                table: "branchgroup");

            migrationBuilder.AlterColumn<string>(
                name: "upstreammergepolicy",
                table: "branchgroup",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "upstreammergepolicy",
                table: "branchgroup",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "recreatefromupstream",
                table: "branchgroup",
                nullable: false,
                defaultValue: false);
        }
    }
}
