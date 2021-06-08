using BeatSaberMarkupLanguage.Components;
using BeatsaverBeatmapAvailability.Configuration;
using BeatsaverBeatmapAvailability.Managers;
using HMUI;
using ModestTree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
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
        private readonly BeatmapAvailabilityChecker _beatmapAvailabilityChecker;
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
        private IPreviewBeatmapLevel _lastSelectedBeatmap;
        private bool _lastIsWIPLevel = false;
        private ButtonState _currentState = ButtonState.Unknown;
        private string _statusText = "blep";
        private CurvedTextMeshPro _curvedTextMeshPro;

        private float _lastClickTime = 0f;
        private float _doubleClickThreshold = .25f;
        private CancellationTokenSource _singleClickCancellationTokenSource;


        public event Action<IPreviewBeatmapLevel, bool> OnButtonPressed; // IPreviewBeatmapLevel, isWIP
        public event Action OnOpenSettingsButtonPressed; // IPreviewBeatmapLevel, isWIP

        public enum ButtonState
        {
            Online,
            Loading,
            Offline,
            DoubleClick,
            Unknown
        }

        internal ButtonManager(PluginConfig pluginConfig, BeatmapAvailabilityCacheManager beatmapAvailabilityCacheManager, BeatmapAvailabilityChecker beatmapAvailabilityChecker, BeatSaverAPIManager beatSaverAPIManager, DiContainer container, LevelSelectionNavigationController levelSelectionNavigationController, StandardLevelDetailViewController standardLevelDetailViewController)
        {
            _pluginConfig = pluginConfig;
            _beatmapAvailabilityCacheManager = beatmapAvailabilityCacheManager;
            _beatmapAvailabilityChecker = beatmapAvailabilityChecker;
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
            _pluginConfig.onConfigChanged += _pluginConfig_onConfigChanged;
        }

        public async Task InitializeAsync()
        {
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

            _imageButton = CreateImage();
            _curvedTextMeshPro = CreateTextForImage(_imageButton);

            _imageButton.OnClickEvent += Clicked;
            UpdateSprite();
            UpdateStatusText("Unknown");
        }

        public void Clicked(PointerEventData __)
        {
            if (_currentState == ButtonState.Loading) return;
            _currentState = ButtonState.DoubleClick;
            

            if (_lastClickTime + _doubleClickThreshold > Time.realtimeSinceStartup)
            {
                //Double Click
                _singleClickCancellationTokenSource.Cancel();
                _currentState = ButtonState.Unknown;
                _lastClickTime = 0;
                OnOpenSettingsButtonPressed?.Invoke();
            }

            UpdateSprite();

            if (_singleClickCancellationTokenSource == null)
                _singleClickCancellationTokenSource = new CancellationTokenSource();
            CancellationToken ct = _singleClickCancellationTokenSource.Token;

            _ = Task.Run(async () => {
                try
                {
                    await Task.Run(async () => {
                        await Task.Delay((int) (1000f * _doubleClickThreshold));

                        if (ct.IsCancellationRequested)
                        {
                            ct.ThrowIfCancellationRequested();
                        }

                        // Single Press

                        CheckOnlineBeatmapAvailability();

                        OnButtonPressed?.Invoke(_lastSelectedBeatmap, _lastIsWIPLevel);

                    }, _singleClickCancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    
                }
                finally
                {
                    _singleClickCancellationTokenSource.Dispose();
                    _singleClickCancellationTokenSource = null;
                }
            });

            _lastClickTime = Time.realtimeSinceStartup;
        }

        private void _pluginConfig_onConfigChanged()
        {
            UpdateImageTransform(_imageButton, _pluginConfig.ButtonSettings);
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
                case ButtonState.DoubleClick:
                case ButtonState.Loading:
                    _imageButton.sprite = _spriteLoading;
                    break;
                case ButtonState.Unknown:
                    _imageButton.sprite = _spriteUnknown;
                    break;
            }
        }

        private void UpdateStatusText(string text)
        {
            _curvedTextMeshPro.text = text;
        }

        public async Task<Sprite> LoadSprite(string resource)
        {
            using Stream mrs = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource);
            using MemoryStream ms = new MemoryStream();
            await mrs.CopyToAsync(ms);
            return BeatSaberMarkupLanguage.Utilities.LoadSpriteRaw(ms.ToArray());
        }

        public async void CheckOnlineBeatmapAvailability()
        {
            if (_currentState == ButtonState.Loading) return;
            _currentState = ButtonState.Loading;
            UpdateSprite();
            var availabilityData = await _beatmapAvailabilityChecker.CheckIfBeatmapIsAvailable(_lastSelectedBeatmap);
            switch (availabilityData.Availability)
            {
                case Models.Availability.Online:
                    _currentState = ButtonState.Online;
                    break;
                case Models.Availability.Offline:
                    _currentState = ButtonState.Offline;
                    break;
            }
            _statusText = availabilityData.KeyText;
            UpdateSprite();
        }

        private void _standardLevelDetailViewController_didActivateEvent(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            if(_reactivateOnActivate)
            {
                ShowButton(_lastSelectedBeatmap);
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

                    ShowButton(beatmap);
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

        internal void ShowButton(IPreviewBeatmapLevel previewBeatmapLevel)
        {
            if (previewBeatmapLevel == null) return;
            _lastSelectedBeatmap = previewBeatmapLevel;

            _imageButton.gameObject.SetActive(true);

            string levelHash = SongCore.Collections.hashForLevelID(previewBeatmapLevel.levelID);

            if(SongCore.Loader.CustomWIPLevels.Any(x => x.Value.levelID.Equals(previewBeatmapLevel.levelID)))
            {
                _currentState = ButtonState.Offline;
                _statusText = "WIP Level, no code.";
                UpdateSprite();
                return;
            }

            if(_pluginConfig.AutoCheck)
            {
                CheckOnlineBeatmapAvailability();
            }
            else
            {
                // Offline cache check only.
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

        private void UpdateImageTransform(ClickableImage clickableImage, PluginConfig.ButtonData buttonData)
        {
            clickableImage.rectTransform.localPosition = buttonData.Position.ToVector3();//new Vector3(40f, -40f, 0f);
            clickableImage.rectTransform.localScale = new Vector3(.3f, .3f, .3f) * buttonData.Scale; //new Vector3(.3f, .3f, .3f);
            clickableImage.rectTransform.sizeDelta = new Vector2(20f, 20f) * buttonData.Scale;
        }

        private void UpdateStatusTextTransform(CurvedTextMeshPro curvedTextMeshPro, PluginConfig.ButtonData buttonData)
        {
            curvedTextMeshPro.rectTransform.localPosition = buttonData.Position.ToVector3() + new Vector3(1.5f * buttonData.Scale, 0,0);//new Vector3(40f, -40f, 0f);
            curvedTextMeshPro.rectTransform.localScale = new Vector3(.1f, .1f, .1f) * buttonData.Scale; //new Vector3(.3f, .3f, .3f);
            curvedTextMeshPro.rectTransform.sizeDelta = new Vector2(200f, 20f) * buttonData.Scale;
        }


        private ClickableImage CreateImage()
        {
            GameObject gameObject = new GameObject("CoolBeatmapIsOnlineButton");
            ClickableImage image = gameObject.AddComponent<ClickableImage>();
            image.material = BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat;

            image.rectTransform.SetParent(_levelSelectionNavigationController.transform);

            UpdateImageTransform(image, _pluginConfig.ButtonSettings);
            gameObject.AddComponent<LayoutElement>();

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.Tangent;
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.Normal;
            _container.InstantiateComponent<VRGraphicRaycaster>(gameObject);

            return image;
        }

        private CurvedTextMeshPro CreateTextForImage(ClickableImage image)
        {
            CurvedTextMeshPro curvedTextMeshPro = null; ;

            CurvedCanvasSettingsHelper ccsh = new CurvedCanvasSettingsHelper();

            try
            {
                GameObject gameObject = new GameObject("StatusText");
                //gameObject.transform.SetParent(image.gameObject.transform);


                curvedTextMeshPro = gameObject.AddComponent<CurvedTextMeshPro>();

                curvedTextMeshPro.rectTransform.SetParent(_levelSelectionNavigationController.transform);

                gameObject.SetActive(false);
            }
            catch(Exception ex)
            {
                Logger.log.Error(ex.Message);
                Logger.log.Error(ex.StackTrace);
            }

            if(curvedTextMeshPro == null)
            {
                var go = new GameObject("StatusText");
                curvedTextMeshPro = go.AddComponent<CurvedTextMeshPro>();
                go.transform.SetParent(image.gameObject.transform);
                curvedTextMeshPro.text = "Itbroke ree";

                go.AddComponent<LayoutElement>();
                go.AddComponent<Canvas>();
            }

            UpdateStatusTextTransform(curvedTextMeshPro, _pluginConfig.ButtonSettings);

            return curvedTextMeshPro;
        }

    }
}
