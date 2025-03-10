namespace ValheimPlus.Configurations.Sections
{
	public class ProcreationConfiguration : ServerSyncConfig<ProcreationConfiguration>
	{
		public string animalTypes { get; internal set; } = "all";
		public bool loveInformation { get; internal set; } = false;
		public int requiredLovePoints { get; internal set; } = 4;
		public float pregnancyDurationMultiplier { get; internal set; } = 0f;
		public float pregnancyChanceMultiplier { get; internal set; } = 0f;
		public float partnerCheckRange { get; internal set; } = 10f;
		public bool ignoreHunger { get; internal set; } = false;
		public bool ignoreAlerted { get; internal set; } = false;
		public int creatureLimit { get; internal set; } = 0;

	}
}
