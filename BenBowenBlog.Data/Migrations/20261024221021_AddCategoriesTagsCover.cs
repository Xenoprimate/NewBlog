using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BenBowenBlog.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriesTagsCover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Written by hand so it also works on a database that already has posts: existing posts get an
            // "Uncategorized" category (created only if needed) and an empty tag list.
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO "Categories" ("Name", "Icon")
                SELECT 'Uncategorized', 'Filled.Category'
                WHERE EXISTS (SELECT 1 FROM "Posts");
                """);

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Posts",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Posts"
                SET "CategoryId" = (SELECT "Id" FROM "Categories" WHERE "Name" = 'Uncategorized');
                """);

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "Posts",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverImage",
                table: "Posts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverImageAlt",
                table: "Posts",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "Tags",
                table: "Posts",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            // The default only exists to fill existing rows; the model has none, so drop it again.
            migrationBuilder.Sql("""ALTER TABLE "Posts" ALTER COLUMN "Tags" DROP DEFAULT;""");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_CategoryId",
                table: "Posts",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Tags",
                table: "Posts",
                column: "Tags")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Categories_CategoryId",
                table: "Posts",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Categories_CategoryId",
                table: "Posts");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Posts_CategoryId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_Tags",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "CoverImage",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "CoverImageAlt",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Posts");
        }
    }
}
