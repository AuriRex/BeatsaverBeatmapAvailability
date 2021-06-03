using BeatSaberMarkupLanguage.Components;
using BeatsaverBeatmapAvailability.Configuration;
using BeatsaverBeatmapAvailability.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VRUIControls;
using Zenject;

namespace BeatsaverBeatmapAvailability.UI
{
    public class ButtonManager : IInitializable, IDisposable
    {
        private readonly PluginConfig _pluginConfig;
        private readonly BeatmapAvailabilityCacheManager _beatmapAvailabilityCacheManager;
        private readonly BeatSaverAPIManager _beatSaverAPIManager;
        private readonly DiContainer _container;
        private readonly LevelSelectionNavigationController _levelSelectionNavigationController;
        private readonly StandardLevelDetailViewController _standardLevelDetailViewController;

#nullable enable
        private ClickableImage? _imageButton;
#nullable restore
        private Sprite _spriteOnline;
        private Sprite _spriteOffline;
        private Sprite _spriteLoading;
        private Sprite _spriteUnknown;
        private bool _reactivateOnActivate = false;
        private string _lastHash;
        private bool _lastIsWIPLevel = false;
        private ButtonState _currentState = ButtonState.Unknown;
        private string _statusText = "blep";
        private string _lastSongName;
        

        public event Action<string, string, bool> OnButtonPressed; // hash, songName, isWIP

        public enum ButtonState
        {
            Online,
            Loading,
            Offline,
            Unknown
        }

        internal ButtonManager(PluginConfig pluginConfig, BeatmapAvailabilityCacheManager beatmapAvailabilityCacheManager, BeatSaverAPIManager beatSaverAPIManager, DiContainer container, LevelSelectionNavigationController levelSelectionNavigationController, StandardLevelDetailViewController standardLevelDetailViewController)
        {
            _pluginConfig = pluginConfig;
            _beatmapAvailabilityCacheManager = beatmapAvailabilityCacheManager;
            _beatSaverAPIManager = beatSaverAPIManager;
            _container = container;
            _levelSelectionNavigationController = levelSelectionNavigationController;
            _standardLevelDetailViewController = standardLevelDetailViewController;
        }
        
        public void Initialize()
        {
            _ = InitializeAsync();
            _standardLevelDetailViewController.didChangeContentEvent += _standardLevelDetailViewController_didChangeContentEvent;
            _standardLevelDetailViewController.didActivateEvent += _standardLevelDetailViewController_didActivateEvent;
        }


        public async Task InitializeAsync()
        {
            _imageButton = CreateImage();

            var taskOnline = LoadSprite("BeatsaverBeatmapAvailability.Resources.Icons.online.png");
            var taskOffline = LoadSprite("BeatsaverBeatmapAvailability.Resources.Icons.offline.png");
            var taskLoading = LoadSprite("BeatsaverBeatmapAvailability.Resources.Icons.loading.png");
            var taskUnknown = LoadSprite("BeatsaverBeatmapAvailability.Resources.Icons.unknown.png");

            Task.WaitAll(taskOnline, taskOffline, taskLoading, taskUnknown);

            _spriteOnline = await taskOnline;
            _spriteOffline = await taskOffline;
            _spriteLoading = await taskLoading;
            _spriteUnknown = await taskUnknown;

            _spriteOnline.texture.wrapMode = TextureWrapMode.Clamp;
            _spriteOffline.texture.wrapMode = TextureWrapMode.Clamp;
            _spriteLoading.texture.wrapMode = TextureWrapMode.Clamp;
            _spriteUnknown.texture.wrapMode = TextureWrapMode.Clamp;

            _imageButton.OnClickEvent += Clicked;
            UpdateSprite();
        }

        private void UpdateSprite()
        {
            switch (_currentState)
            {
                case ButtonState.Online:
                    _imageButton.sprite = _spriteOnline;
                    break;
                case ButtonState.Offline:
                    _imageButton.sprite = _spriteOffline;
                    break;
                case ButtonState.Loading:
                    _imageButton.sprite = _spriteLoading;
                    break;
                case ButtonState.Unknown:
                    _imageButton.sprite = _spriteUnknown;
                    break;
            }
        }

        public async Task<Sprite> LoadSprite(string resource)
        {
            using Stream mrs = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource);
            using MemoryStream ms = new MemoryStream();
            await mrs.CopyToAsync(ms);
            return BeatSaberMarkupLanguage.Utilities.LoadSpriteRaw(ms.ToArray());
        }

        public void Clicked(PointerEventData _)
        {
            SetButtonState();

            OnButtonPressed?.Invoke(_lastHash, _lastSongName, _lastIsWIPLevel);
        }

        public async void SetButtonState()
        {
            _currentState = ButtonState.Loading;
            UpdateSprite();
            if (_beatmapAvailabilityCacheManager.HashIsCached(_lastHash))
            {
                _statusText = $"BeatSaverKey: {_beatmapAvailabilityCacheManager.GetOldBeatSaverKeyIfAvailable(_lastHash)}";
                if (_beatmapAvailabilityCacheManager.IsProbablyOnline(_lastHash))
                    _currentState = ButtonState.Online;
                else if (_beatmapAvailabilityCacheManager.IsOffline(_lastHash))
                    _currentState = ButtonState.Offline;
            }
            else
            {
                // Beatmap not cached, contact BeatSaver and cache it.
                var beatmap = await _beatSaverAPIManager.GetMapByHash(_lastHash);
                if (BeatSaverAPIManager.MapIsOffline(beatmap))
                {
                    _beatmapAvailabilityCacheManager.AddOfflineBeatmap(_lastHash, _lastSongName);
                    _statusText = $"BeatSaverKey: {_beatmapAvailabilityCacheManager.GetOldBeatSaverKeyIfAvailable(_lastHash)}";
                    _currentState = ButtonState.Offline;
                }
                else
                {
                    _beatmapAvailabilityCacheManager.AddOnlineBeatmap(_lastHash, beatmap);
                    _statusText = $"BeatSaverKey: {beatmap.Key}";
                    _currentState = ButtonState.Online;
                }
            }
            UpdateSprite();
        }

        private void _standardLevelDetailViewController_didActivateEvent(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            if(_reactivateOnActivate)
            {
                ShowButton(_lastHash, _lastSongName);
                _reactivateOnActivate = false;
            }
        }

        private void _standardLevelDetailViewController_didChangeContentEvent(StandardLevelDetailViewController levelDetailViewController, StandardLevelDetailViewController.ContentType contentType)
        {
            switch(contentType)
            {
                case StandardLevelDetailViewController.ContentType.OwnedAndReady:
                    IPreviewBeatmapLevel beatmap = levelDetailViewController.beatmapLevel;

                    string hash = SongCore.Collections.hashForLevelID(beatmap.levelID);
                    Logger.log.Debug($"Selected level with hash: {hash}, {beatmap.songName}");

                    bool isWIP = SongCore.Loader.CustomWIPLevels.Any(x => x.Value.levelID.Equals(beatmap.levelID));

                    ShowButton(hash, beatmap.songName, isWIP);
                    break;
                case StandardLevelDetailViewController.ContentType.Inactive:
                    HideButton(true);
                    break;
                default:
                    HideButton();
                    break;
            }
        }

        internal void HideButton(bool reactivate = false)
        {
            _reactivateOnActivate = reactivate;
            _imageButton.gameObject.SetActive(false);
        }

        internal void ShowButton(string levelHash, string songName, bool isWIPLevel = false)
        {
            if (string.IsNullOrEmpty(levelHash) || string.IsNullOrEmpty(songName)) return;
            _lastHash = levelHash;
            _lastSongName = songName;
            _lastIsWIPLevel = isWIPLevel;

            _imageButton.gameObject.SetActive(true);

            if(isWIPLevel)
            {
                _currentState = ButtonState.Offline;
                _statusText = "WIP Level, no code.";
            }

            if(_pluginConfig.AutoCheck)
            {
                SetButtonState();
            }
            else
            {
                _currentState = ButtonState.Unknown;
                if (_beatmapAvailabilityCacheManager.HashIsCached(levelHash))
                {
                    _statusText = $"BeatSaverKey: {_beatmapAvailabilityCacheManager.GetOldBeatSaverKeyIfAvailable(levelHash)}";
                    if (_beatmapAvailabilityCacheManager.IsProbablyOnline(levelHash))
                        _currentState = ButtonState.Online;
                    else if (_beatmapAvailabilityCacheManager.IsOffline(levelHash))
                        _currentState = ButtonState.Offline;
                }
                UpdateSprite();
            }
        }

        public void Dispose()
        {
            if(_standardLevelDetailViewController != null)
            {
                _standardLevelDetailViewController.didChangeContentEvent -= _standardLevelDetailViewController_didChangeContentEvent;
                _standardLevelDetailViewController.didActivateEvent -= _standardLevelDetailViewController_didActivateEvent;
            }
        }

        private ClickableImage CreateImage()
        {
            GameObject gameObject = new GameObject("CoolBeatmapIsOnlineButton");
            ClickableImage image = gameObject.AddComponent<ClickableImage>();
            image.material = BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat;

            image.rectTransform.SetParent(_levelSelectionNavigationController.transform);
            image.rectTransform.localPosition = new Vector3(38f, -40f, 0f);
            image.rectTransform.localScale = new Vector3(.3f, .3f, .3f);
            image.rectTransform.sizeDelta = new Vector2(20f, 20f);
            gameObject.AddComponent<LayoutElement>();

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.Tangent;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.Normal;
            _container.InstantiateComponent<VRGraphicRaycaster>(gameObject);

            return image;
        }
    }
}
