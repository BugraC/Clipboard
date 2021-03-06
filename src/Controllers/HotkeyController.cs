﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Interop;

namespace ClipboardManager.Controllers
{
    class HotkeyController
    {
        public static HotKey Hotkey { get; set; }

        public class HotKey : IDisposable
        {
            private static Dictionary<int, HotKey> _dictHotKeyToCallBackProc;

            [DllImport("user32.dll")]
            private static extern bool RegisterHotKey(IntPtr hWnd, int id, UInt32 fsModifiers, UInt32 vlc);

            [DllImport("user32.dll")]
            private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

            public const int WmHotKey = 0x0312;

            private bool _disposed = false;

            public Key Key { get; private set; }
            public ModifierKeys KeyModifiers { get; private set; }
            public Action<HotKey> Action { get; private set; }
            public int Id { get; set; }

            public HotKey(Key k, ModifierKeys keyModifiers, Action<HotKey> action, bool register = true)
            {
                Key = k;
                KeyModifiers = keyModifiers;
                Action = action;
                if (register)
                {
                    Register();
                }
            }

            public void SetModifierKeys(Key k, ModifierKeys keyModifiers)
            {
                Unregister();
                Key = k;
                KeyModifiers = keyModifiers;
                Register();
            }

            public bool Register()
            {
                int virtualKeyCode = KeyInterop.VirtualKeyFromKey(Key);
                Id = virtualKeyCode + ((int)KeyModifiers * 0x10000);
                bool result = RegisterHotKey(IntPtr.Zero, Id, (UInt32)KeyModifiers, (UInt32)virtualKeyCode);

                if (_dictHotKeyToCallBackProc == null)
                {
                    _dictHotKeyToCallBackProc = new Dictionary<int, HotKey>();
                    ComponentDispatcher.ThreadFilterMessage += new ThreadMessageEventHandler(ComponentDispatcherThreadFilterMessage);
                }

                _dictHotKeyToCallBackProc.Add(Id, this);

                return result;
            }

            public void Unregister()
            {
                if (_dictHotKeyToCallBackProc.TryGetValue(Id, out HotKey hotKey))
                {
                    UnregisterHotKey(IntPtr.Zero, Id);
                    _dictHotKeyToCallBackProc.Remove(Id);
                }
            }
            
            private static void ComponentDispatcherThreadFilterMessage(ref MSG msg, ref bool handled)
            {
                if (!handled)
                {
                    if (msg.message == WmHotKey)
                    {
                        HotKey hotKey;

                        if (_dictHotKeyToCallBackProc.TryGetValue((int)msg.wParam, out hotKey))
                        {
                            if (hotKey.Action != null)
                            {
                                hotKey.Action.Invoke(hotKey);
                            }
                            handled = true;
                        }
                    }
                }
            }

            // ******************************************************************
            // Implement IDisposable.
            // Do not make this method virtual.
            // A derived class should not be able to override this method.
            public void Dispose()
            {
                Dispose(true);
                // This object will be cleaned up by the Dispose method.
                // Therefore, you should call GC.SupressFinalize to
                // take this object off the finalization queue
                // and prevent finalization code for this object
                // from executing a second time.
                GC.SuppressFinalize(this);
            }

            // ******************************************************************
            // Dispose(bool disposing) executes in two distinct scenarios.
            // If disposing equals true, the method has been called directly
            // or indirectly by a user's code. Managed and unmanaged resources
            // can be _disposed.
            // If disposing equals false, the method has been called by the
            // runtime from inside the finalizer and you should not reference
            // other objects. Only unmanaged resources can be _disposed.
            protected virtual void Dispose(bool disposing)
            {
                // Check to see if Dispose has already been called.
                if (!this._disposed)
                {
                    // If disposing equals true, dispose all managed
                    // and unmanaged resources.
                    if (disposing)
                    {
                        // Dispose managed resources.
                        Unregister();
                    }

                    // Note disposing has been done.
                    _disposed = true;
                }
            }
        }
    }
}
