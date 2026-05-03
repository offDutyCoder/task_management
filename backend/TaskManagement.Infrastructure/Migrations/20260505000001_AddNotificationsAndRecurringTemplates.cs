using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationsAndRecurringTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    TaskItemId = table.Column<int>(type: "int", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_TaskItems_TaskItemId",
                        column: x => x.TaskItemId,
                        principalTable: "TaskItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecurringTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Frequency = table.Column<int>(type: "int", nullable: false),
                    WeekDays = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DayOfMonth = table.Column<int>(type: "int", nullable: true),
                    ExcludeWeekends = table.Column<bool>(type: "bit", nullable: false),
                    ExcludeHolidays = table.Column<bool>(type: "bit", nullable: false),
                    GenerationTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    DefaultStatusId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringTemplates_TaskStatuses_DefaultStatusId",
                        column: x => x.DefaultStatusId,
                        principalTable: "TaskStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecurringTemplates_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecurringTemplateAssignees",
                columns: table => new
                {
                    RecurringTemplateId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringTemplateAssignees", x => new { x.RecurringTemplateId, x.UserId });
                    table.ForeignKey(
                        name: "FK_RecurringTemplateAssignees_RecurringTemplates_RecurringTemplateId",
                        column: x => x.RecurringTemplateId,
                        principalTable: "RecurringTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecurringTemplateAssignees_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecurringTemplateLabels",
                columns: table => new
                {
                    RecurringTemplateId = table.Column<int>(type: "int", nullable: false),
                    LabelId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringTemplateLabels", x => new { x.RecurringTemplateId, x.LabelId });
                    table.ForeignKey(
                        name: "FK_RecurringTemplateLabels_Labels_LabelId",
                        column: x => x.LabelId,
                        principalTable: "Labels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecurringTemplateLabels_RecurringTemplates_RecurringTemplateId",
                        column: x => x.RecurringTemplateId,
                        principalTable: "RecurringTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecurringTemplateShares",
                columns: table => new
                {
                    RecurringTemplateId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringTemplateShares", x => new { x.RecurringTemplateId, x.UserId });
                    table.ForeignKey(
                        name: "FK_RecurringTemplateShares_RecurringTemplates_RecurringTemplateId",
                        column: x => x.RecurringTemplateId,
                        principalTable: "RecurringTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecurringTemplateShares_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TaskItemId",
                table: "Notifications",
                column: "TaskItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTemplates_IsActive",
                table: "RecurringTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTemplates_GenerationTime",
                table: "RecurringTemplates",
                column: "GenerationTime");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTemplates_DefaultStatusId",
                table: "RecurringTemplates",
                column: "DefaultStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTemplates_CreatedByUserId",
                table: "RecurringTemplates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTemplateAssignees_UserId",
                table: "RecurringTemplateAssignees",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTemplateLabels_LabelId",
                table: "RecurringTemplateLabels",
                column: "LabelId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringTemplateShares_UserId",
                table: "RecurringTemplateShares",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "RecurringTemplateAssignees");
            migrationBuilder.DropTable(name: "RecurringTemplateLabels");
            migrationBuilder.DropTable(name: "RecurringTemplateShares");
            migrationBuilder.DropTable(name: "RecurringTemplates");
            migrationBuilder.DropTable(name: "Notifications");
        }
    }
}
