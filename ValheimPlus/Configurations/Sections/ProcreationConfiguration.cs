using ValheimPlus.GameClasses;

namespace ValheimPlus.Configurations.Sections
{
	public class ProcreationConfiguration : ServerSyncConfig<ProcreationConfiguration>
	{
		public AnimalType animalTypes { get; internal set; } = AnimalType.All;
		public bool loveInformation { get; internal set; } = false;
		public bool offspringInformation { get; internal set; } = false;
		public float requiredLovePointsMultiplier { get; internal set; } = 0f;
		public float pregnancyDurationMultiplier { get; internal set; } = 0f;
		public float pregnancyChanceMultiplier { get; internal set; } = 0f;
		public float partnerCheckRangeMultiplier { get; internal set; } = 0f;
		public bool ignoreHunger { get; internal set; } = false;
		public bool ignoreAlerted { get; internal set; } = false;
		public float creatureLimitMultiplier { get; internal set; } = 0f;
		public float maturityDurationMultiplier { get; internal set; } = 0f;
	}
}
