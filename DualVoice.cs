using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DualVoiceApp {

    // ==========================================
    // WASAPI COM INTERFACES & DEFINITIONS
    // ==========================================

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport]
    public interface IMMDeviceEnumerator {
        int EnumAudioEndpoints(int dataFlow, int dwStateMask, out IMMDeviceCollection ppDevices);
        int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppEndpoint);
        int GetDevice(string pwstrId, out IMMDevice ppDevice);
        int RegisterEndpointNotificationCallback(IntPtr pClient);
        int UnregisterEndpointNotificationCallback(IntPtr pClient);
    }

    [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport]
    public interface IMMDeviceCollection {
        int GetCount(out int pcDevices);
        int Item(int nDevice, out IMMDevice ppDevice);
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport]
    public interface IMMDevice {
        int Activate(ref Guid iid, int dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.Interface)] out object ppInterface);
        int OpenPropertyStore(int stgmAccess, out IPropertyStore ppProperties);
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppstrId);
        int GetState(out int pdwState);
    }

    [Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport]
    public interface IPropertyStore {
        int GetCount(out int cProps);
        int GetAt(int iProp, out PropertyKey pkey);
        int GetValue(ref PropertyKey key, out PropVariant pv);
        int SetValue(ref PropertyKey key, ref PropVariant propvar);
        int Commit();
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PropertyKey {
        public Guid fmtid;
        public int pid;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct PropVariant {
        [FieldOffset(0)] public short vt;
        [FieldOffset(8)] public IntPtr ptrVal;
    }

    [Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport]
    public interface IAudioClient {
        int Initialize(int shareMode, uint streamFlags, long hnsBufferDuration, long hnsPeriodicity, IntPtr pFormat, ref Guid audioSessionGuid);
        int GetBufferSize(out uint pNumBufferFrames);
        int GetStreamLatency(out long phnsLatency);
        int GetCurrentPadding(out uint pNumPaddingFrames);
        int IsFormatSupported(int shareMode, IntPtr pFormat, out IntPtr ppClosestMatch);
        int GetMixFormat(out IntPtr ppDeviceFormat);
        int GetDevicePeriod(out long phnsDefaultDevicePeriod, out long phnsMinimumDevicePeriod);
        int Start();
        int Stop();
        int Reset();
        int SetEventHandle(IntPtr eventHandle);
        int GetService(ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object ppv);
    }

    [Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport]
    public interface IAudioCaptureClient {
        int GetBuffer(out IntPtr ppData, out uint pNumFramesToRead, out uint pdwFlags, out ulong pu64DevicePosition, out ulong pu64QPCPosition);
        int ReleaseBuffer(uint numFramesRead);
        int GetNextPacketSize(out uint pNumFramesInNextPacket);
    }

    [Guid("F294ACFC-3146-4483-A7BF-ADDCA7C260E2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport]
    public interface IAudioRenderClient {
        int GetBuffer(int numFramesRequested, out IntPtr ppData);
        int ReleaseBuffer(int numFramesWritten, uint dwFlags);
    }

    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport]
    public interface IAudioEndpointVolume {
        int RegisterControlChangeNotify(IntPtr pNotify);
        int UnregisterControlChangeNotify(IntPtr pNotify);
        int GetChannelCount(out int pnChannelCount);
        int SetMasterVolumeLevel(float fLevelDB, ref Guid pguidEventContext);
        int SetMasterVolumeLevelScalar(float fLevel, ref Guid pguidEventContext);
        int GetMasterVolumeLevel(out float pfLevelDB);
        int GetMasterVolumeLevelScalar(out float pfLevel);
        int SetChannelVolumeLevel(uint nChannel, float fLevelDB, ref Guid pguidEventContext);
        int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, ref Guid pguidEventContext);
        int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);
        int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid pguidEventContext);
        int GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
        int GetVolumeStepInfo(out uint pnStep, out uint pnStepCount);
        int VolumeStepUp(ref Guid pguidEventContext);
        int VolumeStepDown(ref Guid pguidEventContext);
        int QueryHardwareSupport(out uint pdwHardwareSupportMask);
        int GetVolumeRange(out float pflVolumeMindB, out float pflVolumeMaxdB, out float pflVolumeIncrementdB);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WAVEFORMATEX {
        public ushort wFormatTag;
        public ushort nChannels;
        public uint nSamplesPerSec;
        public uint nAvgBytesPerSec;
        public ushort nBlockAlign;
        public ushort wBitsPerSample;
        public ushort cbSize;
    }

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    public class MMDeviceEnumeratorComObject { }

    // ==========================================
    // DATA MODELS
    // ==========================================

    public class AudioDeviceInfo {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsDefault { get; set; }

        public override string ToString() {
            return Name + (IsDefault ? " (Varsayılan)" : "");
        }
    }

    // ==========================================
    // AUDIO ENGINE (WASAPI RESILIENT MIRRORING)
    // ==========================================

    public class AudioEngine {
        private Thread _workerThread;
        private volatile bool _isRunning = false;
        private string _dev1Id;
        private string _dev2Id;

        private IAudioEndpointVolume _epv1;
        private IAudioEndpointVolume _epv2;

        public event Action<string> OnStatusChanged;
        public bool SyncVolumes { get; set; }

        public static List<AudioDeviceInfo> GetActiveDevices() {
            var list = new List<AudioDeviceInfo>();
            try {
                var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
                string defaultId = "";
                IMMDevice defDev;
                if (enumerator.GetDefaultAudioEndpoint(0, 1, out defDev) == 0 && defDev != null) {
                    defDev.GetId(out defaultId);
                }

                IMMDeviceCollection coll;
                if (enumerator.EnumAudioEndpoints(0, 1, out coll) == 0 && coll != null) {
                    int count;
                    coll.GetCount(out count);
                    PropertyKey PKEY_FriendlyName = new PropertyKey {
                        fmtid = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"),
                        pid = 14
                    };

                    for (int i = 0; i < count; i++) {
                        IMMDevice dev;
                        if (coll.Item(i, out dev) == 0 && dev != null) {
                            string id;
                            dev.GetId(out id);
                            IPropertyStore store;
                            dev.OpenPropertyStore(0, out store);
                            PropVariant pv;
                            store.GetValue(ref PKEY_FriendlyName, out pv);
                            string name = Marshal.PtrToStringUni(pv.ptrVal);
                            if (string.IsNullOrEmpty(name)) name = "Ses Aygıtı " + (i + 1);

                            list.Add(new AudioDeviceInfo {
                                Id = id,
                                Name = name,
                                IsDefault = (id == defaultId)
                            });
                        }
                    }
                }
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine("GetActiveDevices error: " + ex);
            }
            return list;
        }

        public void SetDevice1(string devId) {
            _dev1Id = devId;
            InitVolume1();
            RestartBridge();
        }

        public void SetDevice2(string devId) {
            _dev2Id = devId;
            InitVolume2();
            RestartBridge();
        }

        public void ClearDevice1() {
            _dev1Id = null;
            _epv1 = null;
            RestartBridge();
        }

        public void ClearDevice2() {
            _dev2Id = null;
            _epv2 = null;
            RestartBridge();
        }

        private void InitVolume1() {
            _epv1 = null;
            if (string.IsNullOrEmpty(_dev1Id)) return;
            try {
                var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
                IMMDevice dev;
                if (enumerator.GetDevice(_dev1Id, out dev) == 0 && dev != null) {
                    Guid iid = typeof(IAudioEndpointVolume).GUID;
                    object o;
                    dev.Activate(ref iid, 1, IntPtr.Zero, out o);
                    _epv1 = (IAudioEndpointVolume)o;
                }
            } catch { }
        }

        private void InitVolume2() {
            _epv2 = null;
            if (string.IsNullOrEmpty(_dev2Id)) return;
            try {
                var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
                IMMDevice dev;
                if (enumerator.GetDevice(_dev2Id, out dev) == 0 && dev != null) {
                    Guid iid = typeof(IAudioEndpointVolume).GUID;
                    object o;
                    dev.Activate(ref iid, 1, IntPtr.Zero, out o);
                    _epv2 = (IAudioEndpointVolume)o;
                }
            } catch { }
        }

        public float GetVolume1() {
            if (_epv1 != null) {
                try {
                    float v;
                    int hr = _epv1.GetMasterVolumeLevelScalar(out v);
                    if (hr == 0) return v;
                } catch { }
            }
            return 0.5f;
        }

        public float GetVolume2() {
            if (_epv2 != null) {
                try {
                    float v;
                    int hr = _epv2.GetMasterVolumeLevelScalar(out v);
                    if (hr == 0) return v;
                } catch { }
            }
            return 0.5f;
        }

        public void SetVolume1(float v) {
            if (_epv1 != null) {
                try {
                    Guid ctx = Guid.Empty;
                    _epv1.SetMasterVolumeLevelScalar(v, ref ctx);
                } catch { }
            }
        }

        public void SetVolume2(float v) {
            if (_epv2 != null) {
                try {
                    Guid ctx = Guid.Empty;
                    _epv2.SetMasterVolumeLevelScalar(v, ref ctx);
                } catch { }
            }
        }

        private void RestartBridge() {
            StopBridge();
            if (!string.IsNullOrEmpty(_dev1Id) && !string.IsNullOrEmpty(_dev2Id)) {
                StartBridge();
            } else if (!string.IsNullOrEmpty(_dev1Id) || !string.IsNullOrEmpty(_dev2Id)) {
                if (OnStatusChanged != null) OnStatusChanged("1 Kulaklık Seçildi · Diğerini de seçin");
            } else {
                if (OnStatusChanged != null) OnStatusChanged("Beklemede · İki kulaklığı da seçin");
            }
        }

        private void StartBridge() {
            _isRunning = true;
            _workerThread = new Thread(AudioBridgeLoop) {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal,
                Name = "DualVoiceBridge"
            };
            _workerThread.Start();
        }

        public void StopBridge() {
            _isRunning = false;
            if (_workerThread != null) {
                try { _workerThread.Join(400); } catch { }
                _workerThread = null;
            }
        }

        private void AudioBridgeLoop() {
            if (OnStatusChanged != null) OnStatusChanged("Çift Ses Aktif · Canlı İletim Başlıyor");

            while (_isRunning) {
                IAudioClient capAudioClient = null;
                IAudioClient renAudioClient = null;
                try {
                    var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
                    IMMDevice dev1, dev2;
                    if (enumerator.GetDevice(_dev1Id, out dev1) != 0 || dev1 == null ||
                        enumerator.GetDevice(_dev2Id, out dev2) != 0 || dev2 == null) {
                        Thread.Sleep(500);
                        continue;
                    }

                    IMMDevice defDev;
                    string defId = "";
                    if (enumerator.GetDefaultAudioEndpoint(0, 1, out defDev) == 0 && defDev != null) {
                        defDev.GetId(out defId);
                    }

                    IMMDevice sourceDevice = dev1;
                    IMMDevice targetDevice = dev2;

                    if (defId == _dev2Id) {
                        sourceDevice = dev2;
                        targetDevice = dev1;
                    } else if (defId != _dev1Id && defDev != null) {
                        sourceDevice = defDev;
                        targetDevice = dev2;
                    }

                    Guid iidClient = typeof(IAudioClient).GUID;
                    object oCapClient, oRenClient;
                    if (sourceDevice.Activate(ref iidClient, 1, IntPtr.Zero, out oCapClient) != 0 ||
                        targetDevice.Activate(ref iidClient, 1, IntPtr.Zero, out oRenClient) != 0) {
                        Thread.Sleep(500);
                        continue;
                    }

                    capAudioClient = (IAudioClient)oCapClient;
                    renAudioClient = (IAudioClient)oRenClient;

                    IntPtr capMixFormat;
                    capAudioClient.GetMixFormat(out capMixFormat);
                    WAVEFORMATEX capFmt = (WAVEFORMATEX)Marshal.PtrToStructure(capMixFormat, typeof(WAVEFORMATEX));

                    Guid sessionGuid = Guid.Empty;
                    int hrCap = capAudioClient.Initialize(0, 0x00020000, 1000000, 0, capMixFormat, ref sessionGuid);
                    if (hrCap != 0) {
                        Thread.Sleep(500);
                        continue;
                    }

                    uint renFlags = 0x80000000 | 0x08000000;
                    int hrRen = renAudioClient.Initialize(0, renFlags, 1000000, 0, capMixFormat, ref sessionGuid);
                    if (hrRen != 0) {
                        IntPtr renMixFormat;
                        renAudioClient.GetMixFormat(out renMixFormat);
                        hrRen = renAudioClient.Initialize(0, 0, 1000000, 0, renMixFormat, ref sessionGuid);
                    }

                    if (hrRen != 0) {
                        Thread.Sleep(500);
                        continue;
                    }

                    uint renBufferSize;
                    renAudioClient.GetBufferSize(out renBufferSize);

                    Guid iidCap = typeof(IAudioCaptureClient).GUID;
                    object oCap;
                    capAudioClient.GetService(ref iidCap, out oCap);
                    var captureClient = (IAudioCaptureClient)oCap;

                    Guid iidRen = typeof(IAudioRenderClient).GUID;
                    object oRen;
                    renAudioClient.GetService(ref iidRen, out oRen);
                    var renderClient = (IAudioRenderClient)oRen;

                    capAudioClient.Start();
                    renAudioClient.Start();

                    if (OnStatusChanged != null) OnStatusChanged("Çift Ses Aktif · Canlı Dinleniyor");

                    int bytesPerFrame = capFmt.nBlockAlign;
                    byte[] tempBuffer = new byte[65536];

                    while (_isRunning) {
                        uint packetSize;
                        int hrNext = captureClient.GetNextPacketSize(out packetSize);
                        if (hrNext != 0) break;

                        while (packetSize > 0 && _isRunning) {
                            IntPtr pData;
                            uint numFrames;
                            uint flags;
                            ulong devPos, qpcPos;
                            int hrGetBuf = captureClient.GetBuffer(out pData, out numFrames, out flags, out devPos, out qpcPos);
                            if (hrGetBuf == 0 && numFrames > 0) {
                                uint padding;
                                renAudioClient.GetCurrentPadding(out padding);
                                uint available = renBufferSize > padding ? renBufferSize - padding : 0;
                                uint toWrite = Math.Min(numFrames, available);

                                if (toWrite > 0) {
                                    IntPtr pRenData;
                                    int hrRenBuf = renderClient.GetBuffer((int)toWrite, out pRenData);
                                    if (hrRenBuf == 0) {
                                        int dataBytes = (int)toWrite * bytesPerFrame;
                                        if (tempBuffer.Length < dataBytes) tempBuffer = new byte[dataBytes * 2];

                                        if ((flags & 1) != 0) {
                                            Array.Clear(tempBuffer, 0, dataBytes);
                                        } else {
                                            Marshal.Copy(pData, tempBuffer, 0, dataBytes);
                                        }
                                        Marshal.Copy(tempBuffer, 0, pRenData, dataBytes);
                                        renderClient.ReleaseBuffer((int)toWrite, 0);
                                    }
                                }
                            }
                            captureClient.ReleaseBuffer(numFrames);
                            captureClient.GetNextPacketSize(out packetSize);
                        }

                        Thread.Sleep(5);
                    }
                } catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine("Audio bridge exception: " + ex);
                    Thread.Sleep(500);
                } finally {
                    if (capAudioClient != null) {
                        try { capAudioClient.Stop(); } catch { }
                    }
                    if (renAudioClient != null) {
                        try { renAudioClient.Stop(); } catch { }
                    }
                }
            }
        }
    }

    // ==========================================
    // MAIN APPLICATION & DUOLINGO STYLE WPF UI
    // ==========================================

    public class App : Application {
        [STAThread]
        public static void Main() {
            AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                string msg = (e.ExceptionObject != null) ? e.ExceptionObject.ToString() : "Bilinmeyen hata";
                MessageBox.Show("Uygulama Hatası: " + msg, "DualVoice", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            try {
                var app = new App();
                app.DispatcherUnhandledException += (s, e) => {
                    MessageBox.Show("Arayüz Hatası: " + e.Exception.Message, "DualVoice", MessageBoxButton.OK, MessageBoxImage.Error);
                    e.Handled = true;
                };
                app.Run(new MainWindow());
            } catch (Exception ex) {
                MessageBox.Show("Uygulama başlatılırken hata oluştu: " + ex.Message, "DualVoice", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class MainWindow : Window {
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        private const int GWL_STYLE = -16;
        private const int WS_MINIMIZEBOX = 0x00020000;

        private AudioEngine _audioEngine;
        private AudioDeviceInfo _selectedDev1;
        private AudioDeviceInfo _selectedDev2;

        private Border _slot1Card;
        private Border _slot2Card;
        private Grid _slot1EmptyContent;
        private Grid _slot2EmptyContent;
        private Grid _slot1FilledContent;
        private Grid _slot2FilledContent;

        private TextBlock _dev1NameText;
        private TextBlock _dev2NameText;
        private Slider _slider1;
        private Slider _slider2;
        private TextBlock _slider1Badge;
        private TextBlock _slider2Badge;
        private CheckBox _syncCheckBox;
        private TextBlock _statusBadgeText;
        private Ellipse _statusBadgeDot;
        private Border _statusPill;

        private Grid _devicePickerModal;
        private StackPanel _deviceListContainer;
        private TextBlock _pickerModalTitle;
        private int _pickingForSlot = 1;

        private DispatcherTimer _volumePollTimer;
        private float _lastKnownVol1 = -1f;
        private float _lastKnownVol2 = -1f;
        private bool _isUpdatingUiVolume = false;

        public MainWindow() {
            Title = "DualVoice — Çift Kulaklık Ses Paylaştırıcı";
            Width = 860;
            Height = 620;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = Brushes.Transparent;
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F2937"));
            ResizeMode = ResizeMode.CanMinimize;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            ShowInTaskbar = true;

            var logoBmp = LoadEmbeddedLogo();
            if (logoBmp != null) {
                Icon = logoBmp;
            }

            SourceInitialized += (s, e) => {
                try {
                    var helper = new WindowInteropHelper(this);
                    int style = GetWindowLong(helper.Handle, GWL_STYLE);
                    SetWindowLong(helper.Handle, GWL_STYLE, style | WS_MINIMIZEBOX);
                } catch { }
            };

            _audioEngine = new AudioEngine();
            _audioEngine.SyncVolumes = true;

            _audioEngine.OnStatusChanged += (msg) => {
                Dispatcher.BeginInvoke((Action)(() => {
                    UpdateStatusUi(msg);
                }));
            };

            BuildUserInterface();
            StartVolumePoller();
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (_volumePollTimer != null) {
                _volumePollTimer.Stop();
            }
            if (_audioEngine != null) {
                _audioEngine.StopBridge();
            }
        }

        private BitmapImage LoadEmbeddedLogo() {
            try {
                var asm = Assembly.GetExecutingAssembly();
                using (var s = asm.GetManifestResourceStream("logo.jpg")) {
                    if (s != null) {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.StreamSource = s;
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        bmp.Freeze();
                        return bmp;
                    }
                }
            } catch { }

            try {
                string localPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.jpg");
                if (System.IO.File.Exists(localPath)) {
                    var bmp = new BitmapImage(new Uri(localPath));
                    bmp.Freeze();
                    return bmp;
                }
            } catch { }

            return null;
        }

        private void StartVolumePoller() {
            _volumePollTimer = new DispatcherTimer {
                Interval = TimeSpan.FromMilliseconds(40)
            };
            _volumePollTimer.Tick += (s, e) => {
                if (_isUpdatingUiVolume) return;
                if (_slider1 != null && _slider1.IsMouseCaptureWithin) return;
                if (_slider2 != null && _slider2.IsMouseCaptureWithin) return;

                if (_selectedDev1 != null) {
                    float v1 = _audioEngine.GetVolume1();
                    if (_lastKnownVol1 < 0 || Math.Abs(v1 - _lastKnownVol1) > 0.004f) {
                        _lastKnownVol1 = v1;
                        int pct1 = (int)Math.Round(v1 * 100);
                        _isUpdatingUiVolume = true;
                        if (_slider1 != null) _slider1.Value = pct1;
                        if (_slider1Badge != null) _slider1Badge.Text = "%" + pct1;

                        if (_audioEngine.SyncVolumes && _selectedDev2 != null) {
                            _audioEngine.SetVolume2(v1);
                            _lastKnownVol2 = v1;
                            if (_slider2 != null) _slider2.Value = pct1;
                            if (_slider2Badge != null) _slider2Badge.Text = "%" + pct1;
                        }
                        _isUpdatingUiVolume = false;
                    }
                }

                if (!_audioEngine.SyncVolumes && _selectedDev2 != null) {
                    float v2 = _audioEngine.GetVolume2();
                    if (_lastKnownVol2 < 0 || Math.Abs(v2 - _lastKnownVol2) > 0.004f) {
                        _lastKnownVol2 = v2;
                        int pct2 = (int)Math.Round(v2 * 100);
                        _isUpdatingUiVolume = true;
                        if (_slider2 != null) _slider2.Value = pct2;
                        if (_slider2Badge != null) _slider2Badge.Text = "%" + pct2;
                        _isUpdatingUiVolume = false;
                    }
                }
            };
            _volumePollTimer.Start();
        }

        private void BuildUserInterface() {
            var rootGrid = new Grid();

            // Main Duolingo Style Window Container
            // Notice: Prominent 2.5px solid border with bottom 3D shadow so boundaries are unmistakable!
            var mainContainer = new Border {
                CornerRadius = new CornerRadius(22),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F7F9FA")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CFD7DE")),
                BorderThickness = new Thickness(2.5),
                Margin = new Thickness(10)
            };
            mainContainer.Effect = new DropShadowEffect {
                Color = Color.FromRgb(0, 0, 0),
                Direction = 270,
                ShadowDepth = 4,
                Opacity = 0.18,
                BlurRadius = 14
            };

            var layoutGrid = new Grid();
            layoutGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(64) }); // Titlebar
            layoutGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(88) }); // Header with Mascot Logo
            layoutGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Slots
            layoutGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(125) }); // Volume Section
            layoutGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(38) }); // Footer

            // 0: TOP BAR (DUOLINGO HEADER WITH LOGO & CONTROLS)
            var titleBar = new Border {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(20, 20, 0, 0),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB")),
                BorderThickness = new Thickness(0, 0, 0, 2)
            };
            var titleGrid = new Grid();
            titleBar.MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };

            // Left: Logo & Branding
            var brandStack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(18, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };

            var logoBmp = LoadEmbeddedLogo();
            if (logoBmp != null) {
                var logoImg = new Image {
                    Source = logoBmp,
                    Width = 38,
                    Height = 38,
                    Margin = new Thickness(0, 0, 10, 0)
                };
                logoImg.Clip = new RectangleGeometry(new Rect(0, 0, 38, 38), 10, 10);
                brandStack.Children.Add(logoImg);
            } else {
                var logoFallback = new Border {
                    Width = 36,
                    Height = 36,
                    CornerRadius = new CornerRadius(10),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#58CC02")),
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#46A302")),
                    BorderThickness = new Thickness(0, 0, 0, 3),
                    Margin = new Thickness(0, 0, 10, 0)
                };
                var hpIcon = new TextBlock { Text = "🎧", FontSize = 18, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
                logoFallback.Child = hpIcon;
                brandStack.Children.Add(logoFallback);
            }

            var appTitleText = new TextBlock {
                Text = "DualVoice",
                FontWeight = FontWeights.ExtraBold,
                FontSize = 18,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F2937")),
                VerticalAlignment = VerticalAlignment.Center
            };
            brandStack.Children.Add(appTitleText);

            var duolingoPill = new Border {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F8D8")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BAEDB0")),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            var duolingoPillTxt = new TextBlock {
                Text = "ÇİFT KULAKLIK",
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#46A302"))
            };
            duolingoPill.Child = duolingoPillTxt;
            brandStack.Children.Add(duolingoPill);

            titleGrid.Children.Add(brandStack);

            // Right: Chunky 3D Window Action Buttons
            var winBtns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 16, 0), VerticalAlignment = VerticalAlignment.Center };
            var btnMin = CreateDuoHeaderButton("—", () => { WindowState = WindowState.Minimized; });
            var btnClose = CreateDuoHeaderButton("✕", () => { Close(); }, isClose: true);
            winBtns.Children.Add(btnMin);
            winBtns.Children.Add(btnClose);
            titleGrid.Children.Add(winBtns);

            titleBar.Child = titleGrid;
            Grid.SetRow(titleBar, 0);
            layoutGrid.Children.Add(titleBar);

            // 1: HEADER BANNER (Vibrant Duolingo Welcome & Live Status)
            var headerSection = new Grid { Margin = new Thickness(28, 12, 28, 6) };
            var headerLeft = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var h1 = new TextBlock {
                Text = "İki Kulaklık, Tek Müzik!",
                FontSize = 23,
                FontWeight = FontWeights.ExtraBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111827"))
            };
            var h1Sub = new TextBlock {
                Text = "Bağlı kulaklıkları seçin; ikisine de aynı anda net ve gecikmesiz ses aktarın.",
                FontSize = 13.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B5563")),
                Margin = new Thickness(0, 3, 0, 0)
            };
            headerLeft.Children.Add(h1);
            headerLeft.Children.Add(h1Sub);
            headerSection.Children.Add(headerLeft);

            // Live status badge (Duolingo style pill)
            _statusPill = new Border {
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D5DB")),
                BorderThickness = new Thickness(2),
                Padding = new Thickness(14, 8, 16, 8),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            var statusStack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _statusBadgeDot = new Ellipse {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF")),
                Margin = new Thickness(0, 0, 8, 0)
            };
            _statusBadgeText = new TextBlock {
                Text = "Beklemede · Kulaklıkları seçin",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#374151")),
                VerticalAlignment = VerticalAlignment.Center
            };
            statusStack.Children.Add(_statusBadgeDot);
            statusStack.Children.Add(_statusBadgeText);
            _statusPill.Child = statusStack;
            headerSection.Children.Add(_statusPill);

            Grid.SetRow(headerSection, 1);
            layoutGrid.Children.Add(headerSection);

            // 2: TWO HEADPHONE SLOTS (DUOLINGO CARD STYLE)
            var slotsGrid = new Grid { Margin = new Thickness(28, 8, 28, 12) };
            slotsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            slotsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
            slotsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // SLOT 1 (Macaw Sky Blue Theme: #1CB0F6)
            _slot1Card = CreateDuolingoSlot(1, "#1CB0F6", "#1899D6", out _slot1EmptyContent, out _slot1FilledContent, out _dev1NameText);
            Grid.SetColumn(_slot1Card, 0);
            slotsGrid.Children.Add(_slot1Card);

            // SLOT 2 (Feather Green Theme: #58CC02)
            _slot2Card = CreateDuolingoSlot(2, "#58CC02", "#46A302", out _slot2EmptyContent, out _slot2FilledContent, out _dev2NameText);
            Grid.SetColumn(_slot2Card, 2);
            slotsGrid.Children.Add(_slot2Card);

            Grid.SetRow(slotsGrid, 2);
            layoutGrid.Children.Add(slotsGrid);

            // 3: VOLUME CONTROLS (Chunky Sliders & Center Sync Box)
            var volumeSection = new Grid { Margin = new Thickness(28, 0, 28, 10), VerticalAlignment = VerticalAlignment.Center };
            volumeSection.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            volumeSection.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) }); // Center sync
            volumeSection.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Device 1 Volume Card
            var vol1Card = CreateDuolingoVolumeCard(1, "#1CB0F6", out _slider1, out _slider1Badge);
            Grid.SetColumn(vol1Card, 0);
            volumeSection.Children.Add(vol1Card);

            // Center: Duolingo Style 3D Sync Checkbox Card
            var syncCard = CreateDuolingoSyncCard();
            Grid.SetColumn(syncCard, 1);
            volumeSection.Children.Add(syncCard);

            // Device 2 Volume Card
            var vol2Card = CreateDuolingoVolumeCard(2, "#58CC02", out _slider2, out _slider2Badge);
            Grid.SetColumn(vol2Card, 2);
            volumeSection.Children.Add(vol2Card);

            Grid.SetRow(volumeSection, 3);
            layoutGrid.Children.Add(volumeSection);

            // 4: FOOTER (Clean & Simple)
            var footerBar = new Grid { Margin = new Thickness(28, 0, 28, 10) };
            var footerLeft = new TextBlock {
                Text = "DualVoice · Çift Ses Aktarım Motoru",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF")),
                VerticalAlignment = VerticalAlignment.Center
            };
            var footerRight = new TextBlock {
                Text = "WASAPI Low-Latency Engine",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF")),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            footerBar.Children.Add(footerLeft);
            footerBar.Children.Add(footerRight);
            Grid.SetRow(footerBar, 4);
            layoutGrid.Children.Add(footerBar);

            mainContainer.Child = layoutGrid;
            rootGrid.Children.Add(mainContainer);

            // POPUP DEVICE PICKER MODAL
            _devicePickerModal = CreateDuolingoPickerModal();
            _devicePickerModal.Visibility = Visibility.Collapsed;
            rootGrid.Children.Add(_devicePickerModal);

            Content = rootGrid;
        }

        private Border CreateDuolingoSlot(int slotNumber, string colorHex, string bottomBevelHex, out Grid emptyContent, out Grid filledContent, out TextBlock devNameText) {
            // Duolingo 3D Card: 2.5px solid border, 4px bottom border
            var card = new Border {
                CornerRadius = new CornerRadius(20),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB")),
                BorderThickness = new Thickness(2.5, 2.5, 2.5, 5),
                Cursor = Cursors.Hand
            };

            var mainGrid = new Grid();

            // EMPTY STATE CONTENT: Chunky 3D Plus Button and clear instructions
            emptyContent = new Grid {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var emptyStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };

            // 3D Pushable "+" Circular Button
            var plusBtn = new Border {
                Width = 72,
                Height = 72,
                CornerRadius = new CornerRadius(36),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bottomBevelHex)),
                BorderThickness = new Thickness(0, 0, 0, 5),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 12)
            };
            var plusIcon = new TextBlock {
                Text = "+",
                FontSize = 38,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, -2, 0, 0)
            };
            plusBtn.Child = plusIcon;

            var slotLabel = new TextBlock {
                Text = slotNumber == 1 ? "1. KULAKLIK (BİRİNCİL)" : "2. KULAKLIK (İKİNCİL)",
                FontSize = 12,
                FontWeight = FontWeights.ExtraBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            };
            var emptyTitle = new TextBlock {
                Text = "Kulaklık Seç",
                FontSize = 18,
                FontWeight = FontWeights.ExtraBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111827")),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var emptySub = new TextBlock {
                Text = "Listeden bağlamak için tıkla",
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 3, 0, 0)
            };

            emptyStack.Children.Add(plusBtn);
            emptyStack.Children.Add(slotLabel);
            emptyStack.Children.Add(emptyTitle);
            emptyStack.Children.Add(emptySub);
            emptyContent.Children.Add(emptyStack);
            mainGrid.Children.Add(emptyContent);

            // FILLED STATE CONTENT: Mascot Headphone Visual, Device Name, Active Badge
            filledContent = new Grid {
                Visibility = Visibility.Collapsed,
                Margin = new Thickness(16)
            };
            var filledStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };

            // Duolingo vector headphone icon
            var hpVisual = CreateDuolingoHeadphoneVisual(colorHex, bottomBevelHex);
            filledStack.Children.Add(hpVisual);

            var filledBadge = new Border {
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
                Padding = new Thickness(10, 4, 10, 4),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 6)
            };
            var filledBadgeTxt = new TextBlock {
                Text = slotNumber == 1 ? "1. KULAKLIK" : "2. KULAKLIK",
                FontSize = 11,
                FontWeight = FontWeights.ExtraBold,
                Foreground = Brushes.White
            };
            filledBadge.Child = filledBadgeTxt;
            filledStack.Children.Add(filledBadge);

            devNameText = new TextBlock {
                Text = "Kulaklık Adı",
                FontSize = 16,
                FontWeight = FontWeights.ExtraBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111827")),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 270
            };
            filledStack.Children.Add(devNameText);

            var activePill = new StackPanel {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 10)
            };
            var dot = new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#58CC02")), Margin = new Thickness(0, 0, 6, 0) };
            var activeTxt = new TextBlock {
                Text = "Bağlandı ve Hazır",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A"))
            };
            activePill.Children.Add(dot);
            activePill.Children.Add(activeTxt);
            filledStack.Children.Add(activePill);

            // Tactile 3D "Değiştir" button
            var changeBtn = CreateDuolingoButton("DEĞİŞTİR", "#F3F4F6", "#D1D5DB", "#374151", () => {
                OpenDevicePicker(slotNumber);
            });
            changeBtn.HorizontalAlignment = HorizontalAlignment.Center;
            filledStack.Children.Add(changeBtn);

            filledContent.Children.Add(filledStack);
            mainGrid.Children.Add(filledContent);

            card.Child = mainGrid;

            card.MouseLeftButtonDown += (s, e) => {
                OpenDevicePicker(slotNumber);
            };

            // Tactile hover effect on card
            card.MouseEnter += (s, e) => {
                card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
            };
            card.MouseLeave += (s, e) => {
                card.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB"));
            };

            return card;
        }

        private FrameworkElement CreateDuolingoHeadphoneVisual(string colorHex, string bottomHex) {
            var canvas = new Canvas {
                Width = 84,
                Height = 72,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            };

            // Headband arc with bold outline
            var headband = new System.Windows.Shapes.Path {
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
                StrokeThickness = 5,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Data = Geometry.Parse("M 18,38 C 18,12 66,12 66,38")
            };
            canvas.Children.Add(headband);

            // Left Earcup with 3D bottom bevel
            var leftCup = new Border {
                Width = 18,
                Height = 32,
                CornerRadius = new CornerRadius(9),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bottomHex)),
                BorderThickness = new Thickness(0, 0, 0, 4)
            };
            Canvas.SetLeft(leftCup, 9);
            Canvas.SetTop(leftCup, 26);
            canvas.Children.Add(leftCup);

            // Right Earcup with 3D bottom bevel
            var rightCup = new Border {
                Width = 18,
                Height = 32,
                CornerRadius = new CornerRadius(9),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bottomHex)),
                BorderThickness = new Thickness(0, 0, 0, 4)
            };
            Canvas.SetLeft(rightCup, 57);
            Canvas.SetTop(rightCup, 26);
            canvas.Children.Add(rightCup);

            // Sound waves
            var w1 = new Border { Width = 3.5, Height = 12, CornerRadius = new CornerRadius(2), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)) };
            Canvas.SetLeft(w1, 35);
            Canvas.SetTop(w1, 36);
            var w2 = new Border { Width = 3.5, Height = 20, CornerRadius = new CornerRadius(2), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)) };
            Canvas.SetLeft(w2, 42);
            Canvas.SetTop(w2, 32);
            var w3 = new Border { Width = 3.5, Height = 14, CornerRadius = new CornerRadius(2), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)) };
            Canvas.SetLeft(w3, 49);
            Canvas.SetTop(w3, 35);

            canvas.Children.Add(w1);
            canvas.Children.Add(w2);
            canvas.Children.Add(w3);

            return canvas;
        }

        private Border CreateDuolingoVolumeCard(int slotNumber, string colorHex, out Slider slider, out TextBlock valBadge) {
            var box = new Border {
                CornerRadius = new CornerRadius(18),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB")),
                BorderThickness = new Thickness(2, 2, 2, 4),
                Padding = new Thickness(16, 12, 16, 12)
            };

            var stack = new StackPanel();

            var topRow = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            var label = new TextBlock {
                Text = "Cihaz " + slotNumber + " Sesi",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F2937")),
                VerticalAlignment = VerticalAlignment.Center
            };

            // Badge Pill
            var pillBorder = new Border {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex)),
                Padding = new Thickness(8, 2, 8, 2),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            valBadge = new TextBlock {
                Text = "%50",
                FontSize = 12,
                FontWeight = FontWeights.ExtraBold,
                Foreground = Brushes.White
            };
            pillBorder.Child = valBadge;

            topRow.Children.Add(label);
            topRow.Children.Add(pillBorder);
            stack.Children.Add(topRow);

            slider = new Slider {
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                Height = 28,
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            slider.Style = CreateDuolingoSliderStyle(colorHex);

            var capturedBadge = valBadge;
            int capturedSlot = slotNumber;

            slider.ValueChanged += (s, e) => {
                if (_isUpdatingUiVolume) return;
                int intVal = (int)Math.Round(e.NewValue);
                capturedBadge.Text = "%" + intVal;
                float volScalar = (float)(intVal / 100.0);

                if (capturedSlot == 1) {
                    _lastKnownVol1 = volScalar;
                    _audioEngine.SetVolume1(volScalar);
                    if (_audioEngine.SyncVolumes && _selectedDev2 != null) {
                        SyncDevice2ToVolume1(volScalar);
                    }
                } else if (capturedSlot == 2) {
                    _lastKnownVol2 = volScalar;
                    _audioEngine.SetVolume2(volScalar);
                    if (_audioEngine.SyncVolumes && _selectedDev1 != null) {
                        _lastKnownVol1 = volScalar;
                        _isUpdatingUiVolume = true;
                        _audioEngine.SetVolume1(volScalar);
                        if (_slider1 != null) _slider1.Value = intVal;
                        if (_slider1Badge != null) _slider1Badge.Text = "%" + intVal;
                        _isUpdatingUiVolume = false;
                    }
                }
            };

            stack.Children.Add(slider);
            box.Child = stack;
            return box;
        }

        private Border CreateDuolingoSyncCard() {
            var card = new Border {
                CornerRadius = new CornerRadius(18),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB")),
                BorderThickness = new Thickness(2, 2, 2, 4),
                Padding = new Thickness(12, 10, 12, 10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };

            _syncCheckBox = new CheckBox {
                IsChecked = true,
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _syncCheckBox.Style = CreateDuolingoCheckBoxStyle();
            _syncCheckBox.Checked += (s, e) => {
                _audioEngine.SyncVolumes = true;
                if (_slider1 != null) SyncDevice2ToVolume1((float)(_slider1.Value / 100.0));
            };
            _syncCheckBox.Unchecked += (s, e) => {
                _audioEngine.SyncVolumes = false;
            };

            var syncLabel = new TextBlock {
                Text = "Sesleri Eşitle",
                FontWeight = FontWeights.ExtraBold,
                FontSize = 13.5,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111827")),
                Margin = new Thickness(0, 6, 0, 1),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var syncHint = new TextBlock {
                Text = "Tek ses ikisini yönetir",
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280")),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            stack.Children.Add(_syncCheckBox);
            stack.Children.Add(syncLabel);
            stack.Children.Add(syncHint);
            card.Child = stack;

            return card;
        }

        private void SyncDevice2ToVolume1(float volScalar) {
            _isUpdatingUiVolume = true;
            _audioEngine.SetVolume2(volScalar);
            _lastKnownVol2 = volScalar;
            int intVal = (int)Math.Round(volScalar * 100);
            if (_slider2 != null) _slider2.Value = intVal;
            if (_slider2Badge != null) _slider2Badge.Text = "%" + intVal;
            _isUpdatingUiVolume = false;
        }

        private void OpenDevicePicker(int slot) {
            _pickingForSlot = slot;
            _pickerModalTitle.Text = "Cihaz " + slot + " İçin Kulaklık Seç";
            _deviceListContainer.Children.Clear();

            var devices = AudioEngine.GetActiveDevices();
            if (devices.Count == 0) {
                var emptyNotice = new TextBlock {
                    Text = "Aktif ses aygıtı bulunamadı. Lütfen kulaklığınızın açık ve bilgisayara bağlı olduğundan emin olun.",
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EF4444")),
                    Margin = new Thickness(0, 20, 0, 20),
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                _deviceListContainer.Children.Add(emptyNotice);
            } else {
                foreach (var dev in devices) {
                    var btn = CreateDevicePickerRow(dev);
                    _deviceListContainer.Children.Add(btn);
                }
            }

            _devicePickerModal.Visibility = Visibility.Visible;
        }

        private FrameworkElement CreateDevicePickerRow(AudioDeviceInfo dev) {
            var itemBorder = new Border {
                CornerRadius = new CornerRadius(14),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB")),
                BorderThickness = new Thickness(2, 2, 2, 4),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 10),
                Cursor = Cursors.Hand
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var iconCircle = new Border {
                Width = 32,
                Height = 32,
                CornerRadius = new CornerRadius(16),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8F8D8")),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            var iconTxt = new TextBlock { Text = "🎧", FontSize = 16, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            iconCircle.Child = iconTxt;
            Grid.SetColumn(iconCircle, 0);
            grid.Children.Add(iconCircle);

            var nameStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var nameTxt = new TextBlock {
                Text = dev.Name,
                FontSize = 14,
                FontWeight = FontWeights.ExtraBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111827"))
            };
            nameStack.Children.Add(nameTxt);

            if (dev.IsDefault) {
                var defTag = new TextBlock {
                    Text = "★ Windows Varsayılan Çıkışı",
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1CB0F6")),
                    Margin = new Thickness(0, 2, 0, 0)
                };
                nameStack.Children.Add(defTag);
            }
            Grid.SetColumn(nameStack, 1);
            grid.Children.Add(nameStack);

            // Tactile 3D "SEÇ" button
            var selectPill = new Border {
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#58CC02")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#46A302")),
                BorderThickness = new Thickness(0, 0, 0, 3),
                Padding = new Thickness(14, 6, 14, 6),
                VerticalAlignment = VerticalAlignment.Center
            };
            var selectTxt = new TextBlock {
                Text = "SEÇ",
                FontSize = 12,
                FontWeight = FontWeights.ExtraBold,
                Foreground = Brushes.White
            };
            selectPill.Child = selectTxt;
            Grid.SetColumn(selectPill, 2);
            grid.Children.Add(selectPill);

            itemBorder.Child = grid;

            itemBorder.MouseEnter += (s, e) => {
                itemBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#58CC02"));
                itemBorder.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0FDF4"));
            };
            itemBorder.MouseLeave += (s, e) => {
                itemBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB"));
                itemBorder.Background = Brushes.White;
            };

            itemBorder.MouseLeftButtonDown += (s, e) => {
                SelectDeviceForSlot(_pickingForSlot, dev);
                _devicePickerModal.Visibility = Visibility.Collapsed;
            };

            return itemBorder;
        }

        private void SelectDeviceForSlot(int slot, AudioDeviceInfo dev) {
            if (slot == 1) {
                _selectedDev1 = dev;
                _dev1NameText.Text = dev.Name;
                _slot1EmptyContent.Visibility = Visibility.Collapsed;
                _slot1FilledContent.Visibility = Visibility.Visible;
                _audioEngine.SetDevice1(dev.Id);

                float vol = _audioEngine.GetVolume1();
                _lastKnownVol1 = vol;
                int pct = (int)Math.Round(vol * 100);
                _isUpdatingUiVolume = true;
                if (_slider1 != null) _slider1.Value = pct;
                if (_slider1Badge != null) _slider1Badge.Text = "%" + pct;
                _isUpdatingUiVolume = false;

                if (_audioEngine.SyncVolumes && _selectedDev2 != null) {
                    SyncDevice2ToVolume1(vol);
                }
            } else if (slot == 2) {
                _selectedDev2 = dev;
                _dev2NameText.Text = dev.Name;
                _slot2EmptyContent.Visibility = Visibility.Collapsed;
                _slot2FilledContent.Visibility = Visibility.Visible;
                _audioEngine.SetDevice2(dev.Id);

                if (_audioEngine.SyncVolumes && _selectedDev1 != null) {
                    float vol1 = _audioEngine.GetVolume1();
                    SyncDevice2ToVolume1(vol1);
                } else {
                    float vol = _audioEngine.GetVolume2();
                    _lastKnownVol2 = vol;
                    int pct = (int)Math.Round(vol * 100);
                    _isUpdatingUiVolume = true;
                    if (_slider2 != null) _slider2.Value = pct;
                    if (_slider2Badge != null) _slider2Badge.Text = "%" + pct;
                    _isUpdatingUiVolume = false;
                }
            }
        }

        private Grid CreateDuolingoPickerModal() {
            var overlay = new Grid {
                Background = new SolidColorBrush(Color.FromArgb(160, 0, 0, 0))
            };

            var modalBorder = new Border {
                Width = 540,
                MaxHeight = 460,
                CornerRadius = new CornerRadius(22),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F7F9FA")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D0D7DE")),
                BorderThickness = new Thickness(2.5, 2.5, 2.5, 5),
                Padding = new Thickness(24),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            modalBorder.Effect = new DropShadowEffect { Color = Colors.Black, Direction = 270, ShadowDepth = 6, Opacity = 0.25, BlurRadius = 16 };

            var contentStack = new Grid();
            contentStack.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            contentStack.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            contentStack.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Modal Header
            var headerGrid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
            _pickerModalTitle = new TextBlock {
                Text = "Kulaklık Seç",
                FontSize = 18,
                FontWeight = FontWeights.ExtraBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#111827")),
                VerticalAlignment = VerticalAlignment.Center
            };
            var closeBtn = CreateDuoHeaderButton("✕", () => { overlay.Visibility = Visibility.Collapsed; }, isClose: true);
            closeBtn.HorizontalAlignment = HorizontalAlignment.Right;

            headerGrid.Children.Add(_pickerModalTitle);
            headerGrid.Children.Add(closeBtn);
            Grid.SetRow(headerGrid, 0);
            contentStack.Children.Add(headerGrid);

            var scroll = new ScrollViewer {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 16)
            };
            _deviceListContainer = new StackPanel();
            scroll.Content = _deviceListContainer;
            Grid.SetRow(scroll, 1);
            contentStack.Children.Add(scroll);

            var footerStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var refreshBtn = CreateDuolingoButton("🔄 YENİLE", "#1CB0F6", "#1899D6", "#FFFFFF", () => {
                OpenDevicePicker(_pickingForSlot);
            });
            refreshBtn.Margin = new Thickness(0, 0, 10, 0);

            var cancelBtn = CreateDuolingoButton("VAZGEÇ", "#E5E7EB", "#D1D5DB", "#374151", () => {
                overlay.Visibility = Visibility.Collapsed;
            });

            footerStack.Children.Add(refreshBtn);
            footerStack.Children.Add(cancelBtn);
            Grid.SetRow(footerStack, 2);
            contentStack.Children.Add(footerStack);

            modalBorder.Child = contentStack;
            overlay.Children.Add(modalBorder);
            return overlay;
        }

        private void UpdateStatusUi(string msg) {
            if (_statusBadgeText != null) _statusBadgeText.Text = msg;
            if (_statusBadgeDot != null && _statusPill != null) {
                if (msg.Contains("Aktif") || msg.Contains("Dinleniyor")) {
                    _statusBadgeDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#58CC02"));
                    _statusPill.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DCFCE7"));
                    _statusPill.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC"));
                    _statusBadgeText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15803D"));
                } else if (msg.Contains("1 Kulaklık")) {
                    _statusBadgeDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                    _statusPill.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"));
                    _statusPill.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDE68A"));
                    _statusBadgeText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309"));
                } else {
                    _statusBadgeDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CA3AF"));
                    _statusPill.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB"));
                    _statusPill.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D1D5DB"));
                    _statusBadgeText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#374151"));
                }
            }
        }

        private Border CreateDuoHeaderButton(string text, Action onClick, bool isClose = false) {
            var btn = new Border {
                Width = 36,
                Height = 32,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isClose ? "#FEE2E2" : "#F3F4F6")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isClose ? "#FCA5A5" : "#E5E7EB")),
                BorderThickness = new Thickness(0, 0, 0, 2),
                Cursor = Cursors.Hand,
                Margin = new Thickness(4, 0, 0, 0)
            };
            var txt = new TextBlock {
                Text = text,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isClose ? "#DC2626" : "#4B5563")),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            btn.Child = txt;

            btn.MouseLeftButtonDown += (s, e) => {
                e.Handled = true;
                btn.BorderThickness = new Thickness(0, 0, 0, 0);
                btn.Margin = new Thickness(4, 2, 0, 0);
            };
            btn.MouseLeftButtonUp += (s, e) => {
                btn.BorderThickness = new Thickness(0, 0, 0, 2);
                btn.Margin = new Thickness(4, 0, 0, 0);
                onClick();
            };
            return btn;
        }

        private Border CreateDuolingoButton(string text, string bgHex, string bevelHex, string textHex, Action onClick) {
            var btn = new Border {
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgHex)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bevelHex)),
                BorderThickness = new Thickness(0, 0, 0, 3.5),
                Padding = new Thickness(16, 7, 16, 7),
                Cursor = Cursors.Hand
            };
            var txt = new TextBlock {
                Text = text,
                FontSize = 12,
                FontWeight = FontWeights.ExtraBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(textHex)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            btn.Child = txt;

            btn.MouseLeftButtonDown += (s, e) => {
                e.Handled = true;
                btn.BorderThickness = new Thickness(0, 0, 0, 0.5);
                btn.RenderTransform = new TranslateTransform(0, 3);
            };
            btn.MouseLeftButtonUp += (s, e) => {
                btn.BorderThickness = new Thickness(0, 0, 0, 3.5);
                btn.RenderTransform = new TranslateTransform(0, 0);
                onClick();
            };
            return btn;
        }

        private Style CreateDuolingoSliderStyle(string colorHex) {
            string xaml = string.Format(@"
            <Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' 
                   xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' 
                   TargetType='Slider'>
                <Setter Property='Focusable' Value='False'/>
                <Setter Property='Template'>
                    <Setter.Value>
                        <ControlTemplate TargetType='Slider'>
                            <Grid VerticalAlignment='Center'>
                                <Track x:Name='PART_Track'>
                                    <Track.DecreaseRepeatButton>
                                        <RepeatButton Command='Slider.DecreaseLarge'>
                                            <RepeatButton.Template>
                                                <ControlTemplate>
                                                    <Border Height='10' CornerRadius='5,0,0,5' Background='{0}'/>
                                                </ControlTemplate>
                                            </RepeatButton.Template>
                                        </RepeatButton>
                                    </Track.DecreaseRepeatButton>
                                    <Track.IncreaseRepeatButton>
                                        <RepeatButton Command='Slider.IncreaseLarge'>
                                            <RepeatButton.Template>
                                                <ControlTemplate>
                                                    <Border Height='10' CornerRadius='0,5,5,0' Background='#E5E7EB'/>
                                                </ControlTemplate>
                                            </RepeatButton.Template>
                                        </RepeatButton>
                                    </Track.IncreaseRepeatButton>
                                    <Track.Thumb>
                                        <Thumb>
                                            <Thumb.Template>
                                                <ControlTemplate>
                                                    <Grid Width='24' Height='24' Cursor='Hand'>
                                                        <Ellipse Fill='White' Stroke='{0}' StrokeThickness='3.5'/>
                                                        <Ellipse Width='8' Height='8' Fill='{0}'/>
                                                    </Grid>
                                                </ControlTemplate>
                                            </Thumb.Template>
                                        </Thumb>
                                    </Track.Thumb>
                                </Track>
                            </Grid>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
            </Style>", colorHex);
            return (Style)XamlReader.Parse(xaml);
        }

        private Style CreateDuolingoCheckBoxStyle() {
            string xaml = @"
            <Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' 
                   xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' 
                   TargetType='CheckBox'>
                <Setter Property='Focusable' Value='False'/>
                <Setter Property='Template'>
                    <Setter.Value>
                        <ControlTemplate TargetType='CheckBox'>
                            <Border x:Name='boxBorder' Width='28' Height='28' CornerRadius='8' 
                                    Background='#F3F4F6' BorderBrush='#D1D5DB' BorderThickness='0,0,0,3.5'>
                                <Path x:Name='checkMark' Data='M 6,14 L 11,19 L 22,8' 
                                      Stroke='White' StrokeThickness='3' StrokeStartLineCap='Round' StrokeEndLineCap='Round'
                                      Visibility='Collapsed' VerticalAlignment='Center' HorizontalAlignment='Center'/>
                            </Border>
                            <ControlTemplate.Triggers>
                                <Trigger Property='IsChecked' Value='True'>
                                    <Setter TargetName='boxBorder' Property='Background' Value='#58CC02'/>
                                    <Setter TargetName='boxBorder' Property='BorderBrush' Value='#46A302'/>
                                    <Setter TargetName='checkMark' Property='Visibility' Value='Visible'/>
                                </Trigger>
                            </ControlTemplate.Triggers>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
            </Style>";
            return (Style)XamlReader.Parse(xaml);
        }
    }
}
