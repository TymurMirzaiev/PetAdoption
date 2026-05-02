namespace PetAdoption.PetService.Application.Options;

public class DiscoverOptions
{
    public bool RankingEnabled { get; set; } = false;
    public int CandidatePoolMultiplier { get; set; } = 10;
    public int CandidatePoolCap { get; set; } = 100;
    public double FavoriteWeight { get; set; } = 1.0;
    public double SkipWeight { get; set; } = -0.5;
    public double PetTypeBonus { get; set; } = 0.10;
    public double AgeBucketBonus { get; set; } = 0.05;
}
