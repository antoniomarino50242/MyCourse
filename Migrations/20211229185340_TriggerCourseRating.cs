﻿using Microsoft.EntityFrameworkCore.Migrations;

namespace MyCourse.Migrations
{
    public partial class TriggerCourseRating : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE TRIGGER SubscriptionSetCourseOnUpdate
                                AFTER UPDATE
                                ON Subscriptions
                                WHEN NEW.Vote <> OLD.Vote
                                BEGIN
                                UPDATE Courses SET Rating = ( SELECT AVG(Vote) FROM Subscriptions WHERE CourseId = NEW.CourseId ) WHERE Id = NEW.CourseId;
                                END;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER SubscriptionSetCourseOnUpdate");
        }
    }
}
