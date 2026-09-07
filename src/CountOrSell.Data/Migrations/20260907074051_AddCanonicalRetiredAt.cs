using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CountOrSell.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCanonicalRetiredAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "retired_at",
                table: "sets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "retired_at",
                table: "sealed_products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "retired_at",
                table: "cards",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_cards_retired_at",
                table: "cards",
                column: "retired_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_cards_retired_at",
                table: "cards");

            migrationBuilder.DropColumn(
                name: "retired_at",
                table: "sets");

            migrationBuilder.DropColumn(
                name: "retired_at",
                table: "sealed_products");

            migrationBuilder.DropColumn(
                name: "retired_at",
                table: "cards");
        }
    }
}
