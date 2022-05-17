using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CreditsMenu : Menu
{
	public TextAsset contributersFile;
	public TMP_Text text;
	public Color linkCol;
	public Color linkHoverCol;
	public OpenHyperlinks hyperlinkOpener;

	const string youtubeVideoLink = "https://www.youtube.com/watch?v=sLqXFF8mlEU&list=PLFt_AvWsXl0dT82XMtKATYPcVIhpu2fh6&index=1";
	const string githubLink = "https://github.com/SebLague/Geographical-Adventures";
	const string naturalEarthDataLink = "https://www.naturalearthdata.com/downloads/50m-cultural-vectors/";
	const string nasaVisibleEarthLink = "https://visibleearth.nasa.gov/images/73934/topography";
	const string flagsLink = "https://flagpedia.net/";
	const string starDataLink = "https://github.com/astronexus/HYG-Database";
	const string earthObservatoryLink = "https://earthobservatory.nasa.gov/features/NightLights";


	void Start()
	{
		Refresh();
	}

	[NaughtyAttributes.Button()]
	void Refresh()
	{
		hyperlinkOpener.hoverColor = linkHoverCol;
		text.text = "";

		AddText(SetColour("Created by Sebastian Lague.", Color.white));
		AddLineBreak();
		//	AddLineBreak(2);
		AddText("If you're interested in how the game was made, you can find a series of videos about its development on ");
		AddText(CreateHyperlink("YouTube", youtubeVideoLink));
		AddText(". The code for this project is also available on " + CreateHyperlink("GitHub", githubLink) + ".");
		AddLineBreak(2);

		AddLine(SetColour("The game was made with data from the following sources:", Color.white));
		AddLine(CreateHyperlink("Natural Earth Data", naturalEarthDataLink) + " country shape data and city locations.");
		AddLine(CreateHyperlink("NASA Visible Earth", nasaVisibleEarthLink) + " topography and land colour maps.");
		AddLine(CreateHyperlink("NASA Earth Observatory", earthObservatoryLink) + " night-time city light map.");
		AddLine(CreateHyperlink("Flagpedia", flagsLink) + " country flag images.");
		AddLine(CreateHyperlink("Astronexus", starDataLink) + " star data.");
		AddLineBreak();

		AddLine(SetColour("Thanks to the following artists for the music (from Artlist and SoundStripe):", Color.white));
		AddLine("Veaceslav Draganov");
		AddLine("Gray North");
		AddLine("Jan Baars");
		AddLine("The Stolen Orchestra");
		AddLineBreak();

		AddText(SetColour("A huge thanks to the following people for contributing various bug fixes, features, and translations to the project on GitHub:", Color.white));
		AddLineBreak();

		string[] contributorNames = contributersFile.text.Split(',');
		for (int i = 0; i < contributorNames.Length; i++)
		{
			bool isLast = i == contributorNames.Length - 1;
			string contributorName = contributorNames[i].Trim();
			AddText(contributorName + (isLast ? "." : ", "));
		}
	}

	string CreateHyperlink(string displayText, string link)
	{
		return SetColour($"<link=\"{link}\">{displayText}</link>", linkCol);
	}

	void AddLine(string textString)
	{
		AddText(textString + "\n");
	}
	void AddText(string textString)
	{
		text.text += textString;
	}

	void AddLineBreak(int num = 1)
	{
		for (int i = 0; i < num; i++)
		{
			text.text += "\n";
		}
	}

	string SetColour(string text, Color colour)
	{
		return $"<color=#{ColorUtility.ToHtmlStringRGB(colour)}>{text}</color>";
	}

	void OnValidate()
	{
		if (!Application.isPlaying)
		{
			Refresh();
		}
	}
}
