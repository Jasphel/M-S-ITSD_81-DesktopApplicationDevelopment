using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquipmentBorrowing.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLab4SchemaAndConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactInfo",
                table: "Students",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentNumber",
                table: "Students",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Equipment",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Identifier",
                table: "Equipment",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualReturnDate",
                table: "Borrowings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_StudentNumber",
                table: "Students",
                column: "StudentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipment_Identifier",
                table: "Equipment",
                column: "Identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Borrowings_EquipmentId",
                table: "Borrowings",
                column: "EquipmentId",
                unique: true,
                filter: "\"Status\" = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Borrowings_StudentId",
                table: "Borrowings",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Borrowings_Equipment_EquipmentId",
                table: "Borrowings",
                column: "EquipmentId",
                principalTable: "Equipment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Borrowings_Students_StudentId",
                table: "Borrowings",
                column: "StudentId",
                principalTable: "Students",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Borrowings_Equipment_EquipmentId",
                table: "Borrowings");

            migrationBuilder.DropForeignKey(
                name: "FK_Borrowings_Students_StudentId",
                table: "Borrowings");

            migrationBuilder.DropIndex(
                name: "IX_Students_StudentNumber",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Equipment_Identifier",
                table: "Equipment");

            migrationBuilder.DropIndex(
                name: "IX_Borrowings_EquipmentId",
                table: "Borrowings");

            migrationBuilder.DropIndex(
                name: "IX_Borrowings_StudentId",
                table: "Borrowings");

            migrationBuilder.DropColumn(
                name: "ContactInfo",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "StudentNumber",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "Identifier",
                table: "Equipment");

            migrationBuilder.DropColumn(
                name: "ActualReturnDate",
                table: "Borrowings");
        }
    }
}
