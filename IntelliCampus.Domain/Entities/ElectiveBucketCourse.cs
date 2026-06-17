namespace IntelliCampus.Domain.Entities;

public class ElectiveBucketCourse
{
    public int ElectiveBucketId { get; set; }
    public int CourseId { get; set; }

    public ElectiveBucket ElectiveBucket { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
