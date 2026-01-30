using Core.Interfaces;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Managers
{
    public class GameInputManager : MonoBehaviour, IService
    {
        [SerializeField]
        private int maxPlayers = 2;

        [SerializeField]
        private bool allowKeyboardForPlayer0 = true;

        private InputSystem_Actions[] playerActions;

        public void InitializeService()
        {
            SetupPlayers();
        }

        public void StartService()
        {
            InputSystem.onDeviceChange += HandleDeviceChange;
            ApplyDevices();
        }

        public void CleanupService()
        {
            InputSystem.onDeviceChange -= HandleDeviceChange;
            DisablePlayers();
            DisposePlayers();
        }

        public Vector2 GetMovement(int player)
        {
            if (!TryGetPlayerActions(player, out var actions))
                return Vector2.zero;

            return actions.Player.Move.ReadValue<Vector2>();
        }

        public bool GetDashing(int player)
        {
            if (!TryGetPlayerActions(player, out var actions))
                return false;

            return actions.Player.Sprint.WasPressedThisFrame();
        }

        private void SetupPlayers()
        {
            var playerCount = Mathf.Max(1, maxPlayers);
            playerActions = new InputSystem_Actions[playerCount];
            for (int i = 0; i < playerActions.Length; i++)
                playerActions[i] = new InputSystem_Actions();

            ApplyDevices();
        }

        private void DisablePlayers()
        {
            if (playerActions == null)
                return;

            for (int i = 0; i < playerActions.Length; i++)
                playerActions[i].Player.Disable();
        }

        private void DisposePlayers()
        {
            if (playerActions == null)
                return;

            for (int i = 0; i < playerActions.Length; i++)
                playerActions[i]?.Dispose();
            playerActions = null;
        }

        private void ApplyDevices()
        {
            if (playerActions == null)
                return;

            for (int i = 0; i < playerActions.Length; i++)
            {
                var devices = new List<InputDevice>(2);
                var gamepad = i < Gamepad.all.Count ? Gamepad.all[i] : null;
                if (gamepad != null)
                    devices.Add(gamepad);

                if (i == 0 && allowKeyboardForPlayer0)
                {
                    if (Keyboard.current != null)
                        devices.Add(Keyboard.current);
                    if (Mouse.current != null)
                        devices.Add(Mouse.current);
                }

                if (devices.Count > 0)
                {
                    playerActions[i].devices = devices.ToArray();
                    playerActions[i].Player.Enable();
                }
                else
                {
                    playerActions[i].devices = null;
                    playerActions[i].Player.Disable();
                }
            }
        }

        private bool TryGetPlayerActions(int player, out InputSystem_Actions actions)
        {
            actions = null;
            if (playerActions == null)
                return false;
            if (player < 0 || player >= playerActions.Length)
                return false;

            actions = playerActions[player];
            return actions != null;
        }

        private void HandleDeviceChange(InputDevice device, InputDeviceChange change)
        {
            switch (change)
            {
                case InputDeviceChange.Added:
                case InputDeviceChange.Removed:
                case InputDeviceChange.Disconnected:
                case InputDeviceChange.Reconnected:
                    ApplyDevices();
                    break;
            }
        }
    }
}
