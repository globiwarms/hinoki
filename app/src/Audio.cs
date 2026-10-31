// Hinoki — Core Audio interop: system microphone mute toggle/status
using System;
using System.Runtime.InteropServices;

namespace Hinoki
{
    public static class Audio
    {
        // CLSIDs / IIDs
        static readonly Guid CLSID_MMDeviceEnumerator = new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E");
        static readonly Guid IID_IMMDeviceEnumerator = new Guid("A95664D2-9614-4F35-A746-DE8DB63617E6");
        static readonly Guid IID_IAudioEndpointVolume = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");

        const int eRender = 0;
        const int eCapture = 1;
        const int eConsole = 0;

        [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        class MMDeviceEnumeratorCom { }

        [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IMMDeviceEnumerator
        {
            int EnumAudioEndpoints(int dataFlow, int dwStateMask, out IntPtr devices);
            int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice endpoint);
            int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string wzId, out IMMDevice device);
            int RegisterEndpointNotificationCallback(IntPtr pClient);
            int UnregisterEndpointNotificationCallback(IntPtr pClient);
        }

        [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IMMDevice
        {
            int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object iface);
            int OpenPropertyStore(int stgmAccess, out IntPtr store);
            int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
            int GetState(out int pdwState);
        }

        [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IAudioEndpointVolume
        {
            int RegisterControlChangeNotify(IntPtr pNotify);
            int UnregisterControlChangeNotify(IntPtr pNotify);
            int GetChannelCount(out uint pnChannelCount);
            int SetMasterVolumeLevel(float fLevelDB, Guid eventContext);
            int SetMasterVolumeLevelScalar(float fLevel, Guid eventContext);
            int GetMasterVolumeLevel(out float pfLevelDB);
            int GetMasterVolumeLevelScalar(out float pfLevel);
            int SetChannelVolumeLevel(uint nChannel, float fLevelDB, Guid eventContext);
            int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, Guid eventContext);
            int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);
            int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);
            int SetMute(bool bMute, Guid eventContext);
            int GetMute(out bool pbMute);
            // remaining methods unused
        }

        static IAudioEndpointVolume GetEndpoint(bool capture)
        {
            var en = (IMMDeviceEnumerator)new MMDeviceEnumeratorCom();
            IMMDevice dev;
            int hr = en.GetDefaultAudioEndpoint(capture ? eCapture : eRender, eConsole, out dev);
            if (hr != 0) Marshal.ThrowExceptionForHR(hr);
            object iface;
            Guid iid = IID_IAudioEndpointVolume;
            hr = dev.Activate(ref iid, 1 /*CLSCTX_ALL*/, IntPtr.Zero, out iface);
            if (hr != 0) Marshal.ThrowExceptionForHR(hr);
            return (IAudioEndpointVolume)iface;
        }

        public static bool IsMicMuted()
        {
            try { bool m; GetEndpoint(true).GetMute(out m); return m; }
            catch { return false; }
        }

        public static void SetMicMuted(bool mute)
        {
            try { GetEndpoint(true).SetMute(mute, Guid.Empty); }
            catch { }
        }

        public static bool IsSystemMuted()
        {
            try { bool m; GetEndpoint(false).GetMute(out m); return m; }
            catch { return false; }
        }
    }
}
