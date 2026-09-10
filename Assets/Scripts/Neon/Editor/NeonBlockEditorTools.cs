using UnityEditor;
using UnityEngine;

namespace Neon.EditorTools
{
    /// <summary>
    /// Convenience menu items for working with the neon outline system in the editor.
    /// </summary>
    public static class NeonBlockEditorTools
    {
        [MenuItem("Tools/Neon/Add Neon Block To Selection %#n")]
        private static void AddNeonBlockToSelection()
        {
            var manager = NeonOutlineManager.FindOrCreate();
            Undo.RegisterCreatedObjectUndo(manager.gameObject, "Create Neon Manager");

            foreach (var go in Selection.gameObjects)
            {
                if (go.GetComponent<SpriteRenderer>() == null)
                    continue;

                var block = go.GetComponent<NeonBlock>();
                if (block == null)
                {
                    block = Undo.AddComponent<NeonBlock>(go);
                    EditorUtility.SetDirty(block);
                }
            }

            manager.Rebuild();
        }

        [MenuItem("Tools/Neon/Add Neon Block To Selection %#n", true)]
        private static bool AddNeonBlockToSelectionValidate() => Selection.gameObjects.Length > 0;

        [MenuItem("Tools/Neon/Rebuild Outlines")]
        private static void RebuildOutlines()
        {
            var manager = NeonOutlineManager.FindOrCreate();
            manager.Rebuild();
        }
    }
}
