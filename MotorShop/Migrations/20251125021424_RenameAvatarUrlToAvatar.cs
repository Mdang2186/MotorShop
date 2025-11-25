using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MotorShop.Migrations
{
    /// <inheritdoc />
    public partial class RenameAvatarUrlToAvatar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AvatarUrl",
                table: "AspNetUsers",
                newName: "Avatar");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Avatar",
                table: "AspNetUsers",
                newName: "AvatarUrl");
        }
    }
}
