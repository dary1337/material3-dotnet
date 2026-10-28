using System.Collections.Generic;

namespace Material3.Shared {
    // One window's taskbar claims: the oldest open one is shown, and every read and write of them holds the lock.
    internal sealed class ProgressQueue<TState> where TState : struct {
        private readonly List<Claim> _claims = new List<Claim>();

        internal sealed class Claim {
            internal TState State;
            internal double Value;
            internal bool Ended;
        }

        internal Claim Open(TState initial) {
            var claim = new Claim { State = initial };
            lock (_claims) {
                _claims.Add(claim);
            }

            return claim;
        }

        internal bool IsShown(Claim claim) {
            lock (_claims) {
                return Heads(claim);
            }
        }

        // True when the bar shows this claim and must be redrawn.
        internal bool Set(Claim claim, TState state, double value) {
            lock (_claims) {
                if (claim.Ended) {
                    return false;
                }

                claim.State = state;
                claim.Value = value;
                return Heads(claim);
            }
        }

        // True when the bar showed this claim and must be redrawn.
        internal bool End(Claim claim) {
            lock (_claims) {
                if (claim.Ended) {
                    return false;
                }

                bool shown = Heads(claim);
                claim.Ended = true;
                _claims.Remove(claim);
                return shown;
            }
        }

        internal (TState State, double Value) Shown(TState none) {
            lock (_claims) {
                return _claims.Count == 0 ? (none, 0) : (_claims[0].State, _claims[0].Value);
            }
        }

        private bool Heads(Claim claim) => !claim.Ended && _claims.Count > 0 && ReferenceEquals(_claims[0], claim);
    }
}
