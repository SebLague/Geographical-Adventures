namespace GeoGame.Localization
{
	[System.Serializable]
	public struct LocalizedString
	{
		public string id;
		public string text;

		public LocalizedString(string id, string text)
		{
			this.id = id;
			this.text = text;
		}
	}


	[System.Serializable]
	public struct LocalizedStringGroup
	{
		public LocalizedString[] entries;

		public LocalizedStringGroup(LocalizedString[] entries)
		{
			this.entries = entries;
		}
	}
}