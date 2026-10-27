using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Shell;

namespace Material3.Wpf {
    /// <summary>
    /// The window's taskbar progress, shared by everything that reports work. Each producer opens a
    /// <see cref="Session"/>; the oldest open session of a window drives its bar, the next one takes over when it
    /// ends, and the bar clears when the last one closes. UI thread only; without a taskbar (no shell) it does nothing.
    /// </summary>
    public static class TaskbarProgress {
        private static readonly DependencyProperty QueueProperty = DependencyProperty.RegisterAttached(
            "Queue", typeof(List<Session>), typeof(TaskbarProgress), new PropertyMetadata(null));

        /// <summary>Opens a session on <paramref name="window"/>'s taskbar button, starting indeterminate. Dispose it when the work ends.</summary>
        public static Session Begin(Window window) {
            if (window == null) {
                throw new ArgumentNullException(nameof(window));
            }

            List<Session> queue = QueueOf(window);
            var session = new Session(window);
            queue.Add(session);
            Render(window, queue);
            return session;
        }

        private static List<Session> QueueOf(Window window) {
            if (window.GetValue(QueueProperty) is List<Session> queue) {
                return queue;
            }

            queue = new List<Session>();
            window.SetValue(QueueProperty, queue);
            return queue;
        }

        private static void Render(Window window, List<Session> queue) {
            TaskbarItemProgressState state = queue.Count == 0 ? TaskbarItemProgressState.None : queue[0].State;
            double value = queue.Count == 0 ? 0 : queue[0].Value;
            TaskbarItemInfo? info = window.TaskbarItemInfo;
            if (info == null) {
                info = new TaskbarItemInfo();
                Quietly(() => window.TaskbarItemInfo = info);
            }
            Quietly(() => info.ProgressState = state);
            Quietly(() => info.ProgressValue = value);
        }

        private static void Quietly(Action apply) {
            try { apply(); }
            catch (NotImplementedException) { }
            catch (COMException) { }
        }

        /// <summary>One producer's claim on the bar. Only the oldest open session of the window is shown.</summary>
        public sealed class Session : IDisposable {
            private readonly Window _window;
            private bool _ended;

            internal Session(Window window) => _window = window;

            /// <summary>The state this session asks for.</summary>
            public TaskbarItemProgressState State { get; private set; } = TaskbarItemProgressState.Indeterminate;

            /// <summary>The fraction this session asks for, 0 to 1.</summary>
            public double Value { get; private set; }

            /// <summary>Whether this session is the one the bar shows.</summary>
            public bool IsShown => !_ended && _window.GetValue(QueueProperty) is List<Session> queue && queue.Count > 0 && ReferenceEquals(queue[0], this);

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
                if (_ended) {
                    return;
                }

                State = state;
                Value = value;
                if (IsShown) {
                    Render(_window, QueueOf(_window));
                }
            }

            /// <summary>Gives the bar back: the next session takes it over, or it clears.</summary>
            public void Dispose() {
                if (_ended) {
                    return;
                }

                List<Session> queue = QueueOf(_window);
                bool wasShown = IsShown;
                _ended = true;
                queue.Remove(this);
                if (wasShown) {
                    Render(_window, queue);
                }
            }
        }
    }
}
