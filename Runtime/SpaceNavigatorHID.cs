using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using System.Collections.Generic;
using System.Data.SqlTypes;
using UnityEngine.InputSystem.Layouts;



#if SPACENAVIGATOR_DEBUG
using System.Linq;
#endif

namespace SpaceNavigatorDriver
{
    //public class SpaceMouse : HID
    //{
    //    public ButtonControl Button1 { get; protected set; }
    //    public ButtonControl Button2 { get; protected set; }
    //    public Vector3Control Rotation { get; protected set; }

    //    [InputControl(name = "translation", format = "VC3S", layout = "Vector3", displayName = "Translation")]
    //    [InputControl(name = "translation/x", format = "SHRT")]
    //    [InputControl(name = "translation/y", format = "SHRT")]
    //    [InputControl(name = "translation/z", format = "SHRT")]
    //    public Vector3Control Translation { get; protected set; }
    //}

    //[InputControlLayout(stateType = typeof(MergedReports))]
    public class SpaceNavigatorHID : HID, IInputStateCallbackReceiver
    {
        internal const int ReportSizeMax = 33;
        internal const int ReportCountMax = 3;
        internal const int StateSizeMax = ReportSizeMax * ReportCountMax;

        public override unsafe void WriteValueFromBufferIntoState(void* bufferPtr, int bufferSize, void* statePtr)
        {
            base.WriteValueFromBufferIntoState(bufferPtr, bufferSize, statePtr);
        }

        [StructLayout(LayoutKind.Explicit, Size = StateSizeMax)]
        public struct MergedReports
        {
        }

        public static SpaceNavigatorHID current { get; private set; }

        public ButtonControl Button1 { get; protected set; }
        public ButtonControl Button2 { get; protected set; }
        public Vector3Control Rotation { get; protected set; }
        public Vector3Control Translation { get; protected set; }

        private Dictionary<int, int> _reportSizeMap;

        public enum LedStatus { On, Off }

        protected override void FinishSetup()
        {
            base.FinishSetup();

            Button1 = GetChildControl<ButtonControl>("button1");
            Button2 = GetChildControl<ButtonControl>("button2");
            Rotation = GetChildControl<Vector3Control>("rotation");
            Translation = GetChildControl<Vector3Control>("translation");

            _reportSizeMap = HIDDeviceDescriptor.FromJson(description.capabilities)
                .GetReportSizeMap(ReportCountMax);
        }

        public override void MakeCurrent()
        {
            base.MakeCurrent();

            if (current == this)
                return;

            current = this;

            DebugLog($"Current instance: {displayName}");
        }

        public void OnNextUpdate()
        { }

        private byte[] stuff = new byte[StateSizeMax];

        public unsafe void OnStateEvent(InputEventPtr eventPtr)
        {
            //if (eventPtr.IsA<StateEvent>() == false)
            //    return;

            //var stateEventPtr = StateEvent.From(eventPtr);
            //if (stateEventPtr->stateFormat != new FourCC('H', 'I', 'D'))
            //    return;

            //var reportPtr = (byte*)stateEventPtr->state;
            //var reportId = *reportPtr;

            ////var dse = DeltaStateEvent.From(this, out var dseip);
            ////var dseb = new byte[dse.Length];
            ////dse.CopyTo(dseb);
            ////DebugLog(dseb.Length + "\n" + Hex(dseb));

            //DebugLog(StateEvent.GetEventSizeWithPayload<MergedReports>().ToString());

            ////var se = StateEvent.From(this, out var seip);
            ////var seb = new byte[se.Length];
            ////se.CopyTo(seb);
            ////DebugLog(seb.Length + "\n" + Hex(seb));


            //StateEvent skadfskf = *stateEventPtr;

            //var sebo = new byte[valueSizeInBytes];
            //fixed (byte* seboptr = sebo)
            //{
            //    UnsafeUtility.MemCpy(seboptr, stateEventPtr, valueSizeInBytes);
            //}
            //DebugLog(sebo.Length + "\n" + Hex(sebo));


            //InputState.Change(this, eventPtr, InputUpdateType.Default);
            //return;
            //------------------

            if (eventPtr.IsA<DeltaStateEvent>())
                return;

            var stateEventPtr = StateEvent.From(eventPtr);
            if (stateEventPtr->stateFormat != new FourCC('H', 'I', 'D'))
                return;

            var reportPtr = (byte*)stateEventPtr->state;
            var reportId = *reportPtr;

            if (reportId < 1 || reportId > ReportCountMax)
                return;

            if (_reportSizeMap == null)
                return;

            // Get pointer to current state.
            //var newState = (byte*)currentStatePtr + stateBlock.byteOffset;

            //var oldState = (byte*)currentStatePtr + stateBlock.byteOffset;
            //var newStateI = new MergedReports();
            //var newState = (byte*)&newStateI;
            var oink = new MergedReports();
            byte* oinkPtr = (byte*)&oink;
            fixed (byte* newState = stuff)
            {
                //UnsafeUtility.MemCpy(newState, oldState, (stateBlock.sizeInBits + 7) >> 3);

                // Merge incoming report into the current state.
                // Use reportId to map to a specific block inside the state struct.
                var offset = (uint)(ReportSizeMax * (reportId - 1));
                var length2 = _reportSizeMap[reportId];

                var length = stateEventPtr->stateSizeInBytes;
                var maxLength = (stateBlock.sizeInBits + 7) >> 3;
                // Make sure not to not exceed state block boundaries. Its size is not equal to the state's struct size!
                // Guess: Size might be calculated by last element offset + last element size, byte-aligned.
                if (offset + length > maxLength)
                {
                    length = maxLength - offset;
                }

                UnsafeUtility.MemCpy(newState + offset, reportPtr, length);
#if SPACENAVIGATOR_DEBUG
                DebugLog($"Report {reportId} {offset} {length} \n" + Hex(reportPtr, length2, length2));

                DebugLog($"Copied report {reportId} {stateEventPtr->stateSizeInBytes}:\n" + Hex(newState, StateSizeMax, ReportSizeMax));
#endif
                //var se = InputEventPtr.From(StateEvent.FromDefaultStateFor())
                //var e = new InputEvent(eventPtr.type, StateSizeMax, this.deviceId, -1);
                //var sep = StateEvent.FromDefaultStateFor(this, out var ep);
                //UnsafeUtility.MemCpy(sep, newState, StateSizeMax);

                //stateEventPtr->stateFormat = new FourCC("UGA");

                //stateEventPtr->

                //var ep = new InputEvent(eventPtr.type, StateEvent.GetEventSizeWithPayload<MergedReports>(), deviceId);

                UnsafeUtility.MemCpy(oinkPtr, newState, StateSizeMax);

                DebugLog($"Copied report {reportId} {stateEventPtr->stateSizeInBytes}:\n" + Hex(oinkPtr, StateSizeMax, ReportSizeMax));

                var se = StateEvent.From(this, out var seip);
                var septr = (StateEvent*)se.GetUnsafePtr();
                UnsafeUtility.MemCpy(septr + 24, oinkPtr, this.valueSizeInBytes);

                //UnsafeUtility.MemCpy(stateEventPtr + 24, oinkPtr, 68);

                //InputState.Change(this, seip);
                InputState.Change(this, oink, InputUpdateType.Default);
            }

        }

        public unsafe bool GetStateOffsetForEvent(InputControl control, InputEventPtr eventPtr, ref uint offset)
        {
            if (eventPtr.IsA<DeltaStateEvent>())
                return false;

            var stateEventPtr = StateEvent.From(eventPtr);
            if (stateEventPtr->stateFormat != new FourCC('H', 'I', 'D'))
                return false;

            var reportPtr = (byte*)stateEventPtr->state;
            var reportId = *reportPtr;

            if (reportId < 1 || reportId > ReportCountMax)
                return false;

            offset = (uint)(ReportSizeMax * (reportId - 1));

            return true;
        }

        internal static void DebugLog(string msg)
        {
#if SPACENAVIGATOR_DEBUG
            UnityEngine.Debug.Log($"{nameof(SpaceNavigatorHID)}: {msg}");
#endif
        }

        public void SetLEDStatus(LedStatus value)
        {
            throw new NotImplementedException();
        }

#if SPACENAVIGATOR_DEBUG
        protected override unsafe long ExecuteCommand(InputDeviceCommand* commandPtr)
        {
            var type = commandPtr->type;
            DebugLog($"ExecuteCommand: {type}");
            return base.ExecuteCommand(commandPtr);
        }

        private unsafe string Hex(void* newState, int length, int stride = -1)
        {
            byte[] b = new byte[length];
            fixed (byte* ptr = &b[0])
            {
                UnsafeUtility.MemCpy(ptr, newState, length);
            }

            return Hex(b, length, stride);
        }

        private string Hex(byte[] bytes, int length = -1, int stride = -1)
        {
            if (length < 0)
                length = bytes.Length;

            if (stride < 0)
                stride = length;

            var hexString = string.Join(
                "\n",
                Enumerable.Range(0, length / stride)
                    .Select(i => BitConverter.ToString(bytes.Skip(i * stride).Take(stride).ToArray()).Replace('-', ' ')));

            return hexString;
        }
#endif
    }
}