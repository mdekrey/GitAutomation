using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace GitAutomation.Postgres.BranchingMigrations
{
    public partial class AddUpstreamMergePolicy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "upstreammergepolicy",
                table: "branchgroup",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE branchgroup 
SET upstreammergepolicy =
    CASE WHEN recreatefromupstream THEN 'MergeNextIteration'
         ELSE 'None'
    END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "upstreammergepolicy",
                table: "branchgroup");
        }
    }
}
