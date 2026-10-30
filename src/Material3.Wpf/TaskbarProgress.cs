using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Shell;
using Material3.Shared;

namespace Material3.Wpf {
    /// <summary>
    /// The window's taskbar progress, shared by everything that reports work. Each producer opens a
    /// <see cref="Session"/>; the oldest open session of a window drives its bar, the next one takes over when it
    /// ends, and the bar clears when the last one closes. Any thread may report; without a taskbar (no shell) it does nothing.
    /// </summary>
    public static class TaskbarProgress {
        private static readonly DependencyProperty QueueProperty = DependencyProperty.RegisterAttached(
            "Queue", typeof(ProgressQueue<TaskbarItemProgressState>), typeof(TaskbarProgress), new PropertyMetadata(null));

        /// <summary>Opens a session on <paramref name="window"/>'s taskbar button, starting indeterminate. Dispose it when the work ends.</summary>
        public static Session Begin(Window window) {
            if (window == null) {
                throw new ArgumentNullException(nameof(window));
            }

            if (!window.Dispatcher.CheckAccess()) {
                return window.Dispatcher.Invoke(() => Begin(window));
            }

            ProgressQueue<TaskbarItemProgressState> queue = QueueOf(window);
            var session = new Session(window, queue);
            Render(window, queue);
            return session;
        }

        private static ProgressQueue<TaskbarItemProgressState> QueueOf(Window window) {
            if (window.GetValue(QueueProperty) is ProgressQueue<TaskbarItemProgressState> queue) {
                return queue;
            }

            queue = new ProgressQueue<TaskbarItemProgressState>();
            window.SetValue(QueueProperty, queue);
            return queue;
        }

        // Producers report from worker threads; the window's taskbar info is touched on its own.
        private static void Render(Window window, ProgressQueue<TaskbarItemProgressState> queue) {
            if (!window.Dispatcher.CheckAccess()) {
                window.Dispatcher.BeginInvoke(new Action(() => Render(window, queue)));
                return;
            }

            (TaskbarItemProgressState state, double value) = queue.Shown(TaskbarItemProgressState.None);
            TaskbarItemInfo? info = window.TaskbarItemInfo;
            if (info == null) {
                info = new TaskbarItemInfo();
                Quietly(() => window.TaskbarItemInfo = info);
            }
            Quietly(() => info.ProgressState = state);
            Quietly(() => info.ProgressValue = value);
        }

        private static void Quietly(Action apply) {
            try {
                apply();
            }
            catch (NotImplementedException) { }
            catch (COMException) { }
        }

        /// <summary>One producer's claim on the bar. Only the oldest open session of the window is shown.</summary>
        public sealed class Session : IDisposable {
            private readonly Window _window;
            private readonly ProgressQueue<TaskbarItemProgressState> _queue;
            private readonly ProgressQueue<TaskbarItemProgressState>.Claim _claim;

            internal Session(Window window, ProgressQueue<TaskbarItemProgressState> queue) {
                _window = window;
                _queue = queue;
                _claim = queue.Open(TaskbarItemProgressState.Indeterminate);
            }

            /// <summary>The state this session asks for.</summary>
            public TaskbarItemProgressState State => _claim.State;

            /// <summary>The fraction this session asks for, 0 to 1.</summary>
            public double Value => _claim.Value;

            /// <summary>Whether this session is the one the bar shows.</summary>
            public bool IsShown => _queue.IsShown(_claim);

            /// <summary>A green bar at <paramref name="percent"/>, clamped to 0–100.</summary>
            public void SetNormal(int percent) => Set(TaskbarItemProgressState.Normal, Math.Max(0, Math.Min(100, percent)) / 100.0);

            /// <summary>A marquee: work is running with no known fraction.</summary>
            public void SetIndeterminate() => Set(TaskbarItemProgressState.Indeterminate, Value);

            /// <summary>A yellow bar at the current value.</summary>
            public void SetPaused() => Set(TaskbarItemProgressState.Paused, Value);

            /// <summary>A red bar — full when nothing had been reported, so the failure is visible.</summary>
            public void SetError() => Set(TaskbarItemProgressState.Error, Value > 0 ? Value : 1.0);

            /// <summary>A full green bar that stays until the session is disposed, so a finish is seen from the taskbar.</summary>
            public void SetComplete() => Set(TaskbarItemProgressState.Normal, 1.0);

            private void Set(TaskbarItemProgressState state, double value) {
                if (_queue.Set(_claim, state, value)) {
                    Render(_window, _queue);
                }
            }

            /// <summary>Gives the bar back: the next session takes it over, or it clears.</summary>
            public void Dispose() {
                if (_queue.End(_claim)) {
                    Render(_window, _queue);
                }
            }
        }
    }
}
