
namespace BasicDeltaV
{
	public class BasicDeltaV_GameParameters : GameParameters.CustomParameterNode
	{
		[GameParameters.CustomStringParameterUI("", lines = 2, autoPersistance = false)]
		public string Reload = "A re-load is required for changes to take effect.";
		//[GameParameters.CustomParameterUI("Career Mode Restrictions", toolTip = "Data is limited based on KSC building levels", autoPersistance = true)]
		//public bool CareerRestrictions = false;
		[GameParameters.CustomParameterUI("Display info in flight mode", autoPersistance = true)]
		public bool AllowFlight = true;
		[GameParameters.CustomParameterUI("Crew Required", toolTip = "Data is limited to crewed vessels", autoPersistance = true)]
		public bool CrewRestrictions = false;
		[GameParameters.CustomParameterUI("Crew Type Restrictions", toolTip = "Data is limited based on the type of crew onboard", autoPersistance = true)]
		public bool CrewTypeRestrictions = false;
		//[GameParameters.CustomParameterUI("Crew Required", toolTip = "Data is limited to crewed vessels", autoPersistance = true)]
		//public bool CrewRequired = false;
		[GameParameters.CustomParameterUI("Crew Level Restrictions", toolTip = "Data is limited based on crew member level", autoPersistance = true)]
		public bool CrewLevelRestrictions = false;
		[GameParameters.CustomIntParameterUI("Simple Restriction Level", toolTip = "Level at or above which simple (mass, thrust, TWR) data is shown", minValue = 0, maxValue = 5, autoPersistance = true)]
		public int SimpleRestrictionLevel = 1;
		[GameParameters.CustomIntParameterUI("Complex Restriction Level", toolTip = "Level at or above which complex (deltaV, ISP, burn time) data is shown", minValue = 0, maxValue = 5, autoPersistance = true)]
		public int ComplexRestrictionLevel = 3;
        [GameParameters.CustomParameterUI("Disable Stock DeltaV", toolTip = "Disable all stock deltaV calculations; does not affect navball maneuver node information", autoPersistance = true)]
        public bool DisableStockDeltaV = true;

		public override string Title
		{
			get { return "Basic DeltaV"; }
		}

		public override string Section
		{
			get { return "Basic DeltaV"; }
		}

		public override string DisplaySection
		{
			get { return "Basic DeltaV"; }
		}

		public override int SectionOrder
		{
			get { return 0; }
		}

		public override bool HasPresets
		{
			get { return true; }
		}

		public override GameParameters.GameMode GameMode
		{
			get { return GameParameters.GameMode.ANY; }
		}

		public override void SetDifficultyPreset(GameParameters.Preset preset)
		{
			switch(preset)
			{
				case GameParameters.Preset.Easy:
				case GameParameters.Preset.Custom:
					//CareerRestrictions = false;
					AllowFlight = true;
					CrewRestrictions = false;
					CrewTypeRestrictions = false;
					//CrewRequired = false;
					CrewLevelRestrictions = false;
					SimpleRestrictionLevel = 0;
					ComplexRestrictionLevel = 1;
					break;
				case GameParameters.Preset.Normal:
					//CareerRestrictions = true;
					AllowFlight = true;
					CrewRestrictions = true;
					CrewTypeRestrictions = false;
					//CrewRequired = false;
					CrewLevelRestrictions = false;
					SimpleRestrictionLevel = 1;
					ComplexRestrictionLevel = 2;
					break;
				case GameParameters.Preset.Moderate:
					//CareerRestrictions = true;
					AllowFlight = true;
					CrewRestrictions = true;
					CrewTypeRestrictions = true;
					//CrewRequired = true;
					CrewLevelRestrictions = true;
					SimpleRestrictionLevel = 1;
					ComplexRestrictionLevel = 2;
					break;
				case GameParameters.Preset.Hard:
					//CareerRestrictions = true;
					AllowFlight = false;
					CrewRestrictions = true;
					CrewTypeRestrictions = true;
					//CrewRequired = true;
					CrewLevelRestrictions = true;
					SimpleRestrictionLevel = 2;
					ComplexRestrictionLevel = 3;
					break;
			}
		}

		public override bool Enabled(System.Reflection.MemberInfo member, GameParameters parameters)
		{
			if (member.Name == "Reload")
				return HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor;
			else if (member.Name == "CrewRestrictions")
				return AllowFlight;
			else if (member.Name == "CrewTypeRestrictions")
				return AllowFlight && CrewRestrictions;
			else if (member.Name == "CrewLevelRestrictions")
				return AllowFlight && CrewRestrictions;
			else if (member.Name == "SimpleRestrictionLevel")
				return AllowFlight && CrewRestrictions && CrewLevelRestrictions;
			else if (member.Name == "ComplexRestrictionLevel")
				return AllowFlight && CrewRestrictions && CrewLevelRestrictions;
			//else if (member.Name == "CrewRequired")
			//	return AllowFlight;

			return true;
		}

	}
}
