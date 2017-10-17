using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace GitAutomation.Postgres.BranchingMigrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "branchgroup",
                columns: table => new
                {
                    groupname = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    branchtype = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false, defaultValueSql: "('Feature')"),
                    recreatefromupstream = table.Column<bool>(type: "bool", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branchgroup", x => x.groupname);
                });

            migrationBuilder.CreateTable(
                name: "branchstream",
                columns: table => new
                {
                    downstreambranch = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    upstreambranch = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branchstream", x => new { x.downstreambranch, x.upstreambranch });
                    table.ForeignKey(
                        name: "FK_BranchStream_ToDownstreamBranch",
                        column: x => x.downstreambranch,
                        principalTable: "branchgroup",
                        principalColumn: "groupname",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BranchStream_ToUpstreamBranch",
                        column: x => x.upstreambranch,
                        principalTable: "branchgroup",
                        principalColumn: "groupname",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_branchstream_upstreambranch",
                table: "branchstream",
                column: "upstreambranch");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branchstream");

            migrationBuilder.DropTable(
                name: "branchgroup");
        }
    }
}
