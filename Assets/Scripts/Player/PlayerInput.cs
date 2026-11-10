using UnityEngine;
using UnityEngine.InputSystem;

namespace Sanatorio
{
    /// <summary>
    /// Controles twin-stick montados em código (sem asset): teclado + mouse e
    /// controle (Gamepad API do navegador no WebGL). O último dispositivo usado
    /// decide os rótulos dos botões nas dicas ("F" ou "A").
    /// </summary>
    public class PlayerInput
    {
        public readonly InputAction Move;
        public readonly InputAction AimStick;
        public readonly InputAction Point;
        public readonly InputAction Fire;
        public readonly InputAction Reload;
        public readonly InputAction Interact;
        public readonly InputAction Swap;
        public readonly InputAction Slot1;
        public readonly InputAction Slot2;
        public readonly InputAction Grenade;
        public readonly InputAction Knife;
        public readonly InputAction Betty;
        public readonly InputAction Pause;

        public bool UsingGamepad { get; private set; }
        public string InteractLabel => UsingGamepad ? "A" : "F";

        private Vector2 lastMouse;

        public PlayerInput()
        {
            Move = new InputAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            Move.AddBinding("<Gamepad>/leftStick");

            AimStick = new InputAction("AimStick", InputActionType.Value, "<Gamepad>/rightStick");
            Point = new InputAction("Point", InputActionType.Value, "<Pointer>/position");

            Fire = Button("Fire", "<Mouse>/leftButton", "<Gamepad>/rightTrigger");
            Reload = Button("Reload", "<Keyboard>/r", "<Gamepad>/buttonWest");
            Interact = Button("Interact", "<Keyboard>/f", "<Gamepad>/buttonSouth");
            Interact.AddBinding("<Keyboard>/e");
            Swap = new InputAction("Swap", InputActionType.Button, "<Gamepad>/buttonNorth");
            Slot1 = Button("Slot1", "<Keyboard>/1", "<Keyboard>/numpad1");
            Slot2 = Button("Slot2", "<Keyboard>/2", "<Keyboard>/numpad2");
            Grenade = Button("Grenade", "<Keyboard>/g", "<Gamepad>/leftShoulder");
            Knife = Button("Knife", "<Keyboard>/v", "<Gamepad>/rightShoulder");
            Knife.AddBinding("<Mouse>/rightButton");
            Betty = Button("Betty", "<Keyboard>/b", "<Gamepad>/leftTrigger");
            Pause = Button("Pause", "<Keyboard>/escape", "<Gamepad>/start");
            Pause.AddBinding("<Keyboard>/p");
        }

        private static InputAction Button(string name, string a, string b)
        {
            InputAction act = new InputAction(name, InputActionType.Button);
            act.AddBinding(a);
            act.AddBinding(b);
            return act;
        }

        private InputAction[] AllActions => new[] { Move, AimStick, Point, Fire, Reload, Interact, Swap, Slot1, Slot2, Grenade, Knife, Betty, Pause };

        public void Enable()
        {
            foreach (InputAction a in AllActions) a.Enable();
        }

        public void Disable()
        {
            foreach (InputAction a in AllActions)
            {
                a.Disable();
                a.Dispose();
            }
        }

        /// <summary>Atualiza qual dispositivo o jogador está usando agora.</summary>
        public void Poll()
        {
            Gamepad pad = Gamepad.current;
            if (pad != null && (pad.leftStick.ReadValue().sqrMagnitude > 0.1f || pad.rightStick.ReadValue().sqrMagnitude > 0.1f ||
                                pad.rightTrigger.ReadValue() > 0.3f || pad.buttonSouth.isPressed))
            {
                UsingGamepad = true;
            }

            Vector2 mouse = Point.ReadValue<Vector2>();
            bool keys = Keyboard.current != null && Keyboard.current.anyKey.isPressed;
            if ((mouse - lastMouse).sqrMagnitude > 4f || keys || (Mouse.current != null && Mouse.current.leftButton.isPressed))
            {
                UsingGamepad = false;
            }

            lastMouse = mouse;
        }
    }
}
