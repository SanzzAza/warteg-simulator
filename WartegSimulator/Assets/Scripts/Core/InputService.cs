using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Warteg
{
    /// <summary>
    /// Lapisan input tunggal. Memakai Unity Input System (package baru) bila aktif,
    /// dan otomatis jatuh ke Input Manager lama bila project memakai setting "Old".
    /// </summary>
    public static class InputService
    {
#if !ENABLE_INPUT_SYSTEM
        static Vector3 lastMousePos;
#endif

        public static Vector2 MoveAxis()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb == null) return Vector2.zero;
            float x = 0f, y = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;
            return new Vector2(x, y);
#else
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
        }

        public static bool RotateLeft()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && kb.qKey.isPressed;
#else
            return Input.GetKey(KeyCode.Q);
#endif
        }

        public static bool RotateRight()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && kb.eKey.isPressed;
#else
            return Input.GetKey(KeyCode.E);
#endif
        }

        public static bool InteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb == null) return false;
            return kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space);
#endif
        }

        public static bool MenuPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        public static bool PointerDown()
        {
#if ENABLE_INPUT_SYSTEM
            bool mouseDown = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool touchDown = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            return mouseDown || touchDown;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        public static bool PointerUp()
        {
#if ENABLE_INPUT_SYSTEM
            bool mouseUp = Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
            bool touchUp = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
            return mouseUp || touchUp;
#else
            return Input.GetMouseButtonUp(0);
#endif
        }

        public static bool PointerHeld()
        {
#if ENABLE_INPUT_SYSTEM
            bool mouseHeld = Mouse.current != null && Mouse.current.leftButton.isPressed;
            bool touchHeld = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed;
            return mouseHeld || touchHeld;
#else
            return Input.GetMouseButton(0);
#endif
        }

        public static Vector2 PointerPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                return Touchscreen.current.primaryTouch.position.ReadValue();
            if (Mouse.current != null) return Mouse.current.position.ReadValue();
            return Vector2.zero;
#else
            return Input.mousePosition;
#endif
        }

        public static Vector2 PointerDelta()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) return Mouse.current.delta.ReadValue();
            if (Touchscreen.current != null) return Touchscreen.current.primaryTouch.delta.ReadValue();
            return Vector2.zero;
#else
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 12f;
#endif
        }

        public static float ScrollDelta()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) return Mouse.current.scroll.ReadValue().y * 0.01f;
            return 0f;
#else
            return Input.mouseScrollDelta.y;
#endif
        }

        public static bool PointerOverUI()
        {
            return UnityEngine.EventSystems.EventSystem.current != null &&
                   UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        }
    }
}
