using System;
using System.Collections.Generic;
using Windows.Gaming.Input;

namespace robot.sl.Devices
{
    public struct GamepadButtonDownResult
    {
        public bool ButtonDown;
        public bool ButtonClicked;
    }

    public class GamepadButtonDown
    {
        private TimeSpan _buttonDownTime;
        private DateTime? _buttonDownCalledTime;
        private bool _buttonDownCalled;
        private GamepadButtons _gamepadButtonOne;
        private GamepadButtons? _gamepadButtonTwo;

        public GamepadButtonDown(TimeSpan buttonDownTime,
                                 GamepadButtons gamepadButtonOne)
        {
            _buttonDownTime = buttonDownTime;
            _gamepadButtonOne = gamepadButtonOne;
        }

        public GamepadButtonDown(TimeSpan buttonDownTime,
                                 GamepadButtons gamepadButtonOne,
                                 GamepadButtons gamepadButtonTwo)
        {
            _buttonDownTime = buttonDownTime;
            _gamepadButtonOne = gamepadButtonOne;
            _gamepadButtonTwo = gamepadButtonTwo;
        }

        public GamepadButtonDownResult UpdateGamepadButtonState(GamepadReading gamepadReading)
        {
            var gamepadButtonDownResult = UpdateGamepadButtonState(gamepadReading, null);
            return gamepadButtonDownResult;
        }

        public GamepadButtonDownResult UpdateGamepadButtonState(GamepadReading gamepadReading, List<GamepadButtons> gamepadButtonsPreventClickable)
        {
            var clickable = true;
            if(gamepadButtonsPreventClickable != null)
            {
                foreach (var button in gamepadButtonsPreventClickable)
                {
                    clickable = (gamepadReading.Buttons & _gamepadButtonOne) == _gamepadButtonOne;

                    if(!clickable)
                    {
                        break;
                    }
                }
            }

            var gamepadButtonDownResult = new GamepadButtonDownResult();

            var buttonDown = false;
            if (!_gamepadButtonTwo.HasValue)
            {
                buttonDown = (gamepadReading.Buttons & _gamepadButtonOne) == _gamepadButtonOne;
            }
            else
            {
                buttonDown = (gamepadReading.Buttons & (_gamepadButtonOne | _gamepadButtonTwo.Value)) == (_gamepadButtonOne | _gamepadButtonTwo.Value);
            }

            gamepadButtonDownResult.ButtonDown = buttonDown;

            if (!buttonDown && _buttonDownCalled)
            {
                _buttonDownCalled = false;
            }

            if (buttonDown
                && !_buttonDownCalled
                && !_buttonDownCalledTime.HasValue
                && clickable)
            {
                _buttonDownCalledTime = DateTime.Now;
            }
            else if (_buttonDownCalledTime.HasValue
                     && DateTime.Now >= _buttonDownCalledTime.Value.Add(_buttonDownTime)
                     && !_buttonDownCalled)
            {
                gamepadButtonDownResult.ButtonClicked = true;

                _buttonDownCalled = true;
                _buttonDownCalledTime = null;
            }

            return gamepadButtonDownResult;
        }
    }
}
