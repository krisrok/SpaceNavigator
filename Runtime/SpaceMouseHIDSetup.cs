using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using static UnityEngine.InputSystem.HID.HID;

namespace SpaceNavigatorDriver
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    internal static class SpaceMouseHIDSetup
    {
        private static bool _isInited;
        private static int _waitCounter = 0;

        static SpaceMouseHIDSetup()
        {
            Init();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Init()
        {
            if (_isInited)
                return;

            _isInited = true;

#if UNITY_EDITOR
            // Hack to delay the actual initialization by 1 frame _only_ right after editor startup. Why:
            // If we don't delay after startup the auto-generated HID layout will be applied (until domain reload).
            // If we delay all the time there'll be some ugly InvalidCastExceptions because of layout mismatches.
            if (EditorApplication.isPlayingOrWillChangePlaymode == false &&
                InputSystem.devices.Any(d => MatchDeviceShallowPass(d.description)) == false)
            {
                _waitCounter = 1;
                EditorApplication.update += LateInit;
                return;
            }
#endif

            RegisterLayout();
        }

#if UNITY_EDITOR
        private static void LateInit()
        {
            if (_waitCounter-- > 0)
                return;

            EditorApplication.update -= LateInit;

            RegisterLayout();
        }
#endif

        private static void RegisterLayout()
        {
            // Register our handler.
            DebugLog("Register onFindLayoutForDevice handler");
            InputSystem.onFindLayoutForDevice += InputSystem_onFindLayoutForDevice;
        }

        /// <summary>
        /// Should be fast in recognizing possible matching devices.
        /// </summary>
        /// <param name="description"></param>
        /// <param name="executeDeviceCommand"></param>
        /// <returns></returns>
        private static bool MatchDeviceShallowPass(InputDeviceDescription description)
        {
            if (description.interfaceName != "HID")
                return false;

            // Use the supplied descriptor here.
            // It might still suffice to identify the device as non-matching.
            var hidDescriptor = HIDDeviceDescriptor.FromJson(description.capabilities);

            // TODO: Match against list of compatible vendor and product ids, maybe from https://github.com/openantz/antz/wiki/3D-Mouse#developers
            if (hidDescriptor.vendorId != 0x256f)
                return false;

            return true;
        }

        /// <summary>
        /// Performs a more thorough analysis of the device.
        /// The <paramref name="description"/>'s capabilities field might be set by this method if needed.
        /// </summary>
        /// <param name="description"></param>
        /// <param name="executeDeviceCommand"></param>
        /// <returns></returns>
        private static bool MatchDeviceDeepPass(ref InputDeviceDescription description, ref HIDDeviceDescriptor hidDescriptor, InputDeviceExecuteCommandDelegate executeDeviceCommand)
        {
            hidDescriptor = HIDDeviceDescriptor.FromJson(description.capabilities);

            // Read the full descriptor if it does not contain any elements
            // We need the elements to construct the layout
            if (hidDescriptor.elements == null || hidDescriptor.elements.Length == 0)
                hidDescriptor = HIDHelpers.ReadHIDDeviceDescriptor(ref description, executeDeviceCommand);

            if (hidDescriptor.elements == null)
            {
                Debug.LogError("Could not read HID descriptor");
                return false;
            }

            return true;
        }

        private static string InputSystem_onFindLayoutForDevice(ref InputDeviceDescription description, string matchedLayout, InputDeviceExecuteCommandDelegate executeDeviceCommand)
        {
            HIDDeviceDescriptor hidDescriptor = new HIDDeviceDescriptor();

            if (MatchDeviceShallowPass(description) == false)
                return null;

            if (MatchDeviceDeepPass(ref description, ref hidDescriptor, executeDeviceCommand) == false)
                return null;

            DebugLog($"Found {description.product} and descriptor:\n{hidDescriptor.ToJson()}");

            return null;
        }

#if UNITY_EDITOR
        private static void DelayRemove()
        {
            if (_waitCounter-- > 0)
                return;

            EditorApplication.update -= DelayRemove;
        }
#endif

        [System.Diagnostics.DebuggerHidden]
        [System.Diagnostics.DebuggerNonUserCode]
        [System.Diagnostics.DebuggerStepThrough]
        private static void DebugLog(string msg)
        {
            Debug.Log($"{nameof(SpaceMouseHIDSetup)}: {msg}");
        }
    }
}