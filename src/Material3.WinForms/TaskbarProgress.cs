using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Material3.WinForms {
    /// <summary>
    /// Mirrors long-running progress to the Windows taskbar button via ITaskbarList3 (Win7+), no-op where unavailable.
    /// Producers that may overlap open a <see cref="Session"/> each: the oldest open session of a form drives its bar,
    /// the next one takes over when it ends, and the bar clears when the last one closes.
    /// </summary>
    public static class TaskbarProgress {
        /// <summary>The taskbar button's progress state.</summary>
        public enum State {
            /// <summary>No progress shown.</summary>
            None = 0,
            /// <summary>A marquee: work is running with no known fraction.</summary>
            Indeterminate = 0x1,
            /// <summary>A green bar at the current value.</summary>
            Normal = 0x2,
            /// <summary>A red bar at the current value.</summary>
            Error = 0x4,
            /// <summary>A yellow bar at the current value.</summary>
            Paused = 0x8,
        }

        private static readonly object Gate = new object();
        private static ITaskbarList3? _instance;
        private static bool _initFailed;

        static TaskbarProgress() {
            AppDomain.CurrentDomain.ProcessExit += (_, __) => ReleaseInstance();
        }

        private static void SetValue(IWin32Window owner, int percent) {
            if (!IsOwnerUsable(owner)) {
                return;
            }

            if (percent < 0) {
                percent = 0;
            }

            if (percent > 100) {
                percent = 100;
            }

            ITaskbarList3? tb = TryGetInstance();
            if (tb == null) {
                return;
            }

            try {
                tb.SetProgressValue(owner.Handle, (ulong)percent, 100UL);
            }
            catch (COMException) {
            }
            catch (ObjectDisposedException) { }
        }

        private static void SetState(IWin32Window owner, State state) {
            if (!IsOwnerUsable(owner)) {
                return;
            }

            ITaskbarList3? tb = TryGetInstance();
            if (tb == null) {
                return;
            }

            try {
                tb.SetProgressState(owner.Handle, (uint)state);
            }
            catch (COMException) { }
            catch (ObjectDisposedException) { }
        }

        private static readonly ConditionalWeakTable<Form, List<Session>> Queues = new ConditionalWeakTable<Form, List<Session>>();

        /// <summary>Opens a session on <paramref name="form"/>'s taskbar button, starting indeterminate. Dispose it when the work ends.</summary>
        public static Session Begin(Form form) {
            if (form == null) {
                throw new ArgumentNullException(nameof(form));
            }

            List<Session> queue = Queues.GetValue(form, f => {
                f.HandleCreated += (s, e) => Render(f);
                return new List<Session>();
            });
            var session = new Session(form);
            queue.Add(session);
            Render(form);
            return session;
        }

        private static void Render(Form form) {
            if (!Queues.TryGetValue(form, out List<Session>? queue)) {
                return;
            }

            Session? shown = queue.Count > 0 ? queue[0] : null;
            State state = shown?.State ?? State.None;
            SetState(form, state);
            if (shown != null && state != State.None && state != State.Indeterminate) {
                SetValue(form, (int)Math.Round(shown.Value * 100));
            }
        }

        /// <summary>One producer's claim on the bar. Only the oldest open session of the form is shown.</summary>
        public sealed class Session : IDisposable {
            private readonly Form _form;
            private bool _ended;

            internal Session(Form form) => _form = form;

            /// <summary>The state this session asks for.</summary>
            public State State { get; private set; } = State.Indeterminate;

            /// <summary>The fraction this session asks for, 0 to 1.</summary>
            public double Value { get; private set; }

            /// <summary>Whether this session is the one the bar shows.</summary>
            public bool IsShown => !_ended && Queues.TryGetValue(_form, out List<Session>? queue) && queue.Count > 0 && ReferenceEquals(queue[0], this);

            /// <summary>A green bar at <paramref name="percent"/>, clamped to 0–100.</summary>
            public void SetNormal(int percent) => Set(State.Normal, Math.Max(0, Math.Min(100, percent)) / 100.0);

            /// <summary>A marquee: work is running with no known fraction.</summary>
            public void SetIndeterminate() => Set(State.Indeterminate, Value);

            /// <summary>A yellow bar at the current value.</summary>
            public void SetPaused() => Set(State.Paused, Value);

            /// <summary>A red bar — full when nothing had been reported, so the failure is visible.</summary>
            public void SetError() => Set(State.Error, Value > 0 ? Value : 1.0);

            /// <summary>A full green bar that stays until the session is disposed, so a finish is seen from the taskbar.</summary>
            public void SetComplete() => Set(State.Normal, 1.0);

            private void Set(State state, double value) {
                if (_ended) {
                    return;
                }

                State = state;
                Value = value;
                if (IsShown) {
                    Render(_form);
                }
            }

            /// <summary>Gives the bar back: the next session takes it over, or it clears.</summary>
            public void Dispose() {
                if (_ended) {
                    return;
                }

                bool wasShown = IsShown;
                _ended = true;
                if (Queues.TryGetValue(_form, out List<Session>? queue)) {
                    queue.Remove(this);
                }

                if (wasShown) {
                    Render(_form);
                }
            }
        }

        private static bool IsOwnerUsable(IWin32Window owner) {
            return !(owner is Control control) || (!control.IsDisposed && control.IsHandleCreated);
        }

        private static ITaskbarList3? TryGetInstance() {
            if (_initFailed) {
                return null;
            }

            if (_instance != null) {
                return _instance;
            }

            lock (Gate) {
                if (_instance != null) {
                    return _instance;
                }

                if (_initFailed) {
                    return null;
                }

                try {
                    Type t = Type.GetTypeFromCLSID(new Guid("56FDF344-FD6D-11d0-958A-006097C9A090"))
                        ?? throw new InvalidOperationException("TaskbarList CLSID not registered.");
                    var tb = (ITaskbarList3)Activator.CreateInstance(t)!;
                    tb.HrInit();
                    _instance = tb;
                    return tb;
                }
                catch (Exception ex) {
                    Debug.WriteLine($"Material3.WinForms: ITaskbarList3 init failed, taskbar progress disabled. {ex}");
                    _initFailed = true;
                    return null;
                }
            }
        }

        private static void ReleaseInstance() {
            lock (Gate) {
                if (_instance == null) {
                    return;
                }

                try {
                    Marshal.ReleaseComObject(_instance);
                }
                catch (Exception ex) {
                    Debug.WriteLine($"Material3.WinForms: failed to release ITaskbarList3. {ex}");
                }
                _instance = null;
            }
        }

        [ComImport]
        [Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ITaskbarList3 {
            void HrInit();
            void AddTab(IntPtr hwnd);
            void DeleteTab(IntPtr hwnd);
            void ActivateTab(IntPtr hwnd);
            void SetActiveAlt(IntPtr hwnd);
            void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fullscreen);
            void SetProgressValue(IntPtr hwnd, ulong completed, ulong total);
            void SetProgressState(IntPtr hwnd, uint state);
        }
    }
}
