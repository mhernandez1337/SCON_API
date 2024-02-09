global using Microsoft.EntityFrameworkCore;

namespace WebSupplementalAPI
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {

        }

        public DbSet<AdvancedOpinion> advanceopinions => Set<AdvancedOpinion>();
        public DbSet<AdminOrder> adminorders => Set<AdminOrder>();
        public DbSet<AgingSubmittedCases> agingcases => Set<AgingSubmittedCases>();
        public DbSet<COAUnpublishedOrder> coaunpublishedorders => Set<COAUnpublishedOrder>();
        public DbSet<UnpublishedOrder> unpublishedorders => Set<UnpublishedOrder>();
        public DbSet<COAOralArgument> coaoralarguements => Set<COAOralArgument>();
        public DbSet<OralArgument> oralArgCalendar => Set<OralArgument>();
        public DbSet<Statistics> statistics => Set<Statistics>();
        public DbSet<Courts> courts => Set<Courts>();
        public DbSet<County> counties=> Set<County>();
        public DbSet<CourtJudHistory> jhcourts => Set<CourtJudHistory>();
        public DbSet<Department> departments=> Set<Department>();
        public DbSet<JudicialPosition> positions=> Set<JudicialPosition>();
        public DbSet<JudicialHistoryData> historyData => Set<JudicialHistoryData>();
        public DbSet<Mapbox> mapboxes => Set<Mapbox>();
        public DbSet<FindACourt> findacourt => Set<FindACourt>();

    }
}
