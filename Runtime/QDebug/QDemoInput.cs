using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace QTool
{

	public static class QDemoInput
	{

		public static bool Attack
		{
			get
			{
#if ENABLE_INPUT_SYSTEM
				return UnityEngine.InputSystem.Mouse.current.leftButton.isPressed;
#else
				return Input.GetKey(KeyCode.Mouse0);
#endif
			}
		}
		public static Vector2 PointerPosition
		{
			get
			{
#if ENABLE_INPUT_SYSTEM
				return UnityEngine.InputSystem.Mouse.current.position.ReadValue();
#else
				return Input.mousePosition;
#endif
			}
		}
		public static Vector2 MoveDirection
		{
			get
			{
#if ENABLE_INPUT_SYSTEM
				var keyboard = UnityEngine.InputSystem.Keyboard.current;
				return new Vector2(
					keyboard.dKey.isPressed ? 1 : (keyboard.aKey.isPressed ? -1 : 0),
					keyboard.wKey.isPressed ? 1 : (keyboard.sKey.isPressed ? -1 : 0));
#else
				return new Vector2(
                Input.GetKey(KeyCode.D) ? 1 : (Input.GetKey(KeyCode.A) ? -1 : 0),
                Input.GetKey(KeyCode.W) ? 1 : (Input.GetKey(KeyCode.S) ? -1 : 0));
#endif
			}
		}
		public static bool Space
		{
			get
			{
#if ENABLE_INPUT_SYSTEM
				return UnityEngine.InputSystem.Keyboard.current.spaceKey.isPressed;
#else
				return Input.GetKey(KeyCode.Space);
#endif
			}
		}
		public static bool F
		{
			get
			{

#if ENABLE_INPUT_SYSTEM
				return UnityEngine.InputSystem.Keyboard.current.fKey.isPressed;
#else
				return Input.GetKey(KeyCode.F);
#endif
			}
		}
		public static bool Shift
		{
			get
			{
				
#if ENABLE_INPUT_SYSTEM
				return UnityEngine.InputSystem.Keyboard.current.shiftKey.isPressed;
#else
				return Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift);
#endif
			}
		}
		public static bool Ctrl
		{
			get
			{
				
#if ENABLE_INPUT_SYSTEM
				return UnityEngine.InputSystem.Keyboard.current.ctrlKey.isPressed;
#else
				return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
#endif
			}
		}
		public static bool Enter
		{
			get
			{

			
#if ENABLE_INPUT_SYSTEM
				return UnityEngine.InputSystem.Keyboard.current.enterKey.isPressed;
#else	
				return Input.GetKey(KeyCode.KeypadEnter);
#endif
			}
		}
		public static bool ESC
		{
			get
			{
				
#if ENABLE_INPUT_SYSTEM
				return UnityEngine.InputSystem.Keyboard.current.escapeKey.isPressed;
#else
				return Input.GetKey(KeyCode.Escape);
#endif
			}
		}
	}

}
