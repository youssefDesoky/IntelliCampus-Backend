using IntelliCampus.Domain.Entities;
using IntelliCampus.Presistence.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.Presistence.Data.DataSeeding;

public static class ExamHallSeeder
{
    public static async Task SeedExamHallsAsync(IntelliCampusDbContext context)
    {
        if (await context.ExamHalls.AnyAsync())
            return;

        var halls = new List<ExamHall>
        {
            new() { HallName = "Exam Hall 1", HallNameAr = "لجنة 1", Capacity = 50 },
            new() { HallName = "Exam Hall 2", HallNameAr = "لجنة 2", Capacity = 50 },
            new() { HallName = "Exam Hall 3", HallNameAr = "لجنة 3", Capacity = 40 },
            new() { HallName = "Exam Hall 4", HallNameAr = "لجنة 4", Capacity = 40 },
            new() { HallName = "Exam Hall 5", HallNameAr = "لجنة 5", Capacity = 30 },
            new() { HallName = "Exam Hall 6", HallNameAr = "لجنة 6", Capacity = 30 },
        };
        context.ExamHalls.AddRange(halls);
        await context.SaveChangesAsync();
    }
}
