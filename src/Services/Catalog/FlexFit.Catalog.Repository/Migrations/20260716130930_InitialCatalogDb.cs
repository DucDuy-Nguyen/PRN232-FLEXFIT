using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlexFit.Catalog.Repository.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalogDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Categori__19093A0BC8C5ABA1", x => x.CategoryId);
                });

            migrationBuilder.CreateTable(
                name: "GymAmenities",
                columns: table => new
                {
                    AmenityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    AmenityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__GymAmeni__842AF50BF3A9D836", x => x.AmenityId);
                });

            migrationBuilder.CreateTable(
                name: "Gyms",
                columns: table => new
                {
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),
                    RatingAverage = table.Column<decimal>(type: "decimal(3,2)", nullable: false),
                    TotalReviews = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Gyms__1A3A7C967D8AF47B", x => x.GymId);
                });

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    District = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreditCost = table.Column<int>(type: "int", nullable: false),
                    OpenTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    CloseTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Branches__A1682FC57F628C8E", x => x.BranchId);
                    table.ForeignKey(
                        name: "FK__Branches__GymId__5812160E",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "GymId");
                });

            migrationBuilder.CreateTable(
                name: "FavoriteGyms",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoriteGyms", x => new { x.UserId, x.GymId });
                    table.ForeignKey(
                        name: "FK__FavoriteG__GymId__4B7734FF",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "GymId");
                });

            migrationBuilder.CreateTable(
                name: "GymImages",
                columns: table => new
                {
                    GymImageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    GymId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__GymImage__659DCAAE6F641A53", x => x.GymImageId);
                    table.ForeignKey(
                        name: "FK__GymImages__GymId__619B8048",
                        column: x => x.GymId,
                        principalTable: "Gyms",
                        principalColumn: "GymId");
                });

            migrationBuilder.CreateTable(
                name: "BranchAmenityMappings",
                columns: table => new
                {
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AmenityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchAmenityMappings", x => new { x.BranchId, x.AmenityId });
                    table.ForeignKey(
                        name: "FK__BranchAme__Ameni__6E01572D",
                        column: x => x.AmenityId,
                        principalTable: "GymAmenities",
                        principalColumn: "AmenityId");
                    table.ForeignKey(
                        name: "FK__BranchAme__Branc__6D0D32F4",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId");
                });

            migrationBuilder.CreateTable(
                name: "BranchImages",
                columns: table => new
                {
                    BranchImageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__BranchIm__DEDBCB2E0DD9038B", x => x.BranchImageId);
                    table.ForeignKey(
                        name: "FK__BranchIma__Branc__66603565",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BranchStaffs",
                columns: table => new
                {
                    StaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchStaffs", x => new { x.StaffId, x.BranchId });
                    table.ForeignKey(
                        name: "FK_BranchStaffs_Branches",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId");
                });

            migrationBuilder.CreateTable(
                name: "Classes",
                columns: table => new
                {
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoachName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    CreditCost = table.Column<int>(type: "int", nullable: false),
                    DifficultyLevel = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CaloriesBurnEstimate = table.Column<int>(type: "int", nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Open"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Classes__CB1927C00781F387", x => x.ClassId);
                    table.ForeignKey(
                        name: "FK__Classes__BranchI__7D439ABD",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId");
                    table.ForeignKey(
                        name: "FK__Classes__Categor__7E37BEF6",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "CategoryId");
                });

            migrationBuilder.CreateTable(
                name: "GymSessions",
                columns: table => new
                {
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    BranchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    CreditCost = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false, defaultValue: "Open"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__GymSessi__C9F492905AF95180", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK__GymSessio__Branc__778AC167",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "BranchId");
                });

            migrationBuilder.CreateTable(
                name: "ClassSchedules",
                columns: table => new
                {
                    ScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "(newid())"),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartHour = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndHour = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ClassSch__9C8A5B49CB2537F1", x => x.ScheduleId);
                    table.ForeignKey(
                        name: "FK__ClassSche__Class__02084FDA",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "ClassId");
                });

            migrationBuilder.CreateTable(
                name: "FavoriteClasses",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoriteClasses", x => new { x.UserId, x.ClassId });
                    table.ForeignKey(
                        name: "FK_FavoriteClasses_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "ClassId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BranchAmenityMappings_AmenityId",
                table: "BranchAmenityMappings",
                column: "AmenityId");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_GymId",
                table: "Branches",
                column: "GymId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchImages_BranchId",
                table: "BranchImages",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchStaffs_BranchId",
                table: "BranchStaffs",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "UQ__Categori__8517B2E053ED6742",
                table: "Categories",
                column: "CategoryName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Classes_BranchId",
                table: "Classes",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Classes_CategoryId",
                table: "Classes",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSchedules_ClassId",
                table: "ClassSchedules",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteClasses_ClassId",
                table: "FavoriteClasses",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteGyms_GymId",
                table: "FavoriteGyms",
                column: "GymId");

            migrationBuilder.CreateIndex(
                name: "UQ__GymAmeni__7B4A459F1F1C7085",
                table: "GymAmenities",
                column: "AmenityName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GymImages_GymId",
                table: "GymImages",
                column: "GymId");

            migrationBuilder.CreateIndex(
                name: "IX_GymSessions_BranchId",
                table: "GymSessions",
                column: "BranchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BranchAmenityMappings");

            migrationBuilder.DropTable(
                name: "BranchImages");

            migrationBuilder.DropTable(
                name: "BranchStaffs");

            migrationBuilder.DropTable(
                name: "ClassSchedules");

            migrationBuilder.DropTable(
                name: "FavoriteClasses");

            migrationBuilder.DropTable(
                name: "FavoriteGyms");

            migrationBuilder.DropTable(
                name: "GymImages");

            migrationBuilder.DropTable(
                name: "GymSessions");

            migrationBuilder.DropTable(
                name: "GymAmenities");

            migrationBuilder.DropTable(
                name: "Classes");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Gyms");
        }
    }
}

