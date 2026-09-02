using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElectronicService.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AllowPendingCatalogRecognitionFeedbackDecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_recognition_feedback_lifecycle",
                table: "catalog_recognition_feedback");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_recognition_feedback_lifecycle",
                table: "catalog_recognition_feedback",
                sql: "(\"status\" = 'Pending' AND \"feedback_type\" <> 'None' AND \"label_quality\" = 'None' AND \"reviewed_by_user_id\" IS NULL AND \"reviewer_role\" IS NULL AND \"finalized_at_utc\" IS NULL AND \"is_training_eligible\" = FALSE) OR (\"status\" = 'Finalized' AND \"feedback_type\" <> 'None' AND \"label_quality\" <> 'None' AND \"reviewed_by_user_id\" IS NOT NULL AND \"reviewer_role\" IS NOT NULL AND \"finalized_at_utc\" IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_recognition_feedback_lifecycle",
                table: "catalog_recognition_feedback");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_recognition_feedback_lifecycle",
                table: "catalog_recognition_feedback",
                sql: "(\"status\" = 'Pending' AND \"feedback_type\" = 'None' AND \"label_quality\" = 'None' AND \"final_normalized_value\" IS NULL AND \"reviewed_by_user_id\" IS NULL AND \"reviewer_role\" IS NULL AND \"finalized_at_utc\" IS NULL AND \"is_training_eligible\" = FALSE) OR (\"status\" = 'Finalized' AND \"feedback_type\" <> 'None' AND \"label_quality\" <> 'None' AND \"reviewed_by_user_id\" IS NOT NULL AND \"reviewer_role\" IS NOT NULL AND \"finalized_at_utc\" IS NOT NULL)");
        }
    }
}
