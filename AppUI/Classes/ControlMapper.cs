using AppCore;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using static AppUI.Classes.KeyboardInputSender;

namespace AppUI.Classes
{
    /// <summary>
    /// enum of all available controls in game that can be binded to
    /// </summary>
    public enum GameControl
    {
        OK, // "Select"
        Exit,
        Misc,
        Menu,
        Toggle,
        Trigger,
        RotLt,
        RotRt,
        Start,
        Select,
        Up,
        Down,
        Left,
        Right
    }

    public class ControlMapper
    {
        private enum ControlType
        {
            Keyboard,
            Joystick
        }

        /// <summary>
        /// Defines all possible inputs that can be used for binding to game controls
        /// </summary>
        public static List<ControlInputSetting> ControlInputs = new List<ControlInputSetting>()
        {
            new ControlInputSetting("ESC", 1, Key.Escape, StringKey.Esc) { KeyScanCode = ScanCodeShort.ESCAPE },
            new ControlInputSetting("1", 2, Key.D1, StringKey.D1) { KeyScanCode = ScanCodeShort.KEY_1 },
            new ControlInputSetting("2", 3, Key.D2, StringKey.D2) { KeyScanCode = ScanCodeShort.KEY_2 },
            new ControlInputSetting("3", 4, Key.D3, StringKey.D3) { KeyScanCode = ScanCodeShort.KEY_3 },
            new ControlInputSetting("4", 5, Key.D4, StringKey.D4) { KeyScanCode = ScanCodeShort.KEY_4 },
            new ControlInputSetting("5", 6, Key.D5, StringKey.D5) { KeyScanCode = ScanCodeShort.KEY_5 },
            new ControlInputSetting("6", 7, Key.D6, StringKey.D6) { KeyScanCode = ScanCodeShort.KEY_6 },
            new ControlInputSetting("7", 8, Key.D7, StringKey.D7) { KeyScanCode = ScanCodeShort.KEY_7 },
            new ControlInputSetting("8", 9, Key.D8, StringKey.D8) { KeyScanCode = ScanCodeShort.KEY_8 },
            new ControlInputSetting("9", 10, Key.D9, StringKey.D9) { KeyScanCode = ScanCodeShort.KEY_9 },
            new ControlInputSetting("0", 11, Key.D0, StringKey.D0) { KeyScanCode = ScanCodeShort.KEY_0 },
            new ControlInputSetting("MINUS", 12, Key.OemMinus, StringKey.Minus) { KeyScanCode = ScanCodeShort.OEM_MINUS },
            new ControlInputSetting("EQUALS", 13, Key.OemPlus, StringKey.Equal) { KeyScanCode = ScanCodeShort.OEM_PLUS },
            new ControlInputSetting("BACKSPACE", 14, Key.Back, StringKey.Backspace) { KeyScanCode = ScanCodeShort.BACK },
            new ControlInputSetting("TAB", 15, Key.Tab, StringKey.Tab) { KeyScanCode = ScanCodeShort.TAB },
            new ControlInputSetting("Q", 16, Key.Q) { KeyScanCode = ScanCodeShort.KEY_Q },
            new ControlInputSetting("W", 17, Key.W) { KeyScanCode = ScanCodeShort.KEY_W },
            new ControlInputSetting("E", 18, Key.E) { KeyScanCode = ScanCodeShort.KEY_E },
            new ControlInputSetting("R", 19, Key.R) { KeyScanCode = ScanCodeShort.KEY_R },
            new ControlInputSetting("T", 20, Key.T) { KeyScanCode = ScanCodeShort.KEY_T },
            new ControlInputSetting("Y", 21, Key.Y) { KeyScanCode = ScanCodeShort.KEY_Y },
            new ControlInputSetting("U", 22, Key.U) { KeyScanCode = ScanCodeShort.KEY_U },
            new ControlInputSetting("I", 23, Key.I) { KeyScanCode = ScanCodeShort.KEY_I },
            new ControlInputSetting("O", 24, Key.O) { KeyScanCode = ScanCodeShort.KEY_O },
            new ControlInputSetting("P", 25, Key.P) { KeyScanCode = ScanCodeShort.KEY_P },
            new ControlInputSetting("LEFTBRACKET", 26, Key.OemOpenBrackets, StringKey.LeftBracket) { KeyScanCode = ScanCodeShort.OEM_4 }, // assumes standard us keyboard layout
            new ControlInputSetting("RIGHTBRACKET", 27, Key.OemCloseBrackets, StringKey.RightBracket) { KeyScanCode = ScanCodeShort.OEM_6 },
            new ControlInputSetting("RETURN", 28, Key.Return, StringKey.Return) { KeyScanCode = ScanCodeShort.RETURN },
            new ControlInputSetting("LEFTCTRL", 29, Key.LeftCtrl, StringKey.LeftCtrl) { KeyScanCode = ScanCodeShort.LCONTROL } ,
            new ControlInputSetting("A", 30, Key.A) { KeyScanCode = ScanCodeShort.KEY_A },
            new ControlInputSetting("S", 31, Key.S) { KeyScanCode = ScanCodeShort.KEY_S },
            new ControlInputSetting("D", 32, Key.D) { KeyScanCode = ScanCodeShort.KEY_D },
            new ControlInputSetting("F", 33, Key.F) { KeyScanCode = ScanCodeShort.KEY_F },
            new ControlInputSetting("G", 34, Key.G) { KeyScanCode = ScanCodeShort.KEY_G },
            new ControlInputSetting("H", 35, Key.H) { KeyScanCode = ScanCodeShort.KEY_H },
            new ControlInputSetting("J", 36, Key.J) { KeyScanCode = ScanCodeShort.KEY_J },
            new ControlInputSetting("K", 37, Key.K) { KeyScanCode = ScanCodeShort.KEY_K },
            new ControlInputSetting("L", 38, Key.L) { KeyScanCode = ScanCodeShort.KEY_L },
            new ControlInputSetting("SEMICOLON", 39, Key.OemSemicolon, StringKey.Semicolon) { KeyScanCode = ScanCodeShort.OEM_1 },
            new ControlInputSetting("APOSTROPHE", 40, Key.OemQuotes, StringKey.Apostrophe) { KeyScanCode = ScanCodeShort.OEM_7 },
            new ControlInputSetting("BACKQUOTE", 41, Key.OemTilde, StringKey.Backquote) { KeyScanCode = ScanCodeShort.OEM_4 },
            new ControlInputSetting("LEFTSHIFT", 42, Key.LeftShift, StringKey.LeftShift) { KeyScanCode = ScanCodeShort.OEM_4 },
            new ControlInputSetting("BACKSLASH", 43, Key.OemBackslash, StringKey.Backslash) { KeyScanCode = ScanCodeShort.OEM_5 },
            new ControlInputSetting("BACKSLASH", 43, Key.Oem5, StringKey.Backslash) { KeyScanCode = ScanCodeShort.OEM_5 }, // Note: Oem5 is backslash on my en-us keyboard but I suspect this will be different for other keyboard layouts/manufacturers
            new ControlInputSetting("Z", 44, Key.Z) { KeyScanCode = ScanCodeShort.KEY_Z },
            new ControlInputSetting("X", 45, Key.X) { KeyScanCode = ScanCodeShort.KEY_X },
            new ControlInputSetting("C", 46, Key.C) { KeyScanCode = ScanCodeShort.KEY_C },
            new ControlInputSetting("V", 47, Key.V) { KeyScanCode = ScanCodeShort.KEY_V },
            new ControlInputSetting("B", 48, Key.B) { KeyScanCode = ScanCodeShort.KEY_B },
            new ControlInputSetting("N", 49, Key.N) { KeyScanCode = ScanCodeShort.KEY_N },
            new ControlInputSetting("M", 50, Key.M) { KeyScanCode = ScanCodeShort.KEY_M },
            new ControlInputSetting("COMMA", 51, Key.OemComma, StringKey.Comma) { KeyScanCode = ScanCodeShort.OEM_COMMA },
            new ControlInputSetting("PERIOD", 52, Key.OemPeriod, StringKey.Period) { KeyScanCode = ScanCodeShort.OEM_PERIOD },
            new ControlInputSetting("SLASH (FORWARD)", 53, Key.OemQuestion, StringKey.Slash) { KeyScanCode = ScanCodeShort.OEM_2 },
            new ControlInputSetting("RIGHTSHIFT", 54, Key.RightShift, StringKey.RightShift) { KeyScanCode = ScanCodeShort.RSHIFT },
            new ControlInputSetting("NUMPADMULTIPLY", 55, Key.Multiply, StringKey.NumpadMultiply) { KeyScanCode = ScanCodeShort.MULTIPLY },
            new ControlInputSetting("LEFTALT", 56, Key.LeftAlt, StringKey.LeftAlt) { KeyScanCode = ScanCodeShort.LMENU },
            new ControlInputSetting("SPACE", 57, Key.Space, StringKey.Space) { KeyScanCode = ScanCodeShort.SPACE },
            new ControlInputSetting("CAPS", 58, Key.CapsLock, StringKey.Caps) { KeyScanCode = ScanCodeShort.CAPITAL },
            new ControlInputSetting("F1", 59, Key.F1) { KeyScanCode = ScanCodeShort.F1 },
            new ControlInputSetting("F2", 60, Key.F2) { KeyScanCode = ScanCodeShort.F2 },
            new ControlInputSetting("F3", 61, Key.F3) { KeyScanCode = ScanCodeShort.F3 },
            new ControlInputSetting("F4", 62, Key.F4) { KeyScanCode = ScanCodeShort.F4 },
            new ControlInputSetting("F5", 63, Key.F5) { KeyScanCode = ScanCodeShort.F5 },
            new ControlInputSetting("F6", 64, Key.F6) { KeyScanCode = ScanCodeShort.F6 },
            new ControlInputSetting("F7", 65, Key.F7) { KeyScanCode = ScanCodeShort.F7 },
            new ControlInputSetting("F8", 66, Key.F8) { KeyScanCode = ScanCodeShort.F8 },
            new ControlInputSetting("F9", 67, Key.F9) { KeyScanCode = ScanCodeShort.F9 },
            new ControlInputSetting("F10", 68, Key.F10) { KeyScanCode = ScanCodeShort.F10 },
            new ControlInputSetting("NUMLOCK", 69, Key.NumLock, StringKey.Numlock) { KeyScanCode = ScanCodeShort.NUMLOCK, KeyIsExtended = true },
            new ControlInputSetting("SCROLLLOCK", 70, Key.Scroll, StringKey.Scrolllock) { KeyScanCode = ScanCodeShort.SCROLL },
            new ControlInputSetting("NUMPAD7", 71, Key.NumPad7, StringKey.Numpad7) { KeyScanCode = ScanCodeShort.NUMPAD7 },
            new ControlInputSetting("NUMPAD8", 72, Key.NumPad8, StringKey.Numpad8) { KeyScanCode = ScanCodeShort.NUMPAD8 },
            new ControlInputSetting("NUMPAD9", 73, Key.NumPad9, StringKey.Numpad9) { KeyScanCode = ScanCodeShort.NUMPAD9 },
            new ControlInputSetting("NUMPADSUBTRACT", 74, Key.Subtract, StringKey.NumpadSubtract) { KeyScanCode = ScanCodeShort.SUBTRACT },
            new ControlInputSetting("NUMPAD4", 75, Key.NumPad4, StringKey.Numpad0) { KeyScanCode = ScanCodeShort.NUMPAD4 },
            new ControlInputSetting("NUMPAD5", 76, Key.NumPad5, StringKey.Numpad5) { KeyScanCode = ScanCodeShort.NUMPAD5 },
            new ControlInputSetting("NUMPAD6", 77, Key.NumPad6, StringKey.Numpad6) { KeyScanCode = ScanCodeShort.NUMPAD6 },
            new ControlInputSetting("NUMPADADD", 78, Key.Add, StringKey.NumpadAdd) { KeyScanCode = ScanCodeShort.ADD },
            new ControlInputSetting("NUMPAD1", 79, Key.NumPad1, StringKey.Numpad1) { KeyScanCode = ScanCodeShort.NUMPAD1 },
            new ControlInputSetting("NUMPAD2", 80, Key.NumPad2, StringKey.Numpad2) { KeyScanCode = ScanCodeShort.NUMPAD2 },
            new ControlInputSetting("NUMPAD3", 81, Key.NumPad3, StringKey.Numpad3) { KeyScanCode = ScanCodeShort.NUMPAD3 },
            new ControlInputSetting("NUMPAD0", 82, Key.NumPad0, StringKey.Numpad0) { KeyScanCode = ScanCodeShort.NUMPAD0 },
            new ControlInputSetting("NUMPADDECIMAL", 83, Key.Decimal, StringKey.NumpadDecimal) { KeyScanCode = ScanCodeShort.DECIMAL },
            new ControlInputSetting("F11", 87, Key.F11) { KeyScanCode = ScanCodeShort.F11 },
            new ControlInputSetting("F12", 88, Key.F12) { KeyScanCode = ScanCodeShort.F12 },
            new ControlInputSetting("NUMPADENTER", 156, Key.Enter, StringKey.NumpadEnter) { KeyScanCode = ScanCodeShort.RETURN, KeyIsExtended = true },
            new ControlInputSetting("RIGHTCTRL", 157, Key.RightCtrl, StringKey.RightCtrl) { KeyScanCode = ScanCodeShort.RCONTROL, KeyIsExtended = true },
            new ControlInputSetting("NUMPADDIVIDE", 181, Key.Divide, StringKey.NumpadDivide) { KeyScanCode = ScanCodeShort.DIVIDE, KeyIsExtended = true },
            new ControlInputSetting("PRTSCN", 183, Key.PrintScreen, StringKey.PrtScn) { KeyScanCode = ScanCodeShort.SNAPSHOT, KeyIsExtended = true },
            new ControlInputSetting("RIGHTALT", 184, Key.RightAlt, StringKey.RightAlt) { KeyScanCode = ScanCodeShort.RMENU, KeyIsExtended = true },
            new ControlInputSetting("PAUSEBREAK", 197, Key.Pause, StringKey.PauseBreak) { KeyScanCode = ScanCodeShort.PAUSE, KeyIsExtended = true },
            new ControlInputSetting("HOME", 199, Key.Home, StringKey.Home) { KeyScanCode = ScanCodeShort.HOME, KeyIsExtended = true },
            new ControlInputSetting("UP", 200, Key.Up, StringKey.Up) { KeyScanCode = ScanCodeShort.UP, KeyIsExtended = true },
            new ControlInputSetting("PAGEUP", 201, Key.PageUp, StringKey.PageUp) { KeyScanCode = ScanCodeShort.PRIOR, KeyIsExtended = true },
            new ControlInputSetting("LEFT", 203, Key.Left, StringKey.Left) { KeyScanCode = ScanCodeShort.LEFT, KeyIsExtended = true },
            new ControlInputSetting("RIGHT", 205, Key.Right, StringKey.Right) { KeyScanCode = ScanCodeShort.RIGHT, KeyIsExtended = true },
            new ControlInputSetting("END", 207, Key.End, StringKey.End) { KeyScanCode = ScanCodeShort.END, KeyIsExtended = true },
            new ControlInputSetting("DOWN", 208, Key.Down, StringKey.Down) { KeyScanCode = ScanCodeShort.DOWN, KeyIsExtended = true },
            new ControlInputSetting("PAGEDOWN", 209, Key.PageDown, StringKey.PageDown) { KeyScanCode = ScanCodeShort.NEXT, KeyIsExtended = true },
            new ControlInputSetting("INSERT", 210, Key.Insert, StringKey.Insert) { KeyScanCode = ScanCodeShort.INSERT, KeyIsExtended = true },
            new ControlInputSetting("DELETE", 211, Key.Delete, StringKey.Delete) { KeyScanCode = ScanCodeShort.DELETE, KeyIsExtended = true },
            new ControlInputSetting("LEFTWINKEY", 219, Key.LWin, StringKey.LeftWinKey) { KeyScanCode = ScanCodeShort.LWIN },
            new ControlInputSetting("RIGHTWINKEY", 220, Key.RWin, StringKey.RightWinKey) { KeyScanCode = ScanCodeShort.RWIN },
            new ControlInputSetting("APPS (CONTEXT MENU)", 221, Key.Apps, StringKey.Apps) { KeyScanCode = ScanCodeShort.APPS },

            // dpad is not actually supported natively in ff8. J8 intercepts the dpad input and presses the corresponding keyboard control
            new ControlInputSetting("DPAD UP", 158, GamePadButton.DPadUp, StringKey.DPadUp),
            new ControlInputSetting("DPAD DOWN", 159, GamePadButton.DPadDown, StringKey.DPadDown),
            new ControlInputSetting("DPAD LEFT", 160, GamePadButton.DPadLeft, StringKey.DPadLeft),
            new ControlInputSetting("DPAD RIGHT", 161, GamePadButton.DPadRight, StringKey.DPadRight),

            new ControlInputSetting("UP", 252, GamePadButton.Up, StringKey.Up),
            new ControlInputSetting("DOWN", 253, GamePadButton.Down, StringKey.Down),
            new ControlInputSetting("LEFT", 254, GamePadButton.Left, StringKey.Left),
            new ControlInputSetting("RIGHT", 255, GamePadButton.Right, StringKey.Right),
            new ControlInputSetting("Button 1", 224, GamePadButton.Button1, StringKey.Button1),
            new ControlInputSetting("Button 2", 225, GamePadButton.Button2, StringKey.Button2),
            new ControlInputSetting("Button 3", 226, GamePadButton.Button3, StringKey.Button3),
            new ControlInputSetting("Button 4", 227, GamePadButton.Button4, StringKey.Button4),
            new ControlInputSetting("Button 5", 228, GamePadButton.Button5, StringKey.Button5),
            new ControlInputSetting("Button 6", 229, GamePadButton.Button6, StringKey.Button6),
            new ControlInputSetting("Button 7", 230, GamePadButton.Button7, StringKey.Button7),
            new ControlInputSetting("Button 8", 231, GamePadButton.Button8, StringKey.Button8),
            new ControlInputSetting("Button 9", 232, GamePadButton.Button9, StringKey.Button9),
            new ControlInputSetting("Button 10", 233, GamePadButton.Button10, StringKey.Button10),
            new ControlInputSetting("Button 11", 234, GamePadButton.Button11, StringKey.Button11),
            new ControlInputSetting("Button 12", 235, GamePadButton.Button12, StringKey.Button12),
            new ControlInputSetting("Button 13", 236, GamePadButton.Button13, StringKey.Button13),
            new ControlInputSetting("Button 14", 237, GamePadButton.Button14, StringKey.Button14),


        };

        public static ControlInputSetting GetControlInputFromKey(Key keyboardInput)
        {
            return ControlInputs.Where(c => c.KeyboardKey.HasValue && c.KeyboardKey.Value == keyboardInput).FirstOrDefault();
        }

        public static ControlInputSetting GetControlInputFromButton(GamePadButton button)
        {
            return ControlInputs.Where(c => c.GamepadInput.HasValue && c.GamepadInput.Value == button).FirstOrDefault();
        }

        private static ControlInputSetting GetControlInputFromConfigValue(ControlType type, string[] file, string key)
        {
            bool isRequestedControlType = false;
            int configValue = 0;

            foreach( string line in file)
            {
                if (line.StartsWith("Keyboard") && type == ControlType.Keyboard) isRequestedControlType = true;
                if (line.StartsWith("Joystick") && type == ControlType.Joystick) isRequestedControlType = true;

                if (line.StartsWith(key) && isRequestedControlType)
                {
                    configValue = int.Parse(line.Substring(line.Length - 3).Trim());
                    break;
                }
            }

            return ControlInputs.Where(c => c.ConfigValue == configValue).FirstOrDefault();
        }

        public static Dictionary<GameControl, string> ControlMapping = new Dictionary<GameControl, string>()
        {
            { GameControl.OK, "1." },
            { GameControl.Exit, "2." },
            { GameControl.Misc, "3." },
            { GameControl.Menu, "4." },
            { GameControl.Toggle, "5." },
            { GameControl.Trigger, "6." },
            { GameControl.RotLt, "7." },
            { GameControl.RotRt, "8." },
            { GameControl.Start, "9." },
            { GameControl.Select, "10." },
            { GameControl.Up, "11." },
            { GameControl.Down, "12." },
            { GameControl.Left, "13." },
            { GameControl.Right, "14." },
        };

        public static ControlConfiguration LoadConfigurationFromFile(string pathToFile)
        {
            ControlConfiguration loaded = new ControlConfiguration();

            string[] inputFile = File.ReadAllLines(pathToFile, Encoding.UTF8);

            loaded.KeyboardInputs.Add(GameControl.OK, GetControlInputFromConfigValue(ControlType.Keyboard, inputFile, ControlMapping[GameControl.OK]));
            loaded.KeyboardInputs.Add(GameControl.Exit, GetControlInputFromConfigValue(ControlType.Keyboard, inputFile, ControlMapping[GameControl.Exit]));
            loaded.KeyboardInputs.Add(GameControl.Misc, GetControlInputFromConfigValue(ControlType.Keyboard, inputFile, ControlMapping[GameControl.Misc]));
            loaded.KeyboardInputs.Add(GameControl.Menu, GetControlInputFromConfigValue(ControlType.Keyboard, inputFile, ControlMapping[GameControl.Menu]));
            loaded.KeyboardInputs.Add(GameControl.Toggle, GetControlInputFromConfigValue(ControlType.Keyboard, inputFile, ControlMapping[GameControl.Toggle]));
            loaded.KeyboardInputs.Add(GameControl.Trigger, GetControlInputFromConfigValue(ControlType.Keyboard, inputFile, ControlMapping[GameControl.Trigger]));
            loaded.KeyboardInputs.Add(GameControl.RotLt, GetControlInputFromConfigValue(ControlType.Keyboard, inputFile, ControlMapping[GameControl.RotLt]));
            loaded.KeyboardInputs.Add(GameControl.RotRt, GetControlInputFromConfigValue(ControlType.Keyboard, inputFile, ControlMapping[GameControl.RotRt]));
            loaded.KeyboardInputs.Add(GameControl.Start, GetControlInputFromConfigValue(ControlType.Keyboard, inputFile, ControlMapping[GameControl.Start]));
            loaded.KeyboardInputs.Add(GameControl.Select, GetControlInputFromConfigValue(ControlType.Keyboard, inputFile, ControlMapping[GameControl.Select]));
            loaded.KeyboardInputs.Add(GameControl.Up, GetControlInputFromConfigValue(ControlType.Keyboard, inputFile, ControlMapping[GameControl.Up]));
            loaded.KeyboardInputs.Add(GameControl.Right, GetControlInputFromConfigValue(ControlType.Keyboard, inputFile, ControlMapping[GameControl.Right]));
            loaded.KeyboardInputs.Add(GameControl.Down, GetControlInputFromConfigValue(ControlType.Keyboard, inputFile, ControlMapping[GameControl.Down]));
            loaded.KeyboardInputs.Add(GameControl.Left, GetControlInputFromConfigValue(ControlType.Keyboard, inputFile, ControlMapping[GameControl.Left]));

            loaded.GamepadInputs.Add(GameControl.OK, GetControlInputFromConfigValue(ControlType.Joystick, inputFile, ControlMapping[GameControl.OK]));
            loaded.GamepadInputs.Add(GameControl.Exit, GetControlInputFromConfigValue(ControlType.Joystick, inputFile, ControlMapping[GameControl.Exit]));
            loaded.GamepadInputs.Add(GameControl.Misc, GetControlInputFromConfigValue(ControlType.Joystick, inputFile, ControlMapping[GameControl.Misc]));
            loaded.GamepadInputs.Add(GameControl.Menu, GetControlInputFromConfigValue(ControlType.Joystick, inputFile, ControlMapping[GameControl.Menu]));
            loaded.GamepadInputs.Add(GameControl.Toggle, GetControlInputFromConfigValue(ControlType.Joystick, inputFile, ControlMapping[GameControl.Toggle]));
            loaded.GamepadInputs.Add(GameControl.Trigger, GetControlInputFromConfigValue(ControlType.Joystick, inputFile, ControlMapping[GameControl.Trigger]));
            loaded.GamepadInputs.Add(GameControl.RotLt, GetControlInputFromConfigValue(ControlType.Joystick, inputFile, ControlMapping[GameControl.RotLt]));
            loaded.GamepadInputs.Add(GameControl.RotRt, GetControlInputFromConfigValue(ControlType.Joystick, inputFile, ControlMapping[GameControl.RotRt]));
            loaded.GamepadInputs.Add(GameControl.Start, GetControlInputFromConfigValue(ControlType.Joystick, inputFile, ControlMapping[GameControl.Start]));
            loaded.GamepadInputs.Add(GameControl.Select, GetControlInputFromConfigValue(ControlType.Joystick, inputFile, ControlMapping[GameControl.Select]));
            loaded.GamepadInputs.Add(GameControl.Up, GetControlInputFromConfigValue(ControlType.Joystick, inputFile, ControlMapping[GameControl.Up]));
            loaded.GamepadInputs.Add(GameControl.Right, GetControlInputFromConfigValue(ControlType.Joystick, inputFile, ControlMapping[GameControl.Right]));
            loaded.GamepadInputs.Add(GameControl.Down, GetControlInputFromConfigValue(ControlType.Joystick, inputFile, ControlMapping[GameControl.Down]));
            loaded.GamepadInputs.Add(GameControl.Left, GetControlInputFromConfigValue(ControlType.Joystick, inputFile, ControlMapping[GameControl.Left]));

            return loaded;
        }

        public static bool SaveConfigurationToFile(string pathToFile, ControlConfiguration configToSave)
        {
            string buffer = $@"Keyboard
1. ""Select""   {configToSave.KeyboardInputs[GameControl.OK].ConfigValue}
2. ""Exit""     {configToSave.KeyboardInputs[GameControl.Exit].ConfigValue}
3. ""Misc""     {configToSave.KeyboardInputs[GameControl.Misc].ConfigValue}
4. ""Menu""     {configToSave.KeyboardInputs[GameControl.Menu].ConfigValue}
5. ""Toggle""   {configToSave.KeyboardInputs[GameControl.Toggle].ConfigValue}
6. ""Trigger""  {configToSave.KeyboardInputs[GameControl.Trigger].ConfigValue}
7. ""RotLt""    {configToSave.KeyboardInputs[GameControl.RotLt].ConfigValue}
8. ""RotRt""    {configToSave.KeyboardInputs[GameControl.RotRt].ConfigValue}
9. ""Start""    {configToSave.KeyboardInputs[GameControl.Start].ConfigValue}
10. ""Select""   {configToSave.KeyboardInputs[GameControl.Select].ConfigValue}
11. ""Up""       {configToSave.KeyboardInputs[GameControl.Up].ConfigValue}
12. ""Down""     {configToSave.KeyboardInputs[GameControl.Down].ConfigValue}
13. ""Left""     {configToSave.KeyboardInputs[GameControl.Left].ConfigValue}
14. ""Right""    {configToSave.KeyboardInputs[GameControl.Right].ConfigValue}
Joystick
1. ""Select""   {configToSave.GamepadInputs[GameControl.OK].ConfigValue}
2. ""Exit""     {configToSave.GamepadInputs[GameControl.Exit].ConfigValue}
3. ""Misc""     {configToSave.GamepadInputs[GameControl.Misc].ConfigValue}
4. ""Menu""     {configToSave.GamepadInputs[GameControl.Menu].ConfigValue}
5. ""Toggle""   {configToSave.GamepadInputs[GameControl.Toggle].ConfigValue}
6. ""Trigger""  {configToSave.GamepadInputs[GameControl.Trigger].ConfigValue}
7. ""RotLt""    {configToSave.GamepadInputs[GameControl.RotLt].ConfigValue}
8. ""RotRt""    {configToSave.GamepadInputs[GameControl.RotRt].ConfigValue}
9. ""Start""    {configToSave.GamepadInputs[GameControl.Start].ConfigValue}
10. ""Select""   {configToSave.GamepadInputs[GameControl.Select].ConfigValue}
11. ""Up""       {configToSave.GamepadInputs[GameControl.Up].ConfigValue}
12. ""Down""     {configToSave.GamepadInputs[GameControl.Down].ConfigValue}
13. ""Left""     {configToSave.GamepadInputs[GameControl.Left].ConfigValue}
14. ""Right""    {configToSave.GamepadInputs[GameControl.Right].ConfigValue}";

            File.WriteAllText(pathToFile, buffer, Encoding.UTF8);

            return true;
        }

        public static bool CopyConfigurationFileAndSaveAsNew(string pathToExistingFile, string newConfigName, ControlConfiguration configToSave)
        {
            // ensure file to copy exists
            if (!File.Exists(pathToExistingFile))
            {
                return false;
            }

            FileInfo existingFile = new FileInfo(pathToExistingFile);
            string newFilePath = Path.Combine(existingFile.DirectoryName, newConfigName);

            // ensure file with same name doesn't already exist
            if (File.Exists(newFilePath))
            {
                return false;
            }

            File.Copy(pathToExistingFile, newFilePath);

            return SaveConfigurationToFile(newFilePath, configToSave);
        }

    }

    /// <summary>
    /// Enum of all possible gamepad buttons available
    /// </summary>
    public enum GamePadButton
    {
        Button1,
        Button2,
        Button3,
        Button4,
        Button5,
        Button6,
        Button7,
        Button8,
        Button9,
        Button10,
        Button11,
        Button12,
        Button13,
        Button14,
        LeftTrigger, // XInput devices do not treat trigger as buttons so these enums are used by XInput devices
        RightTrigger,
        Up,
        Down,
        Left,
        Right,
        DPadUp,
        DPadDown,
        DPadLeft,
        DPadRight,
    }

}



