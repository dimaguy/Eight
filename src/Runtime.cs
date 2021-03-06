using Eight.Module;
using KeraLua;
using System;
using System.IO;
using System.Text;

namespace Eight {
    public static class Runtime {
        public static Lua LuaState;
        public static Lua State;

        private static bool _quit;
        public static bool Init() {
            _quit = false;
            LuaState = new Lua {
                Encoding = Encoding.UTF8
            };

            LuaState.PushString($"Eight {Eight.Version}");
            LuaState.SetGlobal("_HOST");

            DoLibs();

            State = LuaState.NewThread();

            if ( !File.Exists("../bios.lua") ) {
                Console.WriteLine("Could not find bios.lua");
                return false;
            }

            var biosContent = File.ReadAllText("../bios.lua");

            var status = State.LoadString(biosContent, "@BIOS");
            if ( status != LuaStatus.OK ) {
                var error = State.ToString(-1);
                Console.WriteLine("Lua Load Exception: {0}", error);
                return false;
            }

            return true;
        }

        private static void DoLibs() {
            // Get io.open and io.lines for filesystem
            LuaState.GetGlobal("io");
            LuaState.GetField(-1, "open");
            LuaState.SetField((int)LuaRegistry.Index, "_io_open");
            LuaState.Pop(1);

            LuaState.GetGlobal("io");
            LuaState.GetField(-1, "lines");
            LuaState.SetField((int)LuaRegistry.Index, "_io_lines");
            LuaState.Pop(1);

            // Get debug.traceback

            LuaState.GetGlobal("debug");
            LuaState.GetField(-1, "traceback");
            LuaState.SetField((int)LuaRegistry.Index, "_debug_traceback");
            LuaState.Pop(1);

            // Destroy dem libtards with shapiro

            LuaState.PushNil();
            LuaState.SetGlobal("debug");

            LuaState.PushNil();
            LuaState.SetGlobal("io");

            LuaState.PushNil();
            LuaState.SetGlobal("dofile");

            LuaState.PushNil();
            LuaState.SetGlobal("loadfile");



            FileSystem.Setup();
            Os.Setup();
            Timer.Setup();
            Screen.Setup();
            HTTP.Setup();
            Audio.Setup();
        }

        public static bool Resume(int n = 0) {
            if ( _quit ) return false;
            var status = State.Resume(null, n, out var nres);
            if ( status == LuaStatus.OK || status == LuaStatus.Yield ) {
                State.Pop(nres);
                if ( status != LuaStatus.OK ) return true;
                Console.WriteLine(State.ToString(-1));
                return false;
            }

            var error = State.ToString(-1) ?? "Unknown Error";
            State.Traceback(State);
            var traceback = State.ToString(-1) ?? "Unknown Trace";

            string nr;
            switch ( status ) {
                case LuaStatus.ErrRun:
                    nr = "Runtime Error";
                    break;
                case LuaStatus.ErrMem:
                    nr = "Memory Allocation Error";
                    break;
                case LuaStatus.ErrErr:
                    nr = "Error Handler Error";
                    break;
                case LuaStatus.ErrSyntax:
                    nr = "Syntax Error";
                    break;
                default:
                    nr = status.ToString();
                    break;
            }

            var hexStatus = status.ToString("X").TrimStart('0');
            Console.WriteLine($"Lua Exception [0x{hexStatus}] {nr}: {error}");
            Console.WriteLine(traceback);
            Console.WriteLine("Could not resume");

            return false;
        }

        public static void Quit() {
            _quit = true;
            State.Close();
        }
    }
}