using UnityEditor;
using UnityEngine;
using VentsAndVending.Inventory;
using VentsAndVending.UI;

namespace VentsAndVending.Editor
{
    /// <summary>
    /// The Maintenance Underpass (Docs/superpowers/specs/2026-11-08-maintenance-underpass-design.md): a crowbar grate in
    /// the Long Crawl drops you into a patrolled sublevel that leads, upright, to an inside-only exit into the plaza.
    /// </summary>
    public static partial class PrototypeSceneBuilder
    {
        /// <summary>VentNet Supplies: the crowbar for 60 CC, delivered to the den's storage locker.</summary>
        private static void ConfigureSupplies(VentNetPanel panel)
        {
            InventoryComponent locker = null;
            foreach (InventoryComponent inv in Object.FindObjectsByType<InventoryComponent>(FindObjectsSortMode.None))
            {
                if (inv.SaveId == "hideout.locker")
                {
                    locker = inv;
                }
            }

            var so = new SerializedObject(panel);
            SerializedProperty list = so.FindProperty("_supplies");
            list.arraySize = 1;
            SerializedProperty crowbar = list.GetArrayElementAtIndex(0);
            crowbar.FindPropertyRelative("Item").objectReferenceValue = Crowbar;
            crowbar.FindPropertyRelative("Price").intValue = 60;
            crowbar.FindPropertyRelative("Blurb").stringValue = "Lever it. Pry it. Don't ask.";
            so.FindProperty("_locker").objectReferenceValue = locker;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
