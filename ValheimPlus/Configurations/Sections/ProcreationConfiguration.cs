using ValheimPlus.GameClasses;

namespace ValheimPlus.Configurations.Sections
{
	public class ProcreationConfiguration : ServerSyncConfig<ProcreationConfiguration>
	{
		public AnimalType animalTypes { get; internal set; } = AnimalType.All;
		public bool loveInformation { get; internal set; } = false;
		public bool offspringInformation { get; internal set; } = false;
		public int requiredLovePoints { get; internal set; } = 4;
		public float pregnancyDurationMultiplier { get; internal set; } = 0f;
		public float pregnancyChanceMultiplier { get; internal set; } = 0f;
		public float partnerCheckRange { get; internal set; } = 5;
		public bool ignoreHunger { get; internal set; } = false;
		public bool ignoreAlerted { get; internal set; } = false;
		public int creatureLimit { get; internal set; } = 4;
		public float maturityDurationMultiplier { get; internal set; } = 0f;
	}
}
