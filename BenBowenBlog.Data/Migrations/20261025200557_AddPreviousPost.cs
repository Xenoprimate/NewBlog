using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BenBowenBlog.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPreviousPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PreviousPostId",
                table: "Posts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_PreviousPostId",
                table: "Posts",
                column: "PreviousPostId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Posts_PreviousPostId",
                table: "Posts",
                column: "PreviousPostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Posts_PreviousPostId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_PreviousPostId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "PreviousPostId",
                table: "Posts");
        }
    }
}
