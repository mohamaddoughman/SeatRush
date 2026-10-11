using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeatRush.Events.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEventStartsAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Events_StartsAt",
                schema: "events",
                table: "Events",
                column: "StartsAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_StartsAt",
                schema: "events",
                table: "Events");
        }
    }
}
