using HarmonyLib;
using System.Reflection;

namespace RigCloner
{
    public class HarmonyPatches
    {
        private static Harmony instance;
        public static bool IsPatched { get; private set; }
        public const string InstanceId = PluginInfo.GUID;

        internal static void ApplyHarmonyPatches()
        {
            if (IsPatched) return;
            if (instance == null) instance = new Harmony(InstanceId);
            instance.PatchAll(Assembly.GetExecutingAssembly());
            IsPatched = true;
        }

        internal static void RemoveHarmonyPatches()
        {
            if (instance == null || !IsPatched) return;
            instance.UnpatchSelf();
            IsPatched = false;
        }
    }
}