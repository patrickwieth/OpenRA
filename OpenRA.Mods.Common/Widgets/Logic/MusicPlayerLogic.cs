#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class MusicPlayerLogic : ChromeLogic
	{
		[FluentReference]
		const string SoundMuted = "label-sound-muted";

		[FluentReference]
		const string NoSongPlaying = "label-no-song-playing";

		readonly ScrollPanelWidget musicList;
		readonly ScrollItemWidget itemTemplate;

		readonly MusicPlaylist musicPlaylist;
		MusicInfo currentSong = null;
		bool noSongsForMode;

		readonly Widget panel;
		readonly Widget customFilterPanel;
		readonly Widget labelContainer;
		readonly Widget buttonsPanel;
		readonly LabelWidget timeLabelWidget;
		readonly LabelWidget titleLabelWidget;
		readonly CheckboxWidget shuffleWidget;
		readonly CheckboxWidget repeatWidget;
		readonly LabelWidget volumeLabelWidget;
		readonly SliderWidget musicSliderWidget;
		readonly Widget noMusicContainer;
		readonly LabelWidget muteLabelWidget;
		readonly Widget modePanel;
		readonly ButtonWidget backButtonWidget;
		readonly LabelWidget noSongsLabelWidget;

		readonly List<(Widget Widget, int BaseY)> customDownTargets = new();
		readonly List<(Widget Widget, int BaseY)> customRaiseTargets = new();
		readonly List<(Widget Widget, int BaseY)> customLowerTargets = new();

		const int CustomDownOffset = 200;
		const int CustomUpOffset = 150;
		const int CustomLowerOffset = 50;

		readonly int basePanelHeight;
		readonly int expandedPanelHeight;
		readonly int collapsedPanelHeight;
		readonly int panelCenterY;
		readonly bool allowPanelResize;
		readonly int customLayoutOffset;

		bool showCustomFilters;

		[ObjectCreator.UseCtor]
		public MusicPlayerLogic(Widget widget, World world, ModData modData, Action onExit)
		{
			panel = widget;

			musicList = panel.Get<ScrollPanelWidget>("MUSIC_LIST");
			itemTemplate = musicList.Get<ScrollItemWidget>("MUSIC_TEMPLATE");
			musicPlaylist = world.WorldActor.Trait<MusicPlaylist>();

			customFilterPanel = panel.GetOrNull<Widget>("CUSTOM_FILTER_PANEL");
			labelContainer = panel.GetOrNull<Widget>("LABEL_CONTAINER");
			buttonsPanel = panel.GetOrNull<Widget>("BUTTONS");
			timeLabelWidget = panel.GetOrNull<LabelWidget>("TIME_LABEL");
			titleLabelWidget = panel.GetOrNull<LabelWidget>("TITLE_LABEL");
			shuffleWidget = panel.GetOrNull<CheckboxWidget>("SHUFFLE");
			repeatWidget = panel.GetOrNull<CheckboxWidget>("REPEAT");
			volumeLabelWidget = panel.GetOrNull<LabelWidget>("VOLUME_LABEL");
			musicSliderWidget = panel.GetOrNull<SliderWidget>("MUSIC_SLIDER");
			noMusicContainer = panel.GetOrNull<Widget>("NO_MUSIC_LABEL");
			muteLabelWidget = panel.GetOrNull<LabelWidget>("MUTE_LABEL");
			modePanel = panel.GetOrNull<Widget>("MODE_PANEL");
			backButtonWidget = panel.GetOrNull<ButtonWidget>("BACK_BUTTON");
			noSongsLabelWidget = panel.GetOrNull<LabelWidget>("NO_MATCHING_SONGS");

			basePanelHeight = panel.Bounds.Height;
			panelCenterY = panel.Bounds.Y + basePanelHeight / 2;
			allowPanelResize = panel.Id == "MUSIC_PANEL";
			customLayoutOffset = (customFilterPanel?.Bounds.Height ?? 0) + CustomDownOffset + CustomLowerOffset;
			expandedPanelHeight = allowPanelResize ? basePanelHeight + customLayoutOffset : basePanelHeight;
			collapsedPanelHeight = basePanelHeight;

			if (customFilterPanel != null)
				customFilterPanel.IsVisible = () => allowPanelResize && showCustomFilters;

			RegisterCustomDownTarget(labelContainer);
			RegisterCustomDownTarget(musicList);
			RegisterCustomDownTarget(noSongsLabelWidget);
			RegisterCustomDownTarget(titleLabelWidget);

			RegisterCustomLowerTarget(buttonsPanel);
			RegisterCustomLowerTarget(timeLabelWidget);
			RegisterCustomLowerTarget(shuffleWidget);
			RegisterCustomLowerTarget(repeatWidget);
			RegisterCustomLowerTarget(volumeLabelWidget);
			RegisterCustomLowerTarget(musicSliderWidget);
			RegisterCustomLowerTarget(noMusicContainer);
			RegisterCustomLowerTarget(muteLabelWidget);
			RegisterCustomLowerTarget(backButtonWidget);

			RegisterCustomRaiseTarget(modePanel);

			ConfigureModeButton(panel, "MUSIC_MODE_MIX", MusicPlaybackMode.MixAll);
			ConfigureModeButton(panel, "MUSIC_MODE_OLD", MusicPlaybackMode.OnlyOldschool);
			ConfigureModeButton(panel, "MUSIC_MODE_FACTION", MusicPlaybackMode.FactionSpecific);

			BuildMusicTable();

			bool NoMusic() => !musicPlaylist.IsMusicAvailable || musicPlaylist.CurrentSongIsBackground || currentSong == null;
			panel.Get("NO_MUSIC_LABEL").IsVisible = () => !musicPlaylist.IsMusicAvailable;

			if (musicPlaylist.IsMusicAvailable)
			{
				panel.Get<LabelWidget>("MUTE_LABEL").GetText = () =>
				{
					if (Game.Settings.Sound.Mute)
						return FluentProvider.GetMessage(SoundMuted);

					return "";
				};
			}

			var playButton = panel.Get<ButtonWidget>("BUTTON_PLAY");
			playButton.OnClick = Play;
			playButton.IsDisabled = NoMusic;
			playButton.IsVisible = () => !Game.Sound.MusicPlaying;

			var pauseButton = panel.Get<ButtonWidget>("BUTTON_PAUSE");
			pauseButton.OnClick = Game.Sound.PauseMusic;
			pauseButton.IsDisabled = NoMusic;
			pauseButton.IsVisible = () => Game.Sound.MusicPlaying;

			var stopButton = panel.Get<ButtonWidget>("BUTTON_STOP");
			stopButton.OnClick = musicPlaylist.Stop;
			stopButton.IsDisabled = NoMusic;

			var nextButton = panel.Get<ButtonWidget>("BUTTON_NEXT");
			nextButton.OnClick = () => { currentSong = musicPlaylist.GetNextSong(); Play(); };
			nextButton.IsDisabled = NoMusic;

			var prevButton = panel.Get<ButtonWidget>("BUTTON_PREV");
			prevButton.OnClick = () => { currentSong = musicPlaylist.GetPrevSong(); Play(); };
			prevButton.IsDisabled = NoMusic;

			if (shuffleWidget != null)
			{
				shuffleWidget.IsChecked = () => Game.Settings.Sound.Shuffle;
				shuffleWidget.OnClick = () => Game.Settings.Sound.Shuffle ^= true;
				shuffleWidget.IsDisabled = () => musicPlaylist.CurrentSongIsBackground;
			}

			if (repeatWidget != null)
			{
				repeatWidget.IsChecked = () => Game.Settings.Sound.Repeat;
				repeatWidget.OnClick = () => Game.Sound.SetMusicLooped(!Game.Settings.Sound.Repeat);
				repeatWidget.IsDisabled = () => musicPlaylist.CurrentSongIsBackground;
			}

			if (timeLabelWidget != null)
				timeLabelWidget.GetText = () =>
			{
				if (currentSong == null || musicPlaylist.CurrentSongIsBackground)
					return "";

				var seek = Game.Sound.MusicSeekPosition;
				var minutes = (int)seek / 60;
				var seconds = (int)seek % 60;
				var totalMinutes = currentSong.Length / 60;
				var totalSeconds = currentSong.Length % 60;

				return $"{minutes:D2}:{seconds:D2} / {totalMinutes:D2}:{totalSeconds:D2}";
			};

			var noSongPlaying = FluentProvider.GetMessage(NoSongPlaying);
			if (titleLabelWidget != null)
				titleLabelWidget.GetText = () => currentSong != null ? currentSong.Title : noSongPlaying;

			if (musicSliderWidget != null)
			{
				musicSliderWidget.OnChange += x => Game.Sound.MusicVolume = x;
				musicSliderWidget.Value = Game.Sound.MusicVolume;
			}

			var songWatcher = panel.GetOrNull<LogicTickerWidget>("SONG_WATCHER");
			if (songWatcher != null)
			{
				songWatcher.OnTick = () =>
				{
					if (musicPlaylist.CurrentSongIsBackground && currentSong != null)
						currentSong = null;

					if (Game.Sound.CurrentMusic == null || currentSong == Game.Sound.CurrentMusic || musicPlaylist.CurrentSongIsBackground)
						return;

					currentSong = Game.Sound.CurrentMusic;
				};
			}

			if (backButtonWidget != null)
				backButtonWidget.OnClick = () => { Game.Settings.Save(); Ui.CloseWindow(); onExit(); };

			UpdateCustomLayout(forceRefresh: true);
		}

		public void BuildMusicTable()
		{
			if (!musicPlaylist.IsMusicAvailable)
				return;

			var music = musicPlaylist.AvailablePlaylist();
			noSongsForMode = musicPlaylist.IsMusicAvailable && music.Length == 0;
			currentSong = musicPlaylist.CurrentSong();

			musicList.RemoveChildren();
			foreach (var song in music)
			{
				var item = ScrollItemWidget.Setup(song.Filename, itemTemplate, () => currentSong == song, () => { currentSong = song; Play(); }, () => { });
				var label = item.Get<LabelWithTooltipWidget>("TITLE");
				WidgetUtils.TruncateLabelToTooltip(label, song.Title);

				item.Get<LabelWidget>("LENGTH").GetText = () => SongLengthLabel(song);
				musicList.AddChild(item);
			}

			if (currentSong != null && !musicPlaylist.CurrentSongIsBackground)
				musicList.ScrollToItem(currentSong.Filename);
		}

		void ConfigureModeButton(Widget panel, string id, MusicPlaybackMode mode)
		{
			var button = panel.GetOrNull<ButtonWidget>(id);
			if (button == null)
				return;

			button.IsHighlighted = () => Game.Settings.Sound.MusicMode == mode;
			button.OnClick = () =>
			{
				if (Game.Settings.Sound.MusicMode == mode)
					return;

				var resume = Game.Sound.MusicPlaying && !musicPlaylist.CurrentSongIsBackground;
				Game.Settings.Sound.MusicMode = mode;
				musicPlaylist.RefreshForPlaybackModeChange(resume);
				BuildMusicTable();
				if (resume)
					Play();
			};
		}

		void Play()
		{
			if (currentSong == null)
				return;

			musicList.ScrollToItem(currentSong.Filename);
			musicPlaylist.Play(currentSong);
		}

		static string SongLengthLabel(MusicInfo song)
		{
			return $"{song.Length / 60:D1}:{song.Length % 60:D2}";
		}
	}
}
