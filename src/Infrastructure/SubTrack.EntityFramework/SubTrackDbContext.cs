using Microsoft.EntityFrameworkCore;

namespace SubTrack.EntityFramework;

public class SubTrackDbContext(DbContextOptions<SubTrackDbContext> options) : DbContext(options)
{
}
