using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Material3.WinForms.Controls {
    // What an owner-drawn control tells a screen reader: the role, the state and the name a native control would
    // report, an optional default action, and, for a bar of tabs or segments, one child per item.
    internal sealed class MaterialAccessible : Control.ControlAccessibleObject {
        private readonly Control _owner;
        private readonly AccessibleRole _role;
        private readonly Func<AccessibleStates>? _state;
        private readonly Func<string?>? _name;
        private readonly Func<string?>? _value;
        private readonly string? _action;
        private readonly Action? _run;
        private readonly Func<IReadOnlyList<Item>>? _items;

        internal sealed class Item {
            public string Name = string.Empty;
            public AccessibleRole Role;
            public AccessibleStates State;
            public Rectangle Bounds;
            public Action? Select;
        }

        public MaterialAccessible(Control owner, AccessibleRole role, Func<AccessibleStates>? state = null, Func<string?>? name = null,
            Func<string?>? value = null, string? action = null, Action? run = null, Func<IReadOnlyList<Item>>? items = null) : base(owner) {
            _owner = owner;
            _role = role;
            _state = state;
            _name = name;
            _value = value;
            _action = action;
            _run = run;
            _items = items;
        }

        public override AccessibleRole Role => _owner.AccessibleRole != AccessibleRole.Default ? _owner.AccessibleRole : _role;

        public override AccessibleStates State => base.State | (_state?.Invoke() ?? AccessibleStates.None);

        public override string? Name {
            get => _owner.AccessibleName ?? _name?.Invoke() ?? base.Name;
            set => base.Name = value;
        }

        public override string? Value {
            get => _value?.Invoke() ?? base.Value;
            set => base.Value = value;
        }

        public override string? DefaultAction => _action ?? base.DefaultAction;

        public override void DoDefaultAction() {
            if (_run != null && _owner.Enabled) {
                _run();
            }
            else {
                base.DoDefaultAction();
            }
        }

        public override int GetChildCount() => _items?.Invoke().Count ?? base.GetChildCount();

        public override AccessibleObject? GetChild(int index) {
            if (_items == null) {
                return base.GetChild(index);
            }

            IReadOnlyList<Item> items = _items();
            return index >= 0 && index < items.Count ? new ItemObject(this, items[index]) : null;
        }

        private sealed class ItemObject : AccessibleObject {
            private readonly MaterialAccessible _parent;
            private readonly Item _item;

            public ItemObject(MaterialAccessible parent, Item item) {
                _parent = parent;
                _item = item;
            }

            public override string? Name {
                get => _item.Name;
                set { }
            }

            public override AccessibleRole Role => _item.Role;

            public override AccessibleStates State => _item.State;

            public override AccessibleObject Parent => _parent;

            public override Rectangle Bounds => _parent._owner.RectangleToScreen(_item.Bounds);

            public override string? DefaultAction => _item.Select != null ? "Select" : null;

            public override void DoDefaultAction() => _item.Select?.Invoke();
        }
    }
}
