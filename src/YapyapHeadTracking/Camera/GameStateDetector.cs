using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YapyapHeadTracking.Camera
{
    /// <summary>
    /// Detects gameplay vs menus/UI overlays.
    ///
    /// YAPYAP is a co-op multiplayer game: there is no Time.timeScale pause. The
    /// authoritative gameplay signal is YAPYAP.InputManager.HasFocus (false whenever
    /// any UI overlay - settings, chat, spell wheels, popups - has input focus, or
    /// the app loses focus) combined with YAPYAP.Pawn.LocalInstance (null until the
    /// local player has spawned, so the main menu and loading are excluded).
    ///
    /// If those members can't be resolved (game update changed them), falls back to
    /// Cursor.lockState - the game locks the cursor exactly while gameplay has focus.
    /// </summary>
    internal class GameStateDetector
    {
        private const float CheckIntervalSeconds = 0.1f;

        private GameState _currentState = GameState.Unknown;
        private float _lastCheckTime;

        public event Action<GameState> StateChanged;

        public bool IsGameplayActive => _currentState == GameState.Gameplay;

        public void Initialize()
        {
            GameTypes.Resolve();
            SceneManager.sceneLoaded += OnSceneLoaded;
            UpdateState();
        }

        public void Shutdown()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        public void Update()
        {
            if (Time.time - _lastCheckTime < CheckIntervalSeconds)
                return;

            _lastCheckTime = Time.time;
            UpdateState();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _lastCheckTime = 0f;
            UpdateState();
        }

        private void UpdateState()
        {
            var newState = DetectState();
            if (newState != _currentState)
            {
                _currentState = newState;
                StateChanged?.Invoke(newState);
            }
        }

        private static GameState DetectState()
        {
            bool? focused = GameTypes.IsLocalPlayerFocused();
            if (focused.HasValue)
                return focused.Value ? GameState.Gameplay : GameState.Paused;

            if (Cursor.lockState == CursorLockMode.Locked)
                return GameState.Gameplay;

            return GameState.Paused;
        }
    }
}
